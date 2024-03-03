using Godot;
using System;
using System.Linq;

public partial class UnitBuilderTool : Node, ITool
{

    private BuildMenu buildMenu;
    private ConstructionBay currentBay;

    public override void _Ready()
    {
        base._Ready();
        buildMenu = Game.FindChildrenRecursive<BuildMenu>(Game.Get()).FirstOrDefault();
        if (buildMenu != null) {
            buildMenu.Close();
        }
    }
    public string GetName()
    {
        return "Build Units";
    }

    public int GetPriority()
    {
        return 1;
    }

    public UnitStats.Abilities GetAbilities()
    {
        return UnitStats.Abilities.BuildUnits;
    }

    public void OnSelect(ConstructionBay bay)
    {
        currentBay = bay;
    }

    public void OnDeselect(ConstructionBay bay)
    {
        currentBay = bay;
    }

    public void OnActivate()
    {
        if (buildMenu != null) {
            buildMenu.Open(currentBay);
        }
    }

    public void OnDeactivate()
    {
        if (buildMenu != null) {
            buildMenu.Close();
        }
    }

    public void OnMouseClick(Vector2 mouseScreenPos, Vector3 mousePos, ITool.MouseButton click)
    {

    }

    public void OnMouseFirstPressed(Vector2 mouseScreenPos, Vector3 mouseWorldPos, ITool.MouseButton click)
    {

    }

    public void OnMouseMove(Vector2 mouseScreenPos, Vector3 mouseWorldPos)
    {

    }
}
