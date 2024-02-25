using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class MainCamera : Camera3D, ConstructionBay.IConstructionBaySelectionCallback, MainUI.IMainInputCallback
{
    // Distance from the planet.
    private float zoom;
    // The tangent frame of the current camera pose.
    private Planet.TangentFrame tangentFrame;
    // Velocity of the camera.
    private Vector3 velocity;
    // Last mouse wheel direction.
    private float mouseWheelVelocity;
    // Last mouse position.
    private Vector2 mousePos;
    // Min zoom.
    private float minDepth;
    // Max zoom.
    private float maxDepth;
    // Curent zoom.
    private float depth;
    // Zoom velocity.
    private float depthVelocity;
    // Planet we are on.
    private Planet planet;
    // Maximum distance we can zoom in.
    [Export]
    private float maxZoom = 1;

    [Export(PropertyHint.Layers3DPhysics)]
    private uint mouseSelectionMask;

    [Export]
    private int playerTeam = 0;

    [Export]
    private float height = 0.1f;

    private BuildMenu buildMenu;
    private bool suppressMouseClicks;

    private List<ITool> tools;
    private ITool activeTool;

    public void SetActiveTool(ITool tool)
    {
        if (activeTool != null) {
            activeTool.OnDeactivate();
        }
        activeTool = tool;
        if (tool != null) {
            tool.OnActivate();
        }
    }

    public Planet.TangentFrame GetTangentFrame()
    {
        return tangentFrame;
    }

    public Planet GetPlanet()
    {
        return planet;
    }

    public static MainCamera Get()
    {
        return Game.FindChildrenRecursive<MainCamera>(Game.Get())?.FirstOrDefault();
    }

    public override void _Ready()
    {
        base._Ready();
        planet = Game.Get().GetPlanet();
        buildMenu = Game.FindChildrenRecursive<BuildMenu>(Game.Get()).FirstOrDefault();
        if (buildMenu != null) {
            buildMenu.Close();
        }
        tangentFrame = planet.GetTangentFrame(GlobalPosition);
        minDepth = planet.ToLocal(GlobalPosition).Z;
        maxDepth = planet.ToLocal(GlobalPosition).Z + maxZoom;
        depth = minDepth;
        depthVelocity = 0;
        tools = Game.FindInterfacesRecursive<ITool>(this).ToList();
        SetActiveTool(tools.FirstOrDefault());
    }

    private void OrientCamera()
    {
        // Orient camera so that "up" always points to the angular position of the camera.
        //Rotation = new Vector3(Rotation.X, Rotation.Y, planetPos.Longitude - Mathf.Pi * 0.5f);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        tangentFrame = planet.GetTangentFrame(GlobalPosition);
        HandleInput();

        UpdatePosition(delta);
        OrientCamera();

        HandleLeftClick();
        HandleRightClick();
    }

    private Vector3 mouseSelectionPoint;

    public override void _PhysicsProcess(double delta)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var from = ProjectRayOrigin(mousePos);
        var to = from + ProjectRayNormal(mousePos) * 10.0f;
        // use global coordinates, not local to node
        var query = PhysicsRayQueryParameters3D.Create(from, to, mouseSelectionMask);
        var result = spaceState.IntersectRay(query);
        if (result.Count > 0) {
            mouseSelectionPoint = (Vector3)result["position"];
        }
    }
    private void HandleRightClick()
    {
        if (suppressMouseClicks) {
            return;
        }
        Vector3 pos = planet.ProjectToCylinder(mouseSelectionPoint, 0, 0);
        bool isRightPressed = Input.IsActionJustPressed("SecondaryClick");
        if (isRightPressed) {
            activeTool?.OnMouseFirstPressed(mousePos, pos, ITool.MouseButton.Right);
        }
        if (!Input.IsActionJustReleased("SecondaryClick")) {
            return;
        }
        activeTool?.OnMouseClick(mousePos, planet.ProjectToCylinder(mouseSelectionPoint, 0, 0), ITool.MouseButton.Right);
    }

    private void HandleInput()
    {
        // Handle input for changing angle
        float angleSpeed = Mathf.Clamp((depth - minDepth) / (maxDepth - minDepth), 1.0f, 10.0f); // Adjust the speed of angle change as needed
        //float angleDamping = 0.75f;
        //planetVelocity.Longitude *= angleDamping;
        if (Input.IsActionPressed("CameraLeft")) {
            velocity = tangentFrame.Left * 0.25f * angleSpeed;
        }
        else if (Input.IsActionPressed("CameraRight")) {
            velocity = tangentFrame.Right * 0.25f * angleSpeed;
        }
        if (Input.IsActionPressed("CameraUp")) {
            mouseWheelVelocity = -1;
        }
        else if (Input.IsActionPressed("CameraDown")) {
            mouseWheelVelocity = 1;
        } else {
            mouseWheelVelocity = mouseWheelVelocity * 0.65f;
        }

        if (Input.IsActionJustReleased("ui_cancel")) {
            OnUICancel();
        }

        // Handle input for changing depth using the mouse wheel
        float depthSpeed = 0.5f; // Adjust the speed of depth change as needed
        depthVelocity += depthSpeed * mouseWheelVelocity;

        activeTool?.OnMouseMove(mousePos, planet.ProjectToCylinder(mouseSelectionPoint, 0, 0));

    }

    private void OnUICancel()
    {
        SetActiveTool(tools.OfType<MoveSelectTool>().FirstOrDefault());
    }

    private void UpdatePosition(double delta)
    {
        GlobalPosition = GlobalPosition - tangentFrame.Out * depth;
        GlobalPosition += velocity * (float)delta;
        depth += depthVelocity * (float)delta;
        depth = Mathf.Clamp(depth, minDepth, maxDepth);
        velocity *= 0.65f;
        depthVelocity *= 0.5f;
        GlobalPosition = planet.ProjectToCylinder(GlobalPosition, height, 0);
        GlobalPosition += tangentFrame.Out * depth;
        LookAtFromPosition(GlobalPosition, planet.ProjectToCylinder(GlobalPosition + tangentFrame.In, 0, 0), tangentFrame.Up.Rotated(tangentFrame.Left, 0.2f));
    }

    private void HandleLeftClick()
    {
        if (suppressMouseClicks) {
            return;
        }
        if (Input.IsActionJustReleased("PrimaryClick")) {
            activeTool?.OnMouseClick(mousePos, planet.ProjectToCylinder(mouseSelectionPoint, 0, 0), ITool.MouseButton.Left);
        }
        if (Input.IsActionJustPressed("PrimaryClick")) {
            activeTool?.OnMouseFirstPressed(mousePos, planet.ProjectToCylinder(mouseSelectionPoint, 0, 0), ITool.MouseButton.Left);
        }
    }


    public void OnSelect(ConstructionBay bay)
    {
        if (buildMenu != null) {
            buildMenu.Open(bay);
        }
    }

    public void OnDeselect(ConstructionBay bay)
    {
        if (buildMenu != null) {
            buildMenu.Close();
        }
    }

    public void OnEvent(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouse) {
            mousePos = (inputEvent as InputEventMouse).GlobalPosition;
        }
        if (inputEvent is InputEventMouseButton) {
            InputEventMouseButton emb = (InputEventMouseButton)inputEvent;
            if (emb.IsPressed()) {
                if (emb.ButtonIndex == MouseButton.WheelUp) {
                    mouseWheelVelocity = 3;
                }
                if (emb.ButtonIndex == MouseButton.WheelDown) {
                    mouseWheelVelocity = -3;
                }
            }
        }
    }

    public void OnMouseEnter()
    {
        suppressMouseClicks = false;
    }

    public void OnMouseLeave()
    {
        suppressMouseClicks = true;
    }
}
