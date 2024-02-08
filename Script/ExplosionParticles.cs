using Godot;
using System;

public partial class ExplosionParticles : CpuParticles3D
{
    public override void _Ready()
    {
        base._Ready();
        Emitting = true;
    }
}
