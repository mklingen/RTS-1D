using Godot;
using System;

[GlobalClass]
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
    [Export]public bool IsABuilding = false;

    [Export(PropertyHint.File, "*.tscn")]
    public string PrefabFile;

    [Export(PropertyHint.File, "*.tscn")]
    public string BuildCursorPrefabFile;


    [Export(PropertyHint.File, "*.tscn")]
    public string ConstructionPilePrefab;

    [Export]
    public float BuildTime = 10.0f;

    [Export]
    public float BuildCost = 1.0f;

    [Export]
    public float DefaultDepth = 0.0f;

    [Export]
    public float DefaultHeight = 0.0f;

    [Flags]
    public enum Abilities
    {
        None=0,
        Move=1,
        BuildUnits=2,
        BuildStructures=4
    }

    [ExportGroup("Abilities")]
    [Export]
    public Abilities CanDoAbilities;

    public bool CanMove()
    {
        return MaxSpeed > 0;
    }

    public Unit CreateUnit(Node parent)
    {
        PackedScene scene = (PackedScene)ResourceLoader.Load(PrefabFile);
        if (scene == null) {
            GD.PrintErr($"Unable to load {PrefabFile}");
            return null;
        }
        var unit = scene.Instantiate<Unit>();
        if (unit == null) {
            GD.PrintErr("Scene did not contain Unit as root.");
            return null;
        }
        parent.AddChild(unit);
        return unit;
    }

    public ConstructionPile CreateConstructionPile(Node parent, int team)
    {
        PackedScene scene = (PackedScene)ResourceLoader.Load(ConstructionPilePrefab);
        if (scene == null) {
            GD.PrintErr($"Unable to load {PrefabFile}");
            return null;
        }
        var node = scene.Instantiate<ConstructionPile>();
        if (node == null) {
            GD.PrintErr("Scene did not contain ConstructionPile as root.");
            return null;
        }
        node.Unit = this;
        node.Team = team;
        parent.AddChild(node);
        return node;
    }
}
