using Godot;
using System;

public partial class OffsetRoot : Node3D, PlanetObject.ISetRandomOffset
{
    private Vector3 localPosOnStart;
    public override void _Ready()
    {
        base._Ready();
        localPosOnStart = new Vector3(Position.X, Position.Y, Position.Z);
    }
    public void SetRandomOffset(Planet.TangentFrame tangentFrame, Vector3 tangentDiff)
    {
        Vector3 globalDiff = tangentFrame.Left * tangentDiff.X + tangentFrame.In * tangentDiff.Y;
        Position = localPosOnStart + Transform.Basis.GetRotationQuaternion().Inverse() * globalDiff;
    }
}
