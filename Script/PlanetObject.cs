using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class PlanetObject : Node3D
{
    [ExportGroup("Coordinates")]
    [Export]
    public float Height;
    [Export]
    public float Depth;
    [ExportGroup("Velocity")]
    [Export]
    public Vector3 GlobalVelocity;
    protected Planet planet;
    private float radius3d;

    [Export]
    private bool setNominalHeightOnStart = false;

    public interface ISetRandomOffset
    {
        public void SetRandomOffset(Planet.TangentFrame frame, Vector3 tangentDiff);
    }

    public enum OrientationMode
    {
        Velocity,
        Auto
    }

    [ExportCategory("Orientation")]
    [Export]
    public OrientationMode OrientMode;


    [ExportCategory("Randomization")]
    [Export]
    private float startRandomOffset = 0.00f;
    private Vector3 randomTangentOffset = Vector3.Zero;

    [Export]
    private float startDepthRandomOffset = 0.00f;

    private List<ISetRandomOffset> offsetSetters = new List<ISetRandomOffset>();

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
        if (startRandomOffset != 0.0f) {
            randomTangentOffset = new Vector3(GD.Randf() * startRandomOffset, GD.Randf() * startRandomOffset, 0.0f);
        }
        offsetSetters = Game.FindInterfacesRecursive<ISetRandomOffset>(this).ToList();
        float nominalHeight = Height;
        ForceSetPosition(GlobalPosition, true);
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
        GlobalPosition = nextPosition;
        ApplyOffsets();
    }

    private void ApplyOffsets()
    {
        if (startRandomOffset == 0.0f) {
            return;
        }
        foreach (var callback in offsetSetters) {
            callback.SetRandomOffset(planet.GetTangentFrame(GlobalPosition), randomTangentOffset);
        }
    }

    public void AutoOrient()
    {
        if (GlobalPosition.IsZeroApprox()) {
            return;
        }
        Planet.TangentFrame tangent = planet.GetTangentFrame(GlobalPosition);
        Vector3 target = GlobalPosition + tangent.In;
        if (!GlobalPosition.IsEqualApprox(target)) {
            LookAt(target, tangent.Up);
        }
    }

    public virtual void ForceSetPosition(Vector3 globalPos, bool randomizeDepth = false)
    {
        this.GlobalPosition = globalPos;
        Vector3 local = GetPlanet().ToLocal(GlobalPosition);
        float length2d = Mathf.Sqrt(local.X * local.X + local.Y * local.Y);
        Height = length2d - GetPlanet().Radius;
        radius3d = local.Length() - GetPlanet().Radius;
        Depth = -local.Z;
        if (randomizeDepth) {
            Depth += (float)GD.RandRange(-startDepthRandomOffset, startRandomOffset);
            this.GlobalPosition = GetPlanet().ProjectToCylinder(globalPos, Height, Depth);
        }
        if (this.OrientMode == OrientationMode.Auto) {
            AutoOrient();
        }
    }

}
