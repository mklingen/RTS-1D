using Godot;

public interface ITool
{
    public enum MouseButton
    {
        Left, Right
    }

    public UnitStats.Abilities GetAbilities();

    public void OnActivate();
    public void OnDeactivate();
    public void OnMouseFirstPressed(Vector2 mouseScreenPos, Vector3 mouseWorldPos, MouseButton click);
    public void OnMouseMove(Vector2 mouseScreenPos, Vector3 mouseWorldPos);
    public void OnMouseClick(Vector2 mouseScreenPos, Vector3 mousePos, MouseButton click);

    public string GetName();

    public int GetPriority();
}
