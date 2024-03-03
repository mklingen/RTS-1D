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
    private float cameraZoom;
    // Zoom velocity.
    private float cameraZoomVelocity;
    // Planet we are on.
    private Planet planet;
    [ExportGroup("Camera Controls")]
    // Maximum distance we can zoom in.
    [Export]
    private float maxZoom = 1;

    [Export]
    private float maxAngleSpeed = 10.0f;

    [Export]
    private float angleSensitivity = 1.0f;

    [Export]
    private float velocityDamping = 0.65f;

    [Export]
    private float cameraZoomDamping = 0.5f;

    [Export]
    private float height = 0.1f;

    [Export]
    private float cameraZoomSensitivity = 0.5f;

    private float depth = 0.0f;

    [ExportGroup("Selection")]
    [Export(PropertyHint.Layers3DPhysics)]
    private uint mouseSelectionMask;

    [Export]
    private int playerTeam = 0;

    private bool suppressMouseClicks;

    private List<ITool> tools;
    private ITool activeTool;

    private float minFOV = 0;
    private float maxFOV = 0;

    public List<ITool> GetTools()
    {
        return tools;
    }

    public void SetActiveToolByName(string tool)
    {
        ITool selected = tools.FirstOrDefault(activeTool => activeTool.GetName() == tool);
        if (selected != null) {
            SetActiveTool(selected);
        }
    }

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
        tangentFrame = planet.GetTangentFrame(GlobalPosition);
        minDepth = (planet.ProjectToSurface(GlobalPosition) - GlobalPosition).Length();
        maxDepth = minDepth + maxZoom;
        depth = -planet.ToLocal(GlobalPosition).Z;
        cameraZoom = minDepth;
        cameraZoomVelocity = 0;
        tools = Game.FindInterfacesRecursive<ITool>(this).ToList();
        OnUICancel();
        minFOV = Fov * 0.5f;
        maxFOV = Fov * 1.5f;
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
        Vector3 pos = planet.ProjectToCylinder(mouseSelectionPoint, 0.0f, 0.0f);
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
        float angleSpeed = Mathf.Clamp((cameraZoom - minDepth) / (maxDepth - minDepth), 1.0f, maxAngleSpeed); // Adjust the speed of angle change as needed
        //float angleDamping = 0.75f;
        //planetVelocity.Longitude *= angleDamping;
        if (Input.IsActionPressed("CameraLeft")) {
            velocity = tangentFrame.Left * angleSensitivity * angleSpeed;
        }
        else if (Input.IsActionPressed("CameraRight")) {
            velocity = tangentFrame.Right * angleSensitivity * angleSpeed;
        }
        if (Input.IsActionPressed("CameraUp")) {
            mouseWheelVelocity = -1;
        }
        else if (Input.IsActionPressed("CameraDown")) {
            mouseWheelVelocity = 1;
        } else {
            mouseWheelVelocity = mouseWheelVelocity * cameraZoomDamping;
        }

        if (Input.IsActionJustReleased("ui_cancel")) {
            OnUICancel();
        }

        // Handle input for changing cameraZoom using the mouse wheel
        cameraZoomVelocity += cameraZoomSensitivity * mouseWheelVelocity;

        activeTool?.OnMouseMove(mousePos, planet.ProjectToCylinder(mouseSelectionPoint, 0, 0));

    }

    private void OnUICancel()
    {
        SetActiveTool(tools.OfType<MoveSelectTool>().FirstOrDefault());
    }

    private void UpdatePosition(double delta)
    {
        GlobalPosition = GlobalPosition - tangentFrame.Up * cameraZoom;
        GlobalPosition += velocity * (float)delta;
        cameraZoom += cameraZoomVelocity * (float)delta;
        cameraZoom = Mathf.Clamp(cameraZoom, minDepth, maxDepth);
        velocity *= velocityDamping;
        cameraZoomVelocity *= cameraZoomDamping;
        GlobalPosition = planet.ProjectToCylinder(GlobalPosition, height, depth);
        GlobalPosition += tangentFrame.Up * cameraZoom;
        LookAtFromPosition(GlobalPosition, planet.ProjectToCylinder(GlobalPosition + tangentFrame.In, 0, 0.2f), tangentFrame.Up.Rotated(tangentFrame.Left, 0.2f));
        zoom = cameraZoom;
        Fov = ((cameraZoom - minDepth) / (maxDepth - minDepth)) * (maxFOV - minFOV) + minFOV;
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
        var unitBuilder = tools.OfType<UnitBuilderTool>().FirstOrDefault();
        if (unitBuilder != null) {
            unitBuilder.OnSelect(bay);
        }
    }

    public void OnDeselect(ConstructionBay bay)
    {
        tools.OfType<UnitBuilderTool>().FirstOrDefault()?.OnDeselect(bay);
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
