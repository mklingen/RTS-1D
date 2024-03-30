using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

[GlobalClass]
public partial class Unit : PlanetObject, Game.ITeamObject, Game.IDamageable
{
    [Signal]
    public delegate void OnDeathEventHandler(Unit unitThatDied);

    [Export]
    public UnitStats Stats = null;

    [Export] private bool AddBuildingOnReady = false;

    [Export]
    protected float targetDistanceThreshold = 0.08f;
    //protected Vector3 currentTarget;

    private TaskLib.Task currentTask;

    public void DoTask(TaskLib.Task task)
    {
        currentTask = task;
        currentTask.Start();
    }

    public bool IsNearGoToTarget(Vector3 goToTarget, float threshold)
    {
        return (GlobalPosition - goToTarget).Length() < threshold;
    }

    public enum State
    { 
        Idle,
        MovingToTarget
    }

    private struct MoveToTargetState
    {
        public Vector3 Target;
        public Vector3 Start;
        public float StartTime;
        public float EndTime;

        public float GetAlpha(float t)
        {
            return Mathf.Clamp((t - StartTime) / (EndTime - StartTime), 0.0f, 1.0f);
        }

        public Vector3 GetCurrentTarget(float t, Curve curve)
        {
            return (Target - Start) * curve.Sample(GetAlpha(t)) + Start;
        }

        public void StartMoving(Vector3 from, Vector3 to, float now, float timeToTravel)
        {
            Start = from;
            Target = to;
            StartTime = now;
            EndTime = now + timeToTravel;
        }
    }

    private MoveToTargetState moveToTargetState;

    [Export]
    private State state;

    private int _team;
    [Export]
    public int Team { get { return _team;  } set { _team = value; this.QueueAction(OnSetTeam); } }

    // List of weapons attached to this unit.
    private List<Weapon> weapons;
    // List of resource collectors attached to this unit.
    private List<Collector> collectors;
    // Resource drop offs attached to this unit.
    private List<ResourceDropOff> dropOffs;
    // List of structure builders attached to this unit.
    private List<Builder> builders;

    private int gridIndex = -1;

    public List<Collector> GetCollectors()
    {
        return collectors;
    }

    private List<Action> frameActions = new List<Action>();

    private void QueueAction(Action action)
    {
        frameActions.Add(action);
    }

    private void OnSetTeam()
    {
        GetTeamObject().AddUnit(this);
        foreach (var child in Game.FindInterfacesRecursive<ISetTeam>(this)) {
            child.SetTeam(Team);
        }
        ApplyTeamColor();
    }

