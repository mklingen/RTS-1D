using Godot;
using System;

public partial class Collector : Node3D
{
    private float currentResources = 0;

    [ExportGroup("Resource Collection")]
    [Export]
    private float MaxResources = 16;
    [Export]
    private float CollectionRate = 5.0f;

    public override void _Ready()
    {
        base._Ready();
        currentResources = 0;
        Disable();
    }

    public bool IsEmpty()
    {
        return currentResources == 0.0f;
    }
        

    public bool IsFull()
    {
        return currentResources >= MaxResources;
    }

    public float GetProportionFull()
    {
        return currentResources / MaxResources;
    }

    public bool TryCollect(ResourceField field, float dt)
    {
        float spaceLeft = MaxResources - currentResources;
        float amount = Mathf.Min(dt * CollectionRate, spaceLeft);
        float amountCollected = field.TryRemoveResourcesNear(GlobalPosition, amount);
        currentResources += amountCollected;
        return !IsFull();
    }

    public void DropOff(ResourceDropOff dropOff)
    {
        dropOff.AddResources(ReleaseResources());
    }

    public void Enable()
    {
        Visible = true;
    }

    public void Disable()
    {
        Visible = false;
    }

    public bool UpdateCollection(ResourceField field, float dt)
    {
        if (TryCollect(field, dt)) {
            Enable();
        } else {
            Disable();
        }
        return !IsFull();
    }

    public float ReleaseResources()
    {
        float amountToReturn = currentResources;
        currentResources = 0;
        return amountToReturn;
    }
}
