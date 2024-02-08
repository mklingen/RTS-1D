using Godot;
using System;

public partial class UnitStats : Resource
{
    [Export]
    public string Name;

    [ExportGroup("Movement")]
    // If <= 0.0, this is a building. Otherwise, it is a moving unit.
    [Export]
    public float MaxSpeed = 1.0f;

    [ExportGroup("Health")]
    [Export]
    public float MaxHealth = 100.0f;

    [Export]
    public float Armor = 0.0f;

    [ExportGroup("Sensing")]
    [Export]
    public Shape3D SensingShape;

    [Export(PropertyHint.Layers3DPhysics)]
    public uint SensingCollisionMask;

    [ExportGroup("Building")]
    [Export(PropertyHint.File, "*.tscn")]
    public string PrefabFile;

    [Export]
    public float BuildTime = 10.0f;

    [Export]
    public float BuildCost = 1.0f;

    public bool CanMove()
    {
        return MaxSpeed > 0;
    }

    public Unit CreateUnit(Node parent)
    {
        PackedScene scene = (PackedScene)ResourceLoader.Load(PrefabFile);
        var unit = scene.Instantiate<Unit>();
        if (unit == null) {
            GD.PrintErr("Scene did not contain Unit as root.");
            return null;
        }
        parent.AddChild(unit);
        return unit;
    }
}
