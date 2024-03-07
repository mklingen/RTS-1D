using Godot;
using System.Collections.Generic;
using System;
using System.Linq;
using static ConstructionBay;

// This is like ConstructionBay, but for buildings.
public partial class Builder : Node3D, Unit.ISetTeam, Selectable.ISelectionInterface
{
    [ExportGroup("Construction")]
    [Export]
    private UnitStats[] availableUnits = new UnitStats[0];

    [Export]
    private float buildSpeed = 1.0f;

    [Export]
    private float buildRadius = 0.25f;

    private ConstructionPile currentPile = null;

    private Node3D lightEffect;

    public ConstructionPile GetCurrentPile()
    {
        return currentPile;
    }

    public interface IBuilderSelectionCallback
    {
        public void OnSelect(Builder bay);
        public void OnDeselect(Builder bay);
    }

    public int Team = 0;

    [Export]
    public string BuildMenuName = "Build Structures";

    public override void _Ready()
    {
        base._Ready();
        lightEffect = FindChild("LightEffect") as Node3D;
    }

    public IEnumerable<UnitStats> GetAvailableUnits()
    {
        foreach (var unit in availableUnits) {
            yield return unit;
        }
    }
    public float GetProgress()
    {
        if (currentPile == null) {
            return 0;
        }
        return currentPile.ConstructionProgress / currentPile.Unit.BuildTime;
    }

    public bool StartBuilding(string name, Vector3 pos)
    {
        UnitStats stat = GetUnit(name);
        if (stat == null) {
            GD.PrintErr($"Unit with name {name} does not exist.");
            return false;
        }

        ConstructionPile pile = stat.CreateConstructionPile(Game.Get(), Team);
        pile.GlobalPosition = pos;
        pile.ForceSetPosition(pile.GlobalPosition);
        pile.Depth = stat.DefaultDepth;
        pile.Height = stat.DefaultHeight;
        Game.Get().GetPlanet().AddBuilding(pile, pile.GlobalPosition);
        GD.Print("Added unit to queue.");
        if (currentPile == null) {
            currentPile = pile;
        }
        return true;
    }


    public UnitStats GetUnit(string name)
    {
        return availableUnits.FirstOrDefault(stat => stat.Name == name);
    }

    public void BuildUnit()
    {
        if (currentPile == null) {
            return;
        }
        Unit created = currentPile.BuildUnit();
        currentPile = null;
    }

    public bool IsBuilding()
    {
        return currentPile != null && (GlobalPosition - currentPile.GlobalPosition).Length() < buildRadius;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (currentPile != null && currentPile.NativeInstance.ToInt64() == 0x0) {
            currentPile = null;
        }
        if (IsBuilding()) {
            if (lightEffect != null) {
                lightEffect.Visible = true;
            }
            float progress = GetProgress();
            if (progress > 1.0f && currentPile.Unit.BuildCost < Game.Get().GetTeam(Team).Resources) {
                BuildUnit();
            } else {
                currentPile.AddProgress((float)(delta * buildSpeed));
            }
        } else {
            if (lightEffect != null) {
                lightEffect.Visible = false;
            }
        }
    }

    public ConstructionPile SearchNearestPile()
    {
        ConstructionPile closest = null;
        float closestDist = float.MaxValue;
        foreach (ConstructionPile pile in Game.FindChildrenRecursive<ConstructionPile>(Game.Get())) {
            if (pile.Team == Team) {
                float dist = (pile.GlobalPosition - GlobalPosition).LengthSquared();
                if (dist < closestDist) {
                    closestDist = dist;
                    closest = pile;
                }
            }
        }
        return closest;
    }

    public void SetToNearestPile()
    {
        currentPile = SearchNearestPile();
    }

    public void SetTeam(int team)
    {
        Team = team;
    }

    public int GetTeam()
    {
        return Team;
    }

    public void OnDeselect()
    {
        foreach (var selector in Game.FindInterfacesRecursive<IBuilderSelectionCallback>(Game.Get())) {
            selector.OnDeselect(this);
        }
    }

    public void OnSelect()
    {
        foreach (var selector in Game.FindInterfacesRecursive<IBuilderSelectionCallback>(Game.Get())) {
            selector.OnSelect(this);
        }
    }

    public void CancelBuilding()
    {
        if (currentPile != null) {
            currentPile.Destroy();
        } 
    }

}
