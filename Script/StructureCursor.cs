using Godot;
using System;
using System.Linq;

// Represents a blueprint for creating a structure.
public partial class StructureCursor : PlanetObject
{
    [ExportGroup("Materials")]
    [Export]
    private BaseMaterial3D blueprintMaterial;
    private MeshInstance3D mesh;

    public override void _Ready()
    {
        base._Ready();
        mesh = Game.FindChildrenRecursive<MeshInstance3D>(this).FirstOrDefault();
        for (int k = 0; k < mesh.GetSurfaceOverrideMaterialCount(); k++) {
            mesh.SetSurfaceOverrideMaterial(0, blueprintMaterial);
        }
    }

    public void SetColor(Color color)
    {
        blueprintMaterial.AlbedoColor = color;
    }

    public void SetMesh(Mesh newMesh)
    {
        mesh.Mesh = newMesh;
        for (int k = 0; k < mesh.GetSurfaceOverrideMaterialCount(); k++) {
            mesh.SetSurfaceOverrideMaterial(0, blueprintMaterial);
        }
    }

}
