using Godot;
using System;
using System.Linq;

public partial class BuildStructuresMenu : Control
{

    [ExportGroup("Prefabs")]
    [Export]
    private PackedScene QueueItemPrefab;

    [Export]
    private PackedScene BuildButtonPrefab;

    private Builder builder;
    private StructurePlacement structurePlacementTool;

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
        mainLabel = FindChild("MainLabel") as Label;
        structurePlacementTool = Game.FindChildrenRecursive<StructurePlacement>(Game.Get()).First();
    }
    private void UpdateUnitButtons()
    {
        foreach (var unit in builder.GetAvailableUnits()) {
            Button button = BuildButtonPrefab.Instantiate() as Button;
            button.Text = $"{unit.Name}\n{(int)(unit.BuildCost)}RP";
            button.ButtonUp += () => BuildButtonUp(unit.Name);
            button.TooltipText = $"Build a {unit.Name} for {(int)(unit.BuildCost)} RP.";
            buttonContainer.AddChild(button);
        }
    }

    private void UpdateConstructionQueue()
    {
        Game.ClearChildren(buildQueueContainer);
        int idx = 0;
        foreach (var unit in builder.GetQueue()) {
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
        builder.ClearQueue(index);
    }

    public void Open(Builder bay)
    {
        if (bay == null) {
            return;
        }
        builder = bay;
        mainLabel.Text = bay.BuildMenuName;
        Game.ClearChildren(buildQueueContainer);
        Game.ClearChildren(buttonContainer);

        UpdateUnitButtons();
        UpdateConstructionQueue();
        Show();
    }

    private void BuildButtonUp(string unitName)
    {
        UnitStats stats = builder.GetUnit(unitName);
        structurePlacementTool.SetPrefabs(stats, unitName, stats.BuildCursorPrefabFile, stats.PrefabFile);
        structurePlacementTool.SetBuilder(builder);
    }

    public void Close()
    {
        Hide();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (builder == null || builder.NativeInstance.ToInt64() == 0x0) {
            Close();
            return;
        }
        if (numQueueItems != builder.GetQueue().Count()) {
            UpdateConstructionQueue();
        }
        if (numQueueItems > 0 && headItem != null) {
            headItem.Progress.Show();
            headItem.Progress.TooltipText = $"{(int)(builder.GetProgress() * 100)}% complete.";
            headItem.Progress.Value = (int)(builder.GetProgress() * 100);
        }
    }
}
