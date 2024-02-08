using Godot;
using System;

public partial class HealthBar : SelectionBar
{
    [Export]
    private Color highHealthColor;

    [Export]
    private Color midHealthColor;
    [Export]
    private float midHealthThreshold = 0.5f;

    [Export]
    private Color lowHealthColor;
    [Export]
    private float lowHealthThreshold = 0.1f;

    private Vector3 scaleInit;
    private Vector3 posInit;

    public override void _Ready()
    {
        base._Ready();
        scaleInit = Scale;
        posInit = Position;
        Visible = false;
        SetHealth(1.0f, 1.0f);
    }

    public void SetHealth(float health, float maxHealth)
    {
        float t = health / maxHealth;
        if (t < lowHealthThreshold) {
            Modulate = lowHealthColor;
        } else if (t < midHealthThreshold) {
            Modulate = midHealthColor;
        } else {
            Modulate = highHealthColor;
        }
        Scale = new Vector3(scaleInit.X * t, scaleInit.Y, scaleInit.Z);
        Position = new Vector3(posInit.X - (1.0f - t) * 0.015f, posInit.Y, posInit.Z);
    }
}
