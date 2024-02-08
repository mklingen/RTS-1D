using Godot;
using System;

public partial class DieAfterTimer : Node3D
{
    [Export] float lifeTime = 0.5f;
    private float startTime;
    public override void _Ready()
    {
        base._Ready();
        startTime = Game.GetTime();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Game.GetTime() - startTime > lifeTime) {
            QueueFree();
        }
    }
}
