using Godot;
using System;

public partial class ConstructionPile : PlanetObject
{
    [Export]
    public UnitStats Unit;
    public int Team;
    public float ConstructionProgress;


    public void OnCreate()
    {
        Game.Get().GetTeam(Team).ConstructionPiles.Add(this);
    }

    public bool IsDone()
    {
        return ConstructionProgress > Unit.BuildTime;
    }

    public void AddProgress(float dt)
    {
        ConstructionProgress += dt;
    }

    public Unit BuildUnit()
    {
        Unit created = Unit.CreateUnit(Game.Get());
        if (created != null) {
            created.GlobalRotation = GlobalRotation;
            created.ForceSetPosition(GlobalPosition, true);
            created.Depth = Unit.DefaultDepth;
            created.Height = Unit.DefaultHeight;
            created.Team = Team;
            created.AddToGrid();
            Game.Get().GetTeam(Team).Resources -= Unit.BuildCost;
            GD.Print("Created building.");
            Destroy();
        }
        else {
            GD.PrintErr("Unit was null.");
        }
        return created;
    }

    public void Destroy()
    {
        Game.Get().GetTeam(Team).ConstructionPiles.Remove(this);
        QueueFree();
    }

}
