using Godot;
using System;

[GlobalClass]
public partial class WeaponStats : Resource
{
    // Human readable name of the weapon.
    [Export] public string Name;
    // The amount of damage the weapon does per shot.
    [Export] public float DamagePerShot;
    // The amount of armor piercing damage.
    [Export] public float ArmorPiercing;
    // The time between shots.
    [Export] public float ShotTime;
    // The maximum range that the weapon can shoot.
    [Export] public float MaxRange;
    // The minimum range the weapon can shoot (will not shoot if below this range).
    [Export] public float MinRange;
    // A prefab to use for the bullet. If this exists, a bullet will be created on
    // shot and added to the scene.
    [Export] public PackedScene BulletPrefab;
}
