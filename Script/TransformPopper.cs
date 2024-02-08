using Godot;
using System;
using System.Diagnostics;

public partial class TransformPopper : Node3D
{
    [ExportGroup("Pop In")]
    [Export]
    private Curve popInCurve;
    [Export]
    private float popInTime;
    [ExportGroup("Pop Out")]
    [Export]
    private Curve popOutCurve;
    [Export]
    private float popOutTime;
    [ExportGroup("Stay")]
    [Export]
    private float stayTime;
    private Vector3 scale0;

    public enum State
    {
        PoppingIn,
        Visible,
        PoppingOut,
        Invisible
    }

    private State state;

    private float timeStateChanged;
    private float alpha;

    public override void _Ready()
    {
        base._Ready();
        scale0 = Scale;
        timeStateChanged = Game.GetTime();
        this.Visible = false;
        state = State.Invisible;
    }

    // Return a scalar between 0 and 1 representing the time since the state changed.
    private float GetAlpha(float t, float maxTime)
    {
        return (t - timeStateChanged) / maxTime;
    }

    private float Sample(float t, float maxTime, Curve curve)
    {
        alpha = GetAlpha(t, maxTime);
        return curve.Sample(alpha);
    }

    private void DoScale(float curve)
    {
        Scale = scale0 * curve;
    }
    
    public void Transition(State state)
    {
        this.state = state;
        timeStateChanged = Game.GetTime();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        float t = Game.GetTime();
        switch (state) {
            case State.PoppingIn: {
                    this.Visible = true;
                    DoScale(Sample(t, popInTime, popInCurve));
                    if (alpha > 1.0f) {
                        Transition(State.Visible);
                    }
                    break;
             }
            case State.Visible: {
                    this.Visible = true;
                    if (stayTime > 0) {
                        alpha = GetAlpha(t, stayTime);
                        if (alpha > 1.0f) {
                            Transition(State.PoppingOut);
                        }
                    }
                    break;
            }
            case State.Invisible: {
                    this.Visible = false;
                    break;
            }
            case State.PoppingOut: {
                    this.Visible = true;
                    DoScale(Sample(t, popOutTime, popOutCurve));
                    if (alpha > 1.0f) {
                        Transition(State.Invisible);
                    }
                    break;
                }
        }
    }
}
