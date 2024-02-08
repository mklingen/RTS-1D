using Godot;
using System;

public partial class ResourceDropOff : Node3D, Unit.ISetTeam, Game.ITeamObject
{
    int team;
    public int Team { get { return team; } set { team = value; } }

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
