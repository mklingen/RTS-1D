using Godot;
using System;
using System.Collections.Generic;

public partial class ResourceDropOff : Node3D, Unit.ISetTeam, Game.ITeamObject
{
    int team;
    public int Team { get { return team; } set { team = value; } }

    [ExportGroup("Collectors")]
    [Export] private int maxNumCollectors = 2;
    [Export(PropertyHint.ResourceType, "UnitStats")] private UnitStats collectorStats;
    [Export] private float collectorBuildSpeed = 1;
    private float startedBuildingCollectorOn = -1;
    private List<Unit> collectors = new List<Unit>();


    private bool everBuilt = false;
    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        // Delete dead collectors.
        collectors.RemoveAll(c => c == null || c.NativeInstance.ToInt64() == 0x0 || c.IsQueuedForDeletion());
        // Collectors can be destroyed, so build new ones when we need it. They will be free.
        if (collectors.Count < maxNumCollectors) {
            MaybeBuildCollector();
        }
    }

    private void MaybeBuildCollector()
    {
        if (startedBuildingCollectorOn < 0) {
            startedBuildingCollectorOn = Game.GetTime();
        }
        float now = Game.GetTime();
        float buildTime = (now - startedBuildingCollectorOn) * collectorBuildSpeed;
        // If we have fully built a collector, create it.
        if (buildTime > collectorStats.BuildTime || !everBuilt) {
            BuildCollector();
        }
    }

    private void BuildCollector()
    {
        startedBuildingCollectorOn = -1;
        var collector = collectorStats.CreateUnit(Game.Get());
        collector.ForceSetPosition(GlobalPosition, true);
        collector.GoTo(GlobalPosition + Game.RandomVector3(0.1f));
        collector.Team = team;
        collectors.Add(collector);
        everBuilt = true;
    }
    public int GetTeam()
    {
        return team;
    }

    public void SetTeam(int team)
    {
        this.team = team;
    }

    public void AddResources(float resources)
    {
        Game.Get().GetTeam(team).Resources += resources;
    }
}
