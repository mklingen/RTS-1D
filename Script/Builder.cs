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
    private int queueSize = 5;

    [Export]
    private float buildSpeed = 1.0f;

    public struct BuildingBluePrint
    {
        public UnitStats Unit;
        public Vector3 Location;
    }

    public interface IBuilderSelectionCallback
    {
        public void OnSelect(Builder bay);
        public void OnDeselect(Builder bay);
    }

    private List<BuildingBluePrint> constructionQueue = new List<BuildingBluePrint>();

    private float startedBuildingAt = 0;

    public int Team = 0;

    [Export]
    public string BuildMenuName = "Build Structures";

    public override void _Ready()
    {
        base._Ready();
    }

    public IEnumerable<UnitStats> GetAvailableUnits()
    {
        foreach (var unit in availableUnits) {
            yield return unit;
        }
    }

    public IEnumerable<BuildingBluePrint> GetQueue()
    {
        foreach (var unit in constructionQueue) {
            yield return unit;
        }
    }

    public float GetProgress()
    {
        if (constructionQueue.Count == 0) {
            return 0.0f;
        }
        float time = Game.GetTime();
        float alpha = (time - startedBuildingAt) / (constructionQueue.First().Unit.BuildTime / buildSpeed);
        return alpha;
    }

    public bool StartBuilding(string name, Vector3 pos)
    {
        UnitStats stat = GetUnit(name);
        if (stat == null) {
            GD.PrintErr($"Unit with name {name} does not exist.");
            return false;
        }
        if (constructionQueue.Count > queueSize) {
            GD.Print($"Queue size exceeded.");
            return false;
        }
        constructionQueue.Add(new BuildingBluePrint { Unit = stat, Location = pos});
        GD.Print("Added unit to queue.");
        if (constructionQueue.Count == 1) {
            startedBuildingAt = Game.GetTime();
        }
        return true;
    }

    public UnitStats GetUnit(string name)
    {
        return availableUnits.FirstOrDefault(stat => stat.Name == name);
    }

    public void BuildUnit()
    {
        if (constructionQueue.Count == 0) {
            GD.PrintErr("Queue is empty.");
            return;
        }
        BuildingBluePrint unit = constructionQueue.First();
        constructionQueue.RemoveAt(0);
        Unit created = unit.Unit.CreateUnit(Game.Get());
        if (created != null) {
            created.GlobalRotation = GlobalRotation;
            created.ForceSetPosition(unit.Location, true);
            created.Team = Team;
            Game.Get().GetTeam(Team).Resources -= unit.Unit.BuildCost;
            GD.Print("Created unit.");
        }
        else {
            GD.PrintErr("Unit was null.");
        }

        if (constructionQueue.Count > 0) {
            startedBuildingAt = Game.GetTime();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (constructionQueue.Count > 0) {
            float progress = GetProgress();
            if (progress > 1.0f && constructionQueue[0].Unit.BuildCost < Game.Get().GetTeam(Team).Resources) {
                BuildUnit();
            }
        }
    }

    public void SetTeam(int team)
    {
        Team = team;
    }

    public bool ClearQueue(int idx)
    {
        // If index is 0, the current unit is canceled.
        if (idx == 0) {
            constructionQueue.RemoveAt(0);
            if (constructionQueue.Count > 0) {
                startedBuildingAt = Game.GetTime();
            }
            return true;
        }
        if (idx > 0 && idx < constructionQueue.Count) {
            // Otherwise, some othe runit is canceled.
            constructionQueue.RemoveAt(idx);
            return true;
        }
        return false;
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
}
