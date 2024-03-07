using Godot;
using System;

public partial class ConstructionPile : PlanetObject
{
    [Export]
    public UnitStats Unit;
    public int Team;
    public float ConstructionProgress;


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
            created.MaybeAddBuilding();
            Game.Get().GetTeam(Team).Resources -= Unit.BuildCost;
            GD.Print("Created unit.");
            QueueFree();
        }
        else {
            GD.PrintErr("Unit was null.");
        }

        return created;
    }

    public void Destroy()
    {
        QueueFree();
    }

}