    public void ApplyTeamColor()
    {
        var meshes = Game.FindChildrenRecursive<MeshInstance3D>(this);
        foreach (MeshInstance3D mesh in meshes) {

            if (mesh.GetSurfaceOverrideMaterialCount() > 1) {
                StandardMaterial3D material = mesh.GetSurfaceOverrideMaterial(1) as StandardMaterial3D;
                if (material != null && Game.Get() != null && Game.Get().GetTeam(Team) != null) {
                    var team = Game.Get().GetTeam(Team);
                    if (team.TeamColorMaterial == null) {
                        team.TeamColorMaterial = material.Duplicate() as StandardMaterial3D;
                        team.TeamColorMaterial.AlbedoColor = team.Color;
                        
                    }
                    mesh.SetSurfaceOverrideMaterial(1, team.TeamColorMaterial);
                }
                
            }

        }
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
    private Unit enemyIsAttackingMe;
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

    public void NotifyUnderAttack(Unit enemy)
    {
        enemyIsAttackingMe = enemy;
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
     
        if (AddBuildingOnReady) {
            AddToGrid();
        }

    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        foreach (var action in frameActions) {
            action.Invoke();
        }
        frameActions.Clear();
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

        if (!Stats.IsABuilding && GlobalVelocity.LengthSquared() > 1e-3 || gridIndex == -1) {
            MaybeMoveGridIndex();
        }
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
        if (enemyIsAttackingMe != null && 
            enemyIsAttackingMe.NativeInstance.ToInt64() != 0x0 &&
            weapons.Count > 0 &&
            nearestEnemy == null) {
            GoTo(enemyIsAttackingMe.GlobalPosition);
            nearestEnemy = enemyIsAttackingMe;
            enemyIsAttackingMe = null;
        }
        else if (collectors.Count > 0 && currentTask == null) {
            var field = Game.FindChildrenRecursive<ResourceField>(Game.Get()).
                Where(field => field.GetResourcesRemaining() > 0)
                .OrderBy(field => field.GlobalPosition.DistanceSquaredTo(GlobalPosition))
                .FirstOrDefault();
            Game.Team team = this.GetTeamObject();
            if (field != null && team != null) {
                ResourceDropOff dropOff = team.GetDropOffs().FirstOrDefault();
                if (dropOff != null) {
                    Vector3? motePosition = field.GetClosestMotePosition(GlobalPosition);
                    if (motePosition != null) {
                        DoTask(new TaskLib.SequenceTask(new List<TaskLib.Task> { new TaskLib.GoToTask(this, planet.ProjectToCylinder(motePosition.Value, Height, Depth), 0.08f), new TaskLib.HarvestTask(this, field), new TaskLib.DropOffTask(this, collectors[0], dropOff) }));
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
        if ((moveToTargetState.Target - pos).LengthSquared() > 1e-2 || state != State.MovingToTarget) {
            moveToTargetState.StartMoving(GlobalPosition, pos, Game.GetTime(), (GlobalPosition - pos).Length() / Stats.MaxSpeed);
            TransitionState(State.MovingToTarget);
        }
    }

    protected void OnIdle(double delta)
    {
        GlobalVelocity *= 0.5f;
        foreach (var weapon in weapons) {
            weapon.ClearPointTarget();
        }

    }

    protected void OnMovingToTarget(double delta)
    {
        Vector3 currentTarget = planet.ProjectToCylinder(moveToTargetState.GetCurrentTarget(Game.GetTime(), Stats.MovementCurve), Height, Depth);
        if (moveToTargetState.GetAlpha(Game.GetTime()) >= 1.0f) {
            TransitionState(State.Idle);
            return;
        }
        Vector3 nextTarget = planet.ProjectToCylinder(moveToTargetState.GetCurrentTarget((float)(Game.GetTime() + delta), Stats.MovementCurve), Height, Depth);
        Vector3 targetVelocity = (nextTarget - currentTarget) / (float)delta;
        GlobalVelocity = targetVelocity;

        foreach (var weapon in weapons) {
            weapon.PointToward(nextTarget + targetVelocity * 2);
        }
    }

    protected void MaybeShootAtTargets()
    {
        if (nearestEnemy == null) {
            return;
        }
        if (ShootAt(nearestEnemy.GlobalPosition, nearestEnemy)) {
            nearestEnemy.NotifyUnderAttack(this);
        }
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
                closestDistSquared = distSquared;
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
    bool ShootAt(Vector3 target, Node3D targetNode = null)
    {
        bool shot = false;
        foreach (var weapon in weapons) {
            shot = shot || weapon.MaybeShoot(target, targetNode);
        }
        return shot;
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
            planet.RemoveBuilding(this);
        } else {
            planet.RemoveUnit(this);
        }
        EmitSignal(SignalName.OnDeath, this);
        QueueFree();
    }

    public void AddToGrid()
    {
        if (Stats.IsABuilding) {
            Game.Get().GetPlanet().AddBuilding(this, GlobalPosition);
        } else {
            Game.Get().GetPlanet().AddUnit(this, GlobalPosition, 1);
        }
    }

    public void MaybeMoveGridIndex()
    {
        int lastIdx = gridIndex;
        gridIndex = Game.Get().GetPlanet().GetGridIndexAt(GlobalPosition);

        if (Stats.IsABuilding) {
            return;
        }

        if (gridIndex != lastIdx) {
            Game.Get().GetPlanet().RemoveUnit(this);
            Game.Get().GetPlanet().AddUnit(this, GlobalPosition, 1);
        }
    }

}
