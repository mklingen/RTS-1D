using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class Toolbar : TabBar
{
    private MainCamera player;
    private UnitStats.Abilities selectedAbilities = UnitStats.Abilities.Move;

    public override void _Ready()
    {
        base._Ready();
        this.TabSelected += Toolbar_TabSelected;
        player = Game.FindChildrenRecursive<MainCamera>(Game.Get()).FirstOrDefault();
        var tools = player.GetTools();
        foreach (var tool in tools) {
            if (tool is MoveSelectTool) {
                var moveTool = (MoveSelectTool)tool;
                moveTool.SelectionChanged += SelectionChanged;
            }
        }
        
        ResetTabs();
    }

    public void SelectionChanged(Array<Selectable> selected)
    {
        selectedAbilities = UnitStats.Abilities.Move;
        foreach (var selection in selected) {
            var unit = Game.FindParent<Unit>(selection);
            if (unit != null) {
                var abilities = unit.Stats.CanDoAbilities;
                selectedAbilities |= abilities;
            }
        }
        ResetTabs();
    }

    public void ResetTabs()
    {
        if (player == null) {
            return;
        }

        this.ClearTabs();
        foreach (var tool in player.GetTools().OrderBy(tool => tool.GetPriority())) {
            if (tool.GetAbilities() != UnitStats.Abilities.Move && !selectedAbilities.HasFlag(tool.GetAbilities())) {
                continue;
            }
            this.AddTab(tool.GetName());
        }
    }

    private void Toolbar_TabSelected(long tab)
    {
        if (player == null) {
            return;
        }
        player.SetActiveToolByName(this.GetTabTitle((int)tab));
    }
}
