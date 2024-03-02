using Godot;
using System;

// Controls the player placing structures.
public partial class StructurePlacement : Node, ITool
{
    [Export]
    private StructureCursor structureCursor = null;

    [ExportGroup("Colors")]
    private Color goodColor = new Color(0, 0, 1);

    private Color badColor = new Color(1, 0, 0);

    private Planet planet;

    public override void _Ready()
    {
        base._Ready();

    }
    [ExportGroup("Prefabs")]
    [Export(PropertyHint.File, "*.tscn")] private string cursorPrefab;
    [Export(PropertyHint.File, "*.tscn")] private string buildingPrefab;

    public void SetPrefabs(string cursor, string buildingAfterPlacement)
    {
        structureCursor?.SetPrefab(cursor);
        buildingPrefab = buildingAfterPlacement;
    }

    private Planet GetPlanet()
    {
        if (planet == null) {
            planet = Game.Get().GetPlanet();
        }
        return planet;
    }

    public void OnActivate()
    {
        if (structureCursor != null) {
            structureCursor.Visible = true;
        }
        // TODO, hook this up to UI.
        SetPrefabs(cursorPrefab, buildingPrefab);
    }

    public void OnDeactivate()
    {
        if (structureCursor != null) {
            structureCursor.Visible = false;
        }
    }

    public void OnMouseClick(Vector2 mousePixels, Vector3 mousePos, ITool.MouseButton click)
    {
        if (structureCursor == null) {
            return;
        }
        switch (click) {
            case ITool.MouseButton.Left:
                structureCursor.SetColor(badColor);
                if (canBuild) {
                    SpawnBuilding();
                }
                break;
            case ITool.MouseButton.Right:
                structureCursor.SetColor(new Color(1, 1, 1));
                break;
        }
    }

    private Node3D SpawnBuilding()
    {
        PackedScene scene = (PackedScene)ResourceLoader.Load(buildingPrefab);
        var node = scene.Instantiate<Node3D>();
        if (node == null) {
            GD.PrintErr("Scene did not contain node3d as root.");
            return null;
        }
        Game.Get().AddChild(node);
        (node as PlanetObject).ForceSetPosition(structureCursor.GlobalPosition);

        if (node is Unit) {
            Unit unit = node as Unit;
            unit.MaybeAddBuilding();
        }
        return node;
    }

    public void OnMouseFirstPressed(Vector2 mousePixels, Vector3 mousePos, ITool.MouseButton click)
    {

    }

    private bool canBuild = false;

    public void OnMouseMove(Vector2 mouseScreenPos, Vector3 mouseWorldPos)
    {
        if (structureCursor == null) {
            return;
        }
        structureCursor.ForceSetPosition(mouseWorldPos);
        canBuild = GetPlanet().CanBuildBuilding(mouseWorldPos);
        structureCursor.SetColor(canBuild ? goodColor : badColor);
    }

    public UnitStats.Abilities GetAbilities()
    {
        return UnitStats.Abilities.BuildStructures;
    }
}
