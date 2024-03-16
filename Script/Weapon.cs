using Godot;
using System;

public partial class Weapon : Node3D
{
    // All weapons of the same type share the same stats.
    [Export(PropertyHint.ResourceType, "WeaponStats")] public WeaponStats Stats;
    // The time the weapon last fired a shot.
    private float timeLastShot = 0;
    private float randomTimeOffset = 0;
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
                    bulletInterface.Launch(GlobalPosition, target, targetNode);
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
}
