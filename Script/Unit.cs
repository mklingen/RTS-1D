using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

public partial class Unit : PlanetObject, Game.ITeamObject, Game.IDamageable
{
    [Signal]
    public delegate void OnDeathEventHandler(Unit unitThatDied);

    [Export]
    public UnitStats Stats = null;

    [Export] private bool AddBuildingOnReady = false;

    [Export]
    protected float targetDistanceThreshold = 0.005f;
    protected Vector3 currentTarget;

    private TaskLib.Task currentTask;

    public void DoTask(TaskLib.Task task)
    {
        currentTask = task;
        currentTask.Start();
    }

    public bool IsNearGoToTarget(float threshold)
    {
        return (GlobalPosition - currentTarget).Length() < threshold;
    }

    public enum State
    { 
        Idle,
        MovingToTarget
    }

    [Export]
    private State state;

    [Export]
    public int Team { get; set; }

    // List of weapons attached to this unit.
    private List<Weapon> weapons;
    // List of resource collectors attached to this unit.
    private List<Collector> collectors;
    // Resource drop offs attached to this unit.
    private List<ResourceDropOff> dropOffs;
    // List of structure builders attached to this unit.
    private List<Builder> builders;
    public List<Collector> GetCollectors()
    {
        return collectors;
    }

    public List<Weapon> GetWeapons()
    {
        return weapons;
    }

    public List<ResourceDropOff> GetDropOffs()
    {
        return dropOffs;
    }

    private float hitPoints;

    private PhysicsShapeQueryParameters3D queryParameters;

    private Unit nearestEnemy;

    private HealthBar healthBar;

    public interface ISetTeam
    {
        public void SetTeam(int team);
        public int GetTeam();

        public Game.Team GetTeamObject()
        {
            return Game.Get().GetTeam(GetTeam());
        }
    }
    public Game.Team GetTeamObject()
    {
        return Game.Get().GetTeam(Team);
    }


