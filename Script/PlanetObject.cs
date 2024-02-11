using Godot;
using System;

public partial class PlanetObject : Node3D
{
    [Export]
    public float Height;
    [Export]
    public float Depth;
    [Export]
    public Vector3 GlobalVelocity;
    protected Planet planet;
    private float radius3d;

    [Export]
    private bool setNominalHeightOnStart = false;

    public enum OrientationMode
    {
        Velocity,
        Auto
    }

    [Export]
    public OrientationMode OrientMode;


    private Planet GetPlanet()
    {
        if (planet == null) {
            planet = Game.Get().GetPlanet();
        }
        return planet;
    }

    public override void _Ready()
    {
        base._Ready();
        float nominalHeight = Height;
        ForceSetPosition(GlobalPosition);
        if (setNominalHeightOnStart) {
            Height = nominalHeight;
        }

    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        Vector3 globalPosition = GlobalPosition;
        Vector3 nextPosition = GetPlanet().ProjectToCylinder(globalPosition + GlobalVelocity * (float)delta, Height, Depth);
        switch (OrientMode) {
            case OrientationMode.Velocity: {
                    if (GlobalVelocity.LengthSquared() > 1e-6) {
                        LookAt(nextPosition, planet.GetTangentFrame(GlobalPosition).Up);
                    }
                    break;
            }
            case OrientationMode.Auto: {
                    AutoOrient();
                    break;
                }
        }
        GlobalPosition = GetPlanet().ProjectToSurface(nextPosition, radius3d);
    }

    public void AutoOrient()
    {
        Planet.TangentFrame tangent = planet.GetTangentFrame(GlobalPosition);
        Vector3 target = GlobalPosition + tangent.Right;
        if (!GlobalPosition.IsEqualApprox(target)) {
            LookAt(GlobalPosition + tangent.Right, tangent.Up);
        }
    }

    public void ForceSetPosition(Vector3 globalPos)
    {
        this.GlobalPosition = globalPos;
        Vector3 local = GetPlanet().ToLocal(GlobalPosition);
        float length2d = Mathf.Sqrt(local.X * local.X + local.Y * local.Y);
        Height = length2d - GetPlanet().Radius;
        radius3d = local.Length() - GetPlanet().Radius;
        Depth = -local.Z;
    }

}
