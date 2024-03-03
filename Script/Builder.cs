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
        public Node3D Pile;
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

        Node3D pile = stat.CreateConstructionPile(Game.Get());
        pile.GlobalPosition = pos;
        if (pile is PlanetObject) {
            var planetObject = pile as PlanetObject;
            planetObject.ForceSetPosition(pile.GlobalPosition);
            planetObject.Depth = stat.DefaultDepth;
            planetObject.Height = stat.DefaultHeight;
            Game.Get().GetPlanet().AddBuilding(planetObject, planetObject.GlobalPosition);
        }
        constructionQueue.Add(new BuildingBluePrint { Unit = stat, Location = pos, Pile = pile});

        GD.Print("Added unit to queue.");
        return true;
    }

    public void EnterCurrentBuildingZone()
    {
        if (constructionQueue.Count == 1) {
            startedBuildingAt = Game.GetTime();
        }
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
            created.Depth = unit.Unit.DefaultDepth;
            created.Height = unit.Unit.DefaultHeight;
            created.Team = Team;
            created.MaybeAddBuilding();
            Game.Get().GetTeam(Team).Resources -= unit.Unit.BuildCost;
            GD.Print("Created unit.");
            unit.Pile.QueueFree();
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
            var front = constructionQueue.First();
            Game.Get().GetPlanet().RemoveBuilding(front.Pile as PlanetObject);
            front.Pile.QueueFree();
            constructionQueue.RemoveAt(0);
            if (constructionQueue.Count > 0) {
                startedBuildingAt = Game.GetTime();
            }
            return true;
        }
        if (idx > 0 && idx < constructionQueue.Count) {
            var item = constructionQueue.ElementAt(idx);
            Game.Get().GetPlanet().RemoveBuilding(item.Pile as PlanetObject);
            item.Pile.QueueFree();
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
