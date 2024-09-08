using Godot;
using System;
public partial class GenericCamera : Node3D
{
    [Export] private NodePath targetNodePath;
    private Node3D target;
    public static Node3D cameraBase;
    private Node3D cameraPivot;
    private Camera3D cameraRaw;
    private float mouseSensitivity;
    public override void _Ready(){
        Input.MouseMode = Input.MouseModeEnum.Captured;
        cameraBase = this;
        cameraPivot = GetNode<Node3D>("CameraPivot");
        cameraRaw = GetNode<Camera3D>("CameraPivot/CameraRaw");
        target = GetNode<Node3D>(targetNodePath);
        // Old mouse sensitivity calculation: mouseSensitivity = 0.06f / GetViewport().GetVisibleRect().Size.X/1152f;
        mouseSensitivity = 0.1f;
    }
    public override void _UnhandledInput(InputEvent @event){
        if (Input.MouseMode != Input.MouseModeEnum.Captured){return;}
        if (@event is InputEventMouseMotion eventMouseMotion){
            cameraPivot.RotationDegrees += new Godot.Vector3(-eventMouseMotion.Relative.Y * mouseSensitivity, -eventMouseMotion.Relative.X * mouseSensitivity,0);
            cameraBase.RotationDegrees += new Godot.Vector3(-eventMouseMotion.Relative.Y * 0, -eventMouseMotion.Relative.X * mouseSensitivity,0);
        }
        if (@event is InputEventMouseButton){/*TODO: zoom in and out*/}
    }
    public override void _PhysicsProcess(double delta){
        if (target == null) {return;}
        cameraBase.GlobalPosition = target.GlobalPosition;
        cameraBase.RotationDegrees = new Godot.Vector3(target.RotationDegrees.X, cameraBase.RotationDegrees.Y, target.RotationDegrees.Z);
        cameraPivot.GlobalPosition = cameraPivot.GlobalPosition.Lerp(cameraBase.GlobalPosition + Vector3.Up * 2, 0.2f);
    }
}