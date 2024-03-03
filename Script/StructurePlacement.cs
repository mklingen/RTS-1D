using Godot;
using System;
using System.Linq;

// Controls the player placing structures.
public partial class StructurePlacement : Node, ITool, Builder.IBuilderSelectionCallback
{
    [Export]
    private StructureCursor structureCursor = null;

    [ExportGroup("Colors")]
    private Color goodColor = new Color(0, 0, 1);

    private Color badColor = new Color(1, 0, 0);

    private Planet planet;

    private Builder builder;

    private BuildStructuresMenu buildMenu;

    private string buildingName;

    public override void _Ready()
    {
        base._Ready();

        buildMenu = Game.FindChildrenRecursive<BuildStructuresMenu>(Game.Get()).FirstOrDefault();
    }

    public string GetName()
    {
        return "Build Structures";
    }
    public int GetPriority()
    {
        return 2;
    }

    public void SetBuilder(Builder b)
    {
        builder = b;
    }

    [ExportGroup("Prefabs")]
    [Export(PropertyHint.File, "*.tscn")] private string cursorPrefab;
    [Export(PropertyHint.File, "*.tscn")] private string buildingPrefab;

    private UnitStats currentStats;

    private StructureCursor CreateCursor()
    {
        PackedScene scene = (PackedScene)ResourceLoader.Load(cursorPrefab);
        var node = scene.Instantiate<StructureCursor>();
        if (node == null) {
            GD.PrintErr("Scene did not contain node3d as root.");
            return null;
        }
        this.AddChild(node);
        return node;
    }

    public void SetPrefabs(UnitStats stats, string buildingName, string cursor, string buildingAfterPlacement)
    {
        this.buildingName = buildingName;
        if (structureCursor == null) {
            structureCursor = CreateCursor();
        }
        structureCursor.SetPrefab(cursor);
        currentStats = stats;
        structureCursor.Depth = stats.DefaultDepth;
        structureCursor.Height = stats.DefaultHeight;
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

        if (buildMenu != null) {
            buildMenu.Open(builder);
        }
    }

    public void OnDeactivate()
    {
        if (structureCursor != null) {
            structureCursor.Visible = false;
        }
        if (buildMenu != null) {
            buildMenu.Close();
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
                    builder.StartBuilding(buildingName, structureCursor.GlobalPosition);
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
        structureCursor.Depth = currentStats.DefaultDepth;
        structureCursor.Height = currentStats.DefaultHeight;
        canBuild = GetPlanet().CanBuildBuilding(mouseWorldPos);
        structureCursor.SetColor(canBuild ? goodColor : badColor);
    }

    public UnitStats.Abilities GetAbilities()
    {
        return UnitStats.Abilities.BuildStructures;
    }

    public void OnSelect(Builder bay)
    {
        this.builder = bay;
    }

    public void OnDeselect(Builder bay)
    {
       if (bay == this.builder) {
            this.builder = null;
       }
    }
}