    public override void _Ready()
    {
        base._Ready();
        if (Stats == null) {
            Stats = new UnitStats();
            GD.PrintErr("Unit had no stats.");
        }
        hitPoints = Stats.MaxHealth;
        weapons = Game.FindChildrenRecursive<Weapon>(this).ToList();
        collectors = Game.FindChildrenRecursive<Collector>(this).ToList();
        dropOffs = Game.FindChildrenRecursive<ResourceDropOff>(this).ToList();
        builders = Game.FindChildrenRecursive<Builder>(this).ToList();
        queryParameters = new PhysicsShapeQueryParameters3D();
        queryParameters.Shape = Stats.SensingShape;
        queryParameters.CollisionMask = Stats.SensingCollisionMask;
        queryParameters.CollideWithBodies = true;
        healthBar = Game.FindChildrenRecursive<HealthBar>(this).FirstOrDefault();
        foreach (var setTeam in Game.FindInterfacesRecursive<ISetTeam>(this)) {
            setTeam.SetTeam(Team);
        }
        Game.Get().AddUnit(this);
        if (AddBuildingOnReady) {
            MaybeAddBuilding();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (currentTask != null) {
            if (currentTask.IsDone()) {
                currentTask.End();
                currentTask = null;
            } else {
                currentTask.Update(delta);
            }
        } else {
            currentTask = GetIdleTask();
        }
        switch (state) {
            case State.Idle: {
                OnIdle(delta);
                break;
            }
            case State.MovingToTarget: {
                OnMovingToTarget(delta);
                break;
            }
        }
        HandleTargetSelection(32);
        MaybeShootAtTargets();
    }

    protected void TransitionState(State nextState)
    {
        state = nextState;
    }

    public void Stop()
    {
        TransitionState(State.Idle);
        GlobalVelocity *= 0;
    }

    protected TaskLib.Task GetIdleTask()
    {
        if (collectors.Count > 0 && currentTask == null) {
            var field = Game.FindChildrenRecursive<ResourceField>(Game.Get()).FirstOrDefault();
            Game.Team team = this.GetTeamObject();
            if (field != null && team != null) {
                ResourceDropOff dropOff = team.GetDropOffs().FirstOrDefault();
                if (dropOff != null) {
                    Vector3? motePosition = field.GetClosestMotePosition(GlobalPosition);
                    if (motePosition != null) {
                        DoTask(new TaskLib.SequenceTask(new List<TaskLib.Task> { new TaskLib.GoToTask(this, motePosition.Value, 0.01f), new TaskLib.HarvestTask(this, field), new TaskLib.DropOffTask(this, collectors[0], dropOff) }));
                        return currentTask;
                    }
                }
            }
        }
        else if (builders.Count > 0 && currentTask == null) {
            ConstructionPile nextBuild = builders.FirstOrDefault()?.GetCurrentPile();
            if (nextBuild != null) {
                DoTask(new TaskLib.BuildTask(this, builders.First(), nextBuild));
                return currentTask;
            } else {
                builders.First().SetToNearestPile();
            }
        }
        return null;
    }

    public void GoTo(Vector3 pos)
    {
        if (!Stats.CanMove()) {
            return;
        }
        currentTarget = planet.ProjectToCylinder(pos, Height, Depth);
        TransitionState(State.MovingToTarget);
    }

    protected void OnIdle(double delta)
    {
        GlobalVelocity *= 0.5f;
    }

    protected void OnMovingToTarget(double delta)
    {
        if ((currentTarget - GlobalPosition).Length() < targetDistanceThreshold) {
            TransitionState(State.Idle);
            return;
        }

        Vector3 targetVelocity = (currentTarget - GlobalPosition) * Stats.MaxSpeed;
        Vector3 tangentVelocity = planet.ProjectToCylinder(GlobalPosition + targetVelocity.Normalized(), Height, Depth) - GlobalPosition;
        targetVelocity = (tangentVelocity.Normalized() * targetVelocity.Length()).LimitLength(Stats.MaxSpeed);
        GlobalVelocity = GlobalVelocity * 0.1f + 0.9f * targetVelocity;
    }

    protected void MaybeShootAtTargets()
    {
        if (nearestEnemy == null) {
            return;
        }
        ShootAt(nearestEnemy.GlobalPosition, nearestEnemy);
    }

    protected void HandleTargetSelection(int maxTargets)
    {
        // Get the state space.
        var spaceState = GetWorld3D().DirectSpaceState;
        queryParameters.Transform = GlobalTransform;
        // Get the objects intersecting the sensor.
        var results = spaceState.IntersectShape(queryParameters, maxTargets);

        // Loop through all results and find the one that has the closest distance.
        Unit closestTarget = null;
        float closestDistSquared = float.MaxValue;
        foreach (var result in results) {
            if (result == null || result.Count == 0) {
                // No collision?
                continue;
            }
            // Get the collider.
            Node collider = (Node)result["collider"];
            // Find the top level object.
            Node3D root = Game.GetRootEntity(Game.Get(), collider as Node3D);
            // Only consider units.
            if (root == null || root is not Unit) {
                continue;
            }
            Unit unit = root as Unit;
            if (unit.Team == Team) {
                // Do not target units of the same team.
                continue;
            }
            float distSquared = (unit.GlobalPosition - GlobalPosition).LengthSquared();
            if (distSquared < closestDistSquared) {
                closestTarget = unit;
            }
        }
        // Now select the nearest enemy.
        if (closestTarget != null) {
            this.nearestEnemy = closestTarget;
        } else {
            this.nearestEnemy = null;
        }
    }

    // Attempts to shoot at the target with all weapons.
    void ShootAt(Vector3 target, Node3D targetNode = null)
    {
        foreach (var weapon in weapons) {
            weapon.MaybeShoot(target, targetNode);
        }
    }

    public void DoDamage(float damage, float armorPiercing)
    {
        float damageAmount = Mathf.Max(damage - Mathf.Max(armorPiercing - Stats.Armor, 0.0f), 0.0f);
        hitPoints -= damageAmount;
        if (healthBar != null) {
            healthBar.SetHealth(hitPoints, Stats.MaxHealth);
        }
        if (hitPoints < 0) {
            Die();
        }
    }

    public void Die()
    {
        if (Stats.IsABuilding) {
            planet.RemoveBuilding(GlobalPosition);
        }
        EmitSignal(SignalName.OnDeath, this);
        QueueFree();
    }

    public void MaybeAddBuilding()
    {
        if (Stats.IsABuilding) {
            Game.Get().GetPlanet().AddBuilding(this, GlobalPosition);
        }
    }
}
