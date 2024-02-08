using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ConstructionBay : Node3D, Unit.ISetTeam, Selectable.ISelectionInterface
{
    [ExportGroup("Construction")]
    [Export]
    private UnitStats[] availableUnits = new UnitStats[0];

    [Export]
    private int queueSize = 5;

    [Export]
    private float buildSpeed = 1.0f;

    private List<UnitStats> constructionQueue = new List<UnitStats>();

    private float startedBuildingAt = 0;

    public int Team = 0;

    public bool DoRepeat = false;

    [Export]
    public string BuildMenuName = "Construction Bay";

    public override void _Ready()
    {
        base._Ready();
        StartBuilding("Tank");
    }

    public interface IConstructionBaySelectionCallback
    {
        public void OnSelect(ConstructionBay bay);
        public void OnDeselect(ConstructionBay bay);
    }

    public IEnumerable<UnitStats> GetAvailableUnits()
    {
        foreach (var unit in availableUnits) {
            yield return unit;
        }
    }

    public IEnumerable<UnitStats> GetQueue()
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
        float alpha = (time - startedBuildingAt) / (constructionQueue.First().BuildTime / buildSpeed);
        return alpha;
    }

    public bool StartBuilding(string name)
    {
        UnitStats stat = availableUnits.FirstOrDefault(stat => stat.Name == name);
        if (stat == null) {
            GD.PrintErr($"Unit with name {name} does not exist.");
            return false;
        }
        if (constructionQueue.Count > queueSize) {
            GD.Print($"Queue size exceeded.");
            return false;
        }
        constructionQueue.Add(stat);
        GD.Print("Added unit to queue.");
        if (constructionQueue.Count == 1) {
            startedBuildingAt = Game.GetTime();
        }
        return true;
    }

    public void BuildUnit()
    {
        if (constructionQueue.Count == 0) {
            GD.PrintErr("Queue is empty.");
            return;
        }
        UnitStats unit = constructionQueue.First();
        constructionQueue.RemoveAt(0);
        Unit created = unit.CreateUnit(Game.Get());
        if (created != null) {
            created.GlobalRotation = GlobalRotation;
            created.ForceSetPosition(GlobalPosition);
            created.Team = Team;
            Game.Get().GetTeam(Team).Resources -= unit.BuildCost;
            GD.Print("Created unit.");
        } else {
            GD.PrintErr("Unit was null.");
        }

        if (constructionQueue.Count > 0) {
            startedBuildingAt = Game.GetTime();
        }
        if (DoRepeat) {
            constructionQueue.Add(unit);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (constructionQueue.Count > 0) {
            float progress = GetProgress();
            if (progress > 1.0f && constructionQueue[0].BuildCost < Game.Get().GetTeam(Team).Resources) {
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

    public void OnDeselect()
    {
        foreach (var selector in Game.FindInterfacesRecursive<IConstructionBaySelectionCallback>(Game.Get())) {
            selector.OnDeselect(this);
        }
    }

    public void OnSelect()
    {
        foreach (var selector in Game.FindInterfacesRecursive<IConstructionBaySelectionCallback>(Game.Get())) {
            selector.OnSelect(this);
        }
    }

    public int GetTeam()
    {
        return Team;
    }
}
