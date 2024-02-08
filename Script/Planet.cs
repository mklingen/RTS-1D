using Godot;
using System;

public partial class Planet : MeshInstance3D
{
    [Export]
    public float Radius = 1.0f;


    public Vector3 ProjectToSurface(Vector3 worldPos, float height = 0)
    {
        return ToGlobal(ToLocal(worldPos).Normalized() * (Radius + height));
    }

    public Vector3 ProjectToCylinder(Vector3 worldPos, float height, float depth)
    {
        Vector3 local = ToLocal(worldPos);
        float r = Radius + height;
        float theta = Mathf.Atan2(local.Y, local.X);
        return ToGlobal(new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), -depth));
    }


    public float GetLatitude(Vector3 worldPos)
    {
        Vector3 local = ToLocal(worldPos);
        return Mathf.Atan2(local.Y, local.X);
    }

    public bool Collides(Vector3 worldPos)
    {
        Vector3 local = ToLocal(worldPos);
        return local.Length() < Radius;
    }

    // Represents a tangent frame when looking at the planet from facing up its z axis.
    // The tangent frame is always pointed such that "up" points out of the planet.
    public struct TangentFrame
    {
        // Points out of the planet.
        public Vector3 Up;
        // Points to the right when "up" is pointing out of the planet.
        public Vector3 Right;
        // POints to the left when "up" is pointing out of the planet.
        public Vector3 Left;
        // Points along the planet's negative z axis.
        public Vector3 In;
        // Points along the planet's z axis.
        public Vector3 Out;

        public Basis ToBasis(float scale = 1.0f)
        {
            return new Basis(Left * scale, Up * scale, In * scale);
        }
    }

    public TangentFrame GetTangentFrame(Vector3 worldPos)
    {
        Vector3 local = ToLocal(worldPos).Normalized();

        // Up vector is the normalized position vector
        Vector3 up = local;

        // In vector is the negative of the global Z axis
        Vector3 In = -Basis.Z.Normalized();

        // Right vector is the cross product of In and Up (normalized)
        Vector3 right = In.Cross(up).Normalized();

        // Left vector is the negative of the Right vector
        Vector3 left = -right;


        Vector3 Out = -In;
        return new TangentFrame
        {
            Up = up,
            Right = right,
            Left = left,
            In = In,
            Out = Out
        };
    }

}
