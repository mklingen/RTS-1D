using Godot;
using System;
using System.Linq;

public partial class QueueItem : PanelContainer
{
    public ProgressBar Progress;
    public Button Button;

    public override void _Ready()
    {
        base._Ready();
        Progress = Game.FindChildrenRecursive<ProgressBar>(this).FirstOrDefault();
        Button = Game.FindChildrenRecursive<Button>(this).FirstOrDefault();
    }

}
