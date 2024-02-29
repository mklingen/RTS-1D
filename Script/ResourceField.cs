using Godot;
using Godot.NativeInterop;
using System;
using System.Linq;
using System.Runtime.InteropServices;

public partial class ResourceField : PlanetObject
{
    [ExportGroup("Resource Field")]
    [Export]
    private float MaxResources = 32.0f;

    private float resourcesRemaining = .0f;

    [ExportGroup("Motes")]
    [Export]
    private Mesh resourceMoteMesh;

    [Export]
    private float projectionRadius = 0.15f;

    [Export]
    private int maxNumMotes = 32;


    [Export]
    private float minMoteScale = 0.01f;

    [Export]
    private float maxMoteScale = 0.1f;

    private MultiMeshInstance3D multiMesh;

    private int livingMotesCount = 0;
    private bool[] motesAlive;
    private Vector3[] motesGlobalPositions;

    public Vector3 RandomInsideUnitSphere()
    {
        Vector3 r;
        do {
            r.X = (float)GD.RandRange(-1.0, 1.0);
            r.Y = (float)GD.RandRange(-1.0, 1.0);
            r.Z = (float)GD.RandRange(-1.0, 1.0);
        } while (r.Length() > 1.0f);
        return r;

    }

    public float GetResourcesRemaining()
    {
        return resourcesRemaining;
    }

    public float GetMaxResources()
    {
        return MaxResources;
    }


    public float GetProportionRemaining()
    {
        return resourcesRemaining / MaxResources;
    }

    public Vector3? GetClosestMotePosition(Vector3 worldPos)
    {
        int idx = GetClosestMoteIdx(worldPos);
        if (idx >= 0) {
            return motesGlobalPositions[idx];
        }
        return null;
    }

    public int GetClosestMoteIdx(Vector3 worldPos)
    {
        int closestIdx = -1;
        float closestDist = float.MaxValue;
        for (int idx = 0; idx < maxNumMotes; idx++) {
            if (!motesAlive[idx]) {
                continue;
            }
            Vector3 globalPos = motesGlobalPositions[idx];
            float dist = globalPos.DistanceSquaredTo(worldPos);
            if (dist < closestDist) {
                closestIdx = idx;
            }
        }
        return closestIdx;
    }


    public override void _Ready()
    {
        base._Ready();
        multiMesh = Game.FindChildrenRecursive<MultiMeshInstance3D>(this)?.FirstOrDefault();

        if (multiMesh == null) {
            multiMesh = new MultiMeshInstance3D();
            multiMesh.Multimesh = new MultiMesh();
            AddChild(multiMesh);
        }
        //multiMesh.Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        multiMesh.Multimesh.InstanceCount = maxNumMotes;
        multiMesh.Multimesh.Mesh = resourceMoteMesh;
        motesAlive = new bool[maxNumMotes];
        livingMotesCount = maxNumMotes;
        motesGlobalPositions = new Vector3[maxNumMotes];
        for (int i = 0; i < maxNumMotes; i++) {
            CreateRandomMote(i);
        }
        resourcesRemaining = MaxResources;
    }

    public float TryRemoveResourcesNear(Vector3 pos, float amount)
    {
        float getAmount = amount;
        if (resourcesRemaining > amount) {
            getAmount = amount;
        } else {
            getAmount = amount - resourcesRemaining;
        }
        resourcesRemaining -= amount;
        resourcesRemaining = Math.Clamp(resourcesRemaining, 0.0f, MaxResources);
        float desiredProportion = GetProportionRemaining();
        int desiredMoteCount = (int)(maxNumMotes * desiredProportion);
        while (livingMotesCount > desiredMoteCount) {
            KillMoteNear(pos);
        }
        return getAmount;
    }

    public void KillMoteNear(Vector3 pos)
    {
        int idx = GetClosestMoteIdx(pos);
        if (idx >= 0) {
            KillMote(idx);
        }
    }

    public void KillMote(int idx)
    {
        motesAlive[idx] = false;
        multiMesh.Multimesh.SetInstanceTransform(idx, new Transform3D());
        livingMotesCount--;
    }

    private void CreateRandomMote(int idx)
    {
        Vector3 offset = RandomInsideUnitSphere() * projectionRadius;
        Vector3 globalPos = ToGlobal(offset);
        motesAlive[idx] = true;
        Vector3 projected = planet.ProjectToSurface(globalPos, 0.0f);
        motesGlobalPositions[idx] = projected;
        Planet.TangentFrame tangent = planet.GetTangentFrame(projected);
        float randomScale = (float)GD.RandRange(minMoteScale, maxMoteScale);
        float randomRotation = (float)GD.RandRange(-Mathf.Pi, Math.PI);
        Basis randomBasis = tangent.ToBasis(randomScale).Rotated(tangent.Up, randomRotation);
        Transform3D globalMoteTransform = new Transform3D(randomBasis, projected);
        multiMesh.Multimesh.SetInstanceTransform(idx, Transform.Inverse() * globalMoteTransform);
    }

}
