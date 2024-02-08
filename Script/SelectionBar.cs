using Godot;
using System;

public partial class SelectionBar : Sprite3D, Selectable.ISelectionInterface
{
    public void OnDeselect()
    {
        Visible = false;
        Hide();
    }

    public void OnSelect()
    {
        Visible = true;
        Show();
    }

    public override void _Ready()
    {
        base._Ready();
        Visible = false;
    }
}
