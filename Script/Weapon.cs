using Godot;
using System;

public partial class Weapon : Node3D
{
    // All weapons of the same type share the same stats.
    [Export(PropertyHint.ResourceType, "WeaponStats")] public WeaponStats Stats;
    [Export] private Transform3D muzzleOffset = Transform3D.Identity;
    [Export] private Vector3 localRotationAxis = Vector3.Up;
    [Export] private float turnSmoothing = 0.75f;

    // The time the weapon last fired a shot.
    private float timeLastShot = 0;
    private float randomTimeOffset = 0;

    private Vector3? currentGlobalTarget;
    private Transform3D originalTransform;


    public override void _Ready()
    {
        base._Ready();
        originalTransform = Transform;
    }

    private Vector3 GetMuzzleGlobalPosition()
    {
        return (GlobalTransform * muzzleOffset).Origin;
    }

    public void ClearPointTarget()
    {
        currentGlobalTarget = null;
    }

    public void PointToward(Vector3 globalTarget)
    {
        currentGlobalTarget = globalTarget;
    }

    // Shoots the weapon at the given target.
    public void Shoot(Vector3 target, Node3D targetNode = null)
    {
        randomTimeOffset = GD.Randf() * 0.1f;
        timeLastShot = Game.GetTime();
        if (Stats.BulletPrefab != null) {
            var bullet = Stats.BulletPrefab.Instantiate();
            if (bullet != null) {
                Game.Get().AddChild(bullet);
                Bullet bulletInterface = bullet as Bullet;
                if (bulletInterface != null) {
                    bulletInterface.Launch(GetMuzzleGlobalPosition(), target, targetNode);
                    bulletInterface.WeaponStats = Stats;
                }
            } else {
                GD.PrintErr("Bullet prefab created was null.");
            }
        }
    }


    // Either returns false if we can't shoot at the target, or shoots at the target.
    public bool MaybeShoot(Vector3 target, Node3D targetNode = null)
    {
        PointToward(target);
        float now = Game.GetTime();
        if (now - timeLastShot < Stats.ShotTime + randomTimeOffset) {
            // Too soon.
            return false;
        }
        float range = (GlobalPosition - target).Length();
        if (range > Stats.MaxRange || range < Stats.MinRange) {
            // Too far away.
            return false;
        }
        // We can shoot.
        Shoot(target, targetNode);
        return true;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (currentGlobalTarget != null) {
            var parentNode = GetParentNode3D();
            Vector3 projectedLocalTarget = ToLocal(currentGlobalTarget.Value);
            Vector3 projUp = localRotationAxis.Dot(projectedLocalTarget) * localRotationAxis;
            projectedLocalTarget -= projUp;
            Transform3D nextTarget = GlobalTransform.LookingAt(ToGlobal(projectedLocalTarget), parentNode.ToGlobal(localRotationAxis));
            GlobalTransform = nextTarget.InterpolateWith(GlobalTransform, turnSmoothing);
        }
    }
}
