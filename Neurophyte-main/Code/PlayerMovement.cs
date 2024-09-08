using Godot;
using System;
using System.Linq;
public partial class PlayerMovement : CharacterBody3D
{
    private CharacterBody3D Player;
    private Area3D InteractionBox;
    private PlayerInventory Inventory;
    private Godot.Vector3 gravity = new Godot.Vector3(0,-1,0);
    private float runSpeed;
    private Godot.Vector2 moveInput;
    private Godot.Vector3 wishDirection;
    private float velocityLength;
	private enum MovementStates : int {Idle,Run,Fall}
    private MovementStates movementState = MovementStates.Idle;
    public override void _Ready(){
        Player = this;
        InteractionBox = GetNode<Area3D>("InteractionBox");
        Inventory = GetNode<PlayerInventory>("Inventory");
    }
    public override void _PhysicsProcess(double delta){
        if (Input.IsActionJustPressed("debug")){
            Inventory.AddItemToInventory(1001);
            GD.Print(Inventory.inventory);
        }
        velocityLength = Velocity.Length();
        var camera_forward = GenericCamera.cameraBase.GlobalTransform.Basis.Z.Normalized();
        var camera_right = GenericCamera.cameraBase.GlobalTransform.Basis.X.Normalized();
        moveInput = Input.GetVector("moveLeft", "moveRight", "moveForward", "moveBackward");
        wishDirection = (camera_forward * moveInput.Y + camera_right * moveInput.X).Normalized();
        Interact();
        UpdateMovementProperties();
        UpdateMovementState();
        MoveAndSlide();
    }
    private void UpdateMovementState(){
        switch (movementState) {
            case MovementStates.Idle:
                if (moveInput.Length() > 0.01) {movementState = MovementStates.Run;}
                break;
            case MovementStates.Run:
                if (moveInput.Length() < 0.01 && velocityLength < 100) {movementState = MovementStates.Idle;}
                runSpeed = GetRunStateProperties(1);
                break;
            case MovementStates.Fall:
                if (IsOnFloor()){
                    movementState = moveInput.Length() < 0.01 && velocityLength < 100 ? MovementStates.Idle : MovementStates.Run;
                }
                break;
        }
    }
    private void UpdateMovementProperties(){
        if (!IsOnFloor()){
            RotateTowards(Velocity.Normalized() * new Vector3(1, 0, 1));
            Velocity += gravity;
            return;
        }
        Player.ApplyFloorSnap();
        RotateTowards(wishDirection.Normalized());
        Velocity = Velocity.Normalized().Lerp(wishDirection, 0.9f) * Velocity.Length();
        Velocity = wishDirection * runSpeed;
    }
    private float GetRunStateProperties(float modifier){
        switch (modifier){
            case 1: return 4.4f;
            case 2: return 4.2f;
            default: return 1f; 
        }
    }
    private void RotateTowards(Godot.Vector3 lookDirection){
        if (lookDirection.Length() <= 0.0001f){return;}
        var targetRotation = (float)System.Math.Atan2(lookDirection.X * -1, lookDirection.Z * -1);
        Rotation = new Godot.Vector3(Rotation.X, targetRotation, Rotation.Z);
        //Rotation = new Godot.Vector3(Rotation.X, Godot.Mathf.LerpAngle(Rotation.Y, targetRotation, 0.9f), Rotation.Z);
    }
    private void Interact(){
        if (!Input.IsActionJustReleased("interact")){return;}
        var overlappingNodes = InteractionBox.GetOverlappingAreas().Concat(InteractionBox.GetOverlappingBodies());
        var interactables = overlappingNodes.Where(i => i.IsInGroup("Interactables") && i.HasMethod("Activate"));
        var closestInteractable = interactables.OrderBy(i => Position.DistanceTo(i.Position)).FirstOrDefault();
        if (closestInteractable != null) {
            closestInteractable.Call("Activate");
        }
    }
}