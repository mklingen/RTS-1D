using Godot;
using System;

// Controls the player placing structures.
public partial class StructurePlacement : Node, ITool
{
    [Export]
    private StructureCursor structureCursor;

    [ExportGroup("Colors")]
    private Color goodColor = new Color(0, 0, 1);

    private Color badColor = new Color(1, 0, 0);

    public override void _Ready()
    {
        base._Ready();
    }

    public void OnActivate()
    {
        if (structureCursor != null) {
            structureCursor.Visible = true;
        }
    }

    public void OnDeactivate()
    {
        if (structureCursor != null) {
            structureCursor.Visible = false;
        }
    }

    public void OnMouseClick(Vector2 mousePixels, Vector3 mousePos, ITool.MouseButton click)
    {
        switch (click) {
            case ITool.MouseButton.Left:
                structureCursor.SetColor(badColor);
                break;
            case ITool.MouseButton.Right:
                structureCursor.SetColor(new Color(1, 1, 1));
                break;
        }
    }

    public void OnMouseFirstPressed(Vector2 mousePixels, Vector3 mousePos, ITool.MouseButton click)
    {

    }

    public void OnMouseMove(Vector2 mouseScreenPos, Vector3 mouseWorldPos)
    {
        if (structureCursor == null) {
            return;
        }
        structureCursor.ForceSetPosition(mouseWorldPos);
        structureCursor.SetColor(goodColor);
    }
}
