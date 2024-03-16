using Godot;
using System;
using System.Linq;

public partial class BuildMenu : Control
{
    [ExportGroup("Prefabs")]
    [Export]
    private PackedScene QueueItemPrefab;

    [Export]
    private PackedScene BuildButtonPrefab;

    private ConstructionBay constructionBay;

    private Control buildQueueContainer;
    private Control buttonContainer;
    private CheckButton repeatButton;
    private int numQueueItems = 0;
    private QueueItem headItem = null;
    private Label mainLabel = null;

    public override void _Ready()
    {
        base._Ready();
        buildQueueContainer = FindChild("BuildQueue") as Control;
        buttonContainer = FindChild("BuildButtons") as Control;
        repeatButton = FindChild("RepeatButton") as CheckButton;
        mainLabel = FindChild("MainLabel") as Label;
        repeatButton.Toggled += RepeatButton_Toggled;
    }

    private void RepeatButton_Toggled(bool buttonPressed)
    {
        constructionBay.DoRepeat = buttonPressed;
    }

    private void UpdateUnitButtons()
    {
        foreach (var unit in constructionBay.GetAvailableUnits()) {
            Button button = BuildButtonPrefab.Instantiate() as Button;
            (button.GetChild(0) as Label).Text = $"{unit.Name}\n{(int)(unit.BuildCost)}RP";
            button.ButtonUp += () => BuildButtonUp(unit.Name);
            button.TooltipText = $"Build a {unit.Name} for {(int)(unit.BuildCost)} RP.";
            buttonContainer.AddChild(button);
        }
    }

    private void UpdateConstructionQueue()
    {
        Game.ClearChildren(buildQueueContainer);
        int idx = 0;
        foreach (var unit in constructionBay.GetQueue()) {
            QueueItem item = QueueItemPrefab.Instantiate() as QueueItem;
            buildQueueContainer.AddChild(item);
            item.Progress.Hide();
            int local = idx;
            item.Button.ButtonUp += () => QueueButtonUp(local);
            item.TooltipText = "Click to cancel building.";
            if (idx == 0) {
                headItem = item;
            }
            idx++;
        }
        numQueueItems = idx;
    }

    private void QueueButtonUp(int index)
    {
        constructionBay.ClearQueue(index);
    }

    public void Open(ConstructionBay bay)
    {
        constructionBay = bay;
        mainLabel.Text = bay.BuildMenuName;
        Game.ClearChildren(buildQueueContainer);
        Game.ClearChildren(buttonContainer);

        UpdateUnitButtons();
        UpdateConstructionQueue();
        Show();
    }

    private void BuildButtonUp(string unitName)
    {
        if (constructionBay.StartBuilding(unitName)) {
            UpdateConstructionQueue();
        }
    }

    public void Close()
    {
        Hide();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (constructionBay == null || constructionBay.NativeInstance.ToInt64() == 0x0) {
            Close();
            return;
        }
        if (numQueueItems != constructionBay.GetQueue().Count()) {
            UpdateConstructionQueue();
        }
        if (numQueueItems > 0 && headItem != null) {
            headItem.Progress.Show();
            headItem.Progress.TooltipText = $"{(int)(constructionBay.GetProgress() * 100)}% complete.";
            headItem.Progress.Value = (int)(constructionBay.GetProgress() * 100);
        }
    }
}
