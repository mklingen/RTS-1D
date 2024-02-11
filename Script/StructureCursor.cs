using Godot;
using System;
using System.Linq;

// Represents a blueprint for creating a structure.
public partial class StructureCursor : PlanetObject
{
    [ExportGroup("Materials")]
    [Export]
    private BaseMaterial3D blueprintMaterial;

    [ExportGroup("Prefabs")]
    [Export(PropertyHint.File, "*.tscn")]
    private string startingPrefab = "";
    
    private Node3D childPrefab = null;

    public override void _Ready()
    {
        base._Ready();
        if (!string.IsNullOrEmpty(startingPrefab)) {
            SetPrefab(startingPrefab);
        }
    }

    public void SetColor(Color color)
    {
        blueprintMaterial.AlbedoColor = color;
    }

    public void SetPrefab(string newPrefab)
    {
        if (childPrefab != null) {
            childPrefab.QueueFree();
        }
        childPrefab = CreatePrefabChild(newPrefab);
        SetMaterials();
    }


    private void SetMaterials()
    {
        if (childPrefab == null) {
            return;
        }
        foreach (var mesh in Game.FindChildrenRecursive<MeshInstance3D>(childPrefab)) {
            for(int k = 0; k < mesh.GetSurfaceOverrideMaterialCount(); k++) {
                mesh.SetSurfaceOverrideMaterial(k, blueprintMaterial);
            }
        }
    }

    private Node3D CreatePrefabChild(string currentPrefab)
    {
        PackedScene scene = (PackedScene)ResourceLoader.Load(currentPrefab);
        var node = scene.Instantiate<Node3D>();
        if (node == null) {
            GD.PrintErr("Scene did not contain node3d as root.");
            return null;
        }
        this.AddChild(node);
        return node;
    }

}
