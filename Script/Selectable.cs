using Godot;
using System;

public partial class Selectable : Node3D
{
    [Export]
    public int BoxSize = 10;

    [Export]
    public bool FaceCamera = true;

    private MainCamera camera;

    public override void _Ready()
    {
        base._Ready();
        camera = MainCamera.Get();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (FaceCamera && IsVisibleInTree()) {
            LookAt(camera.GlobalPosition, camera.GetPlanet().GetTangentFrame(GlobalPosition).Up, true);
        }
    }

    public interface ISelectionInterface
    {
        public void OnDeselect();
        public void OnSelect();
    }

    public void OnDeselect()
    {
        if (NativeInstance.ToInt64() == 0x0) {
            return;
        }
        foreach (var selectable in Game.FindInterfacesRecursive<ISelectionInterface>(GetParent())) {
            selectable.OnDeselect();
        }
    }

    public void OnSelect()
    {
        if (NativeInstance.ToInt64() == 0x0) {
            return;
        }
        foreach (var selectable in Game.FindInterfacesRecursive<ISelectionInterface>(GetParent())) {
            selectable.OnSelect();
        }
    }
}
