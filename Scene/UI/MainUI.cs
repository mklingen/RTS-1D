using Godot;
using System;

public partial class MainUI : Control
{
    public interface IMainInputCallback
    {
        public void OnEvent(InputEvent inputEvent);
        public void OnMouseEnter();
        public void OnMouseLeave();
    }
    [Export]
    private int PlayerTeam = 0;

    private Label resourcesLabel;
    private string resourcesFormat;

    public override void _Ready()
    {
        base._Ready();
        MouseEntered += MainUI_MouseEntered;
        MouseExited += MainUI_MouseExited;
        resourcesLabel = FindChild("ResourcesLabel") as Label;
        Game.Get().GetTeam(PlayerTeam).OnResourcesChanged += MainUI_OnResourcesChanged;
        resourcesFormat = resourcesLabel.Text;
        MainUI_OnResourcesChanged(Game.Get().GetTeam(PlayerTeam).Resources);
    }

    private void MainUI_OnResourcesChanged(float resources)
    {
        resourcesLabel.Text = string.Format(resourcesFormat, (int)resources);
        if ((int)resources == 0) {
            resourcesLabel.Modulate = new Color(1, 0, 0);
        } else {
            resourcesLabel.Modulate = new Color(1, 1, 1);
        }
    }

    private void MainUI_MouseExited()
    {
        foreach (var input in Game.FindInterfacesRecursive<IMainInputCallback>(Game.Get())) {
            input.OnMouseLeave();
        }

    }

    private void MainUI_MouseEntered()
    {
        foreach (var input in Game.FindInterfacesRecursive<IMainInputCallback>(Game.Get())) {
            input.OnMouseEnter();
        }

    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        base._GuiInput(inputEvent);
        foreach (var input in Game.FindInterfacesRecursive<IMainInputCallback>(Game.Get())) {
            input.OnEvent(inputEvent);
        }

    }
}
