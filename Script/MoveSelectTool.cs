using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MoveSelectTool : Node, ITool
{
    // Selection rectangle object.
    private Panel selectionRect;

    // If true we were pressing the primary click button on the last frame.
    private bool wasPreviouslyPressingPrimaryClick = false;
    // Where we started clicking.
    private Vector2 clickStart;
    // Where we ended clicking.
    private Vector2 clickEnd;
    // List of things the player is selecting.
    private List<Selectable> tempSelectionBuffer = new List<Selectable>();
    // Things the player has selected.
    private List<Selectable> selectionBuffer = new List<Selectable>();

    // An object we are using to indicate a go-to-target.
    private PlanetObject goToGizmo;

    // Defines the curve that pops the go-to gizmo in and out.
    [Export]
    private Curve goToGizmoPop;

    private MainCamera mainCamera;

    public int Team = 0;
    // Randomly offset move commands by this amount.
    [Export]
    private float moveRandomization = 0.05f;

    [Signal]
    public delegate void SelectionChangedEventHandler(Array<Selectable> selectables);


    public override void _Ready()
    {
        base._Ready();
        mainCamera = Game.FindChildrenRecursive<MainCamera>(Game.Get()).FirstOrDefault();
        goToGizmo = Game.Get().FindChild("GoToGizmo") as PlanetObject;
        goToGizmo.OrientMode = PlanetObject.OrientationMode.Auto;
        selectionRect = Game.Get().FindChild("SelectionRect") as Panel;
        if (selectionRect == null) {
            GD.PrintErr("No selection rect.");
        }
        else {
            selectionRect.Visible = false;
        }
    }

    public int GetPriority()
    {
        return 0;
    }

    public string GetName()
    {
        return "Move/Select";
    }


    public void OnActivate()
    {

    }

    public void OnDeactivate()
    {

    }

    private void PopupGotoGizmo(Vector3 pos)
    {
        goToGizmo.ForceSetPosition(pos);
        goToGizmo.GetChild<TransformPopper>(0).Transition(TransformPopper.State.PoppingIn);
    }

    public void OnMouseClick(Vector2 mousePixels, Vector3 mousePos, ITool.MouseButton click)
    {
        if (click == ITool.MouseButton.Right) {
            PopupGotoGizmo(mousePos);
            foreach (var selectable in selectionBuffer) {
                if (selectable.NativeInstance.ToInt64() == 0x0) {
                    continue;
                }
                Unit selectableUnit = Game.FindParent<Unit>(selectable);
                if (selectableUnit == null) {
                    continue;
                }
                if (!selectableUnit.Stats.CanMove()) {
                    continue;
                }
                selectableUnit.GoTo(mousePos + Game.RandomVector3(moveRandomization));
            }
        }
    }

    public void OnMouseFirstPressed(Vector2 mousePixels, Vector3 mousePos, ITool.MouseButton click)
    {

    }

    public void OnMouseMove(Vector2 mousePixels, Vector3 mousePos)
    {
        HandleSelection(mousePixels, Input.IsActionPressed("PrimaryClick"));
    }


    private void HandleSelection(Vector2 mousePos, bool isPressingPrimaryClick)
    {
        selectionRect.Visible = isPressingPrimaryClick;
        if (isPressingPrimaryClick) {
            if (!wasPreviouslyPressingPrimaryClick) {
                clickStart = mousePos;
                selectionRect.Position = clickStart;
            }
            Vector2 size = mousePos - clickStart;
            if (size.X < 0) {
                size.X = clickStart.X - mousePos.X;
                selectionRect.Position = new Vector2(mousePos.X, selectionRect.Position.Y);
            }
            if (size.Y < 0) {
                size.Y = clickStart.Y - mousePos.Y;
                selectionRect.Position = new Vector2(selectionRect.Position.X, mousePos.Y);
            }
            size = new Vector2(Mathf.Max(size.X, 5), Mathf.Max(size.Y, 5));
            selectionRect.Size = size;
            clickEnd = mousePos;
            UpdateSelectionBuffer();
        }
        else if (wasPreviouslyPressingPrimaryClick) {
            // Do selection.
            foreach (var select in selectionBuffer) {
                if (select.NativeInstance.ToInt64() == 0) {
                    continue;
                }
                if (!tempSelectionBuffer.Contains(select)) {
                    select.OnDeselect();
                }
            }
            foreach (var select in tempSelectionBuffer) {
                select.OnSelect();
            }
            selectionBuffer = tempSelectionBuffer;
        }
        wasPreviouslyPressingPrimaryClick = isPressingPrimaryClick;
        var selectableArray = new Godot.Collections.Array<Selectable>();
        selectableArray.AddRange(selectionBuffer);
        EmitSignal(SignalName.SelectionChanged, selectableArray);
    }

    private void UpdateSelectionBuffer()
    {
        Vector2 center = clickStart * 0.5f + clickEnd * 0.5f;
        Vector2 size = clickEnd - clickStart;
        tempSelectionBuffer = new List<Selectable>();
        float x = center.X - MathF.Abs(size.X * 0.5f);
        float y = center.Y - MathF.Abs(size.Y * 0.5f);
        var selectionRect = new Rect2(x, y, Mathf.Abs(size.X), Mathf.Abs(size.Y));
        foreach (Selectable selectable in Game.FindChildrenRecursive<Selectable>(Game.Get())) {
            Vector2 screenPos = mainCamera.UnprojectPosition(selectable.GlobalPosition);
            Rect2 fudgeRect = new Rect2(screenPos - new Vector2(selectable.BoxSize, selectable.BoxSize), new Vector2(selectable.BoxSize * 2, selectable.BoxSize * 2));
            if (selectionRect.Intersects(fudgeRect)) {
                var unit = Game.GetRootEntity(Game.Get(), selectable) as Unit;
                if (unit != null && unit.Team == 0) {
                    tempSelectionBuffer.Add(selectable);
                }
            }
        }
    }

    public UnitStats.Abilities GetAbilities()
    {
        return UnitStats.Abilities.Move;
    }
}
