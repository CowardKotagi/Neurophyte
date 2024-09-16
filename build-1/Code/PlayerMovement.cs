using Godot;
using System.Linq;
public partial class PlayerMovement : CharacterBody3D {
    // Object references
    private CollisionShape3D CollisionShape;
    private GenericCamera cameraBase;
    private PlayerInventory Inventory;
    private Area3D InteractionBox;
    private Node3D ClimbDetection;
    private RayCast3D CrownRayCast;
    private RayCast3D GroundRayCast;
    private ShapeCast3D ShapeCast;
    private RayCast3D ShapeCastRayCast;
    // Movement properties
    private float velocityLength;
    private Godot.Vector2 moveInput;
    private Godot.Vector3 wishDirection;
    private Godot.Vector3 leapPosition;
    private Godot.Vector3 gravity = new Godot.Vector3(0, -0.2f, 0);
    // State
    private bool blockInput;
    private float runSpeed;
    private bool canJump;
    private enum MovementStates : int { Idle, Run, Leap, Ledge, Fall }
    private MovementStates movementState = MovementStates.Idle;
    public override void _Ready() {
        CollisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
        Inventory = GetNode<PlayerInventory>("Inventory");
        InteractionBox = GetNode<Area3D>("InteractionBox");
        ClimbDetection = GetNode<Node3D>("ClimbDetection");
        CrownRayCast = GetNode<RayCast3D>("ClimbDetection/CrownRayCast");
        ShapeCast = GetNode<ShapeCast3D>("ClimbDetection/ShapeCast");
        ShapeCastRayCast = GetNode<RayCast3D>("ClimbDetection/ShapeCastRayCast");
        GroundRayCast = GetNode<RayCast3D>("GroundRayCast");
        ShapeCastRayCast.AddException(this);
    }
    public override void _PhysicsProcess(double delta) {
        if (Input.IsActionJustPressed("debug")) {
            Inventory.AddItemToInventory(1000);
            movementState = MovementStates.Leap;
        }
        velocityLength = Velocity.Length();
        moveInput = blockInput ? new Godot.Vector2(0, 0) : Input.GetVector("moveLeft", "moveRight", "moveForward", "moveBackward");
        var camera_forward = GenericCamera.cameraBase.GlobalTransform.Basis.Z.Normalized();
        var camera_right = GenericCamera.cameraBase.GlobalTransform.Basis.X.Normalized();
        wishDirection = (camera_forward * moveInput.Y + camera_right * moveInput.X).Normalized();
        Interact();
        ShapeCastMethod();
        UpdateMovementState();
        MoveAndSlide();
    }
    private void UpdateMovementState() {
        switch (movementState) {
            case MovementStates.Idle:
                if (!IsOnFloor()) {movementState = MovementStates.Fall;}
                if (moveInput.Length() > 0.01) {movementState = MovementStates.Run;}
                Velocity = Vector3.Zero;
                GroundMove();
                Climb();
                break;
            case MovementStates.Run:
                canJump = true;
                if (!IsOnFloor()) {movementState = MovementStates.Fall;}
                if (moveInput.Length() < 0.01 && velocityLength < 1) {movementState = MovementStates.Idle;}
                if (Input.IsActionPressed("run")) {
                    runSpeed = GetRunStateProperties(2);
                } else {
                    runSpeed = GetRunStateProperties(1);
                }
                GroundMove();
                Climb();
                break;
            case MovementStates.Leap:
                if ((leapPosition - GlobalPosition).Length() < 0.1f) {
                    movementState = MovementStates.Idle;
                    CollisionShape.Disabled = false;
                    break;
                }
                
                CollisionShape.Disabled = false;
                Velocity = (leapPosition - GlobalPosition).Normalized() * 10;
                break;
            case MovementStates.Ledge:
                if (!ShapeCast.IsColliding()) {movementState = MovementStates.Fall; break;}
                Velocity = Vector3.Zero;
                if (!Input.IsActionJustPressed("highProfile")) {break;}
                switch (wishDirection.Dot(ShapeCast.GetCollisionNormal(0))) {
                    case < -0.8f:
                        RotateTowards(wishDirection, this);
                        ShapeCastRayCast.ForceRaycastUpdate();
                        LeapPlayerTo(ShapeCastRayCast.GetCollisionPoint());
                        movementState = MovementStates.Fall;
                        break;
                    case > 0.8f:
                        RotateTowards(ShapeCast.GetCollisionNormal(0), this);
                        Velocity = Vector3.Up * -1;
                        movementState = MovementStates.Fall;
                        break;
                    case 0 when moveInput.Length() == 0:
                        Velocity = ShapeCast.GetCollisionNormal(0) * 6 + Vector3.Up * 8; 
                        movementState = MovementStates.Fall; 
                        RotateTowards(Velocity.Normalized(), this);
                        break;
                    default:
                        RotateTowards(wishDirection, this);
                        Velocity = wishDirection * 6 + Vector3.Up * 8;
                        movementState = MovementStates.Fall;
                        break;
                }
                break;
            case MovementStates.Fall:
                if (IsOnFloor()) {movementState = moveInput.Length() < 0.01 && velocityLength < 1 ? MovementStates.Idle : MovementStates.Run;}
                AirMove();
                break;
        }
    }
    private void GroundMove() {
        if (!IsOnFloor()) {return;}
        if (velocityLength <= runSpeed) {
            Velocity += wishDirection * 0.2f;
        } else {
            Velocity *= 0.9f;
        }
        if (canJump == true && !GroundRayCast.IsColliding() && Input.IsActionPressed("highProfile")) {
            Velocity = wishDirection * 7 + new Vector3(0,4,0);
            canJump = false;
        }
        if (moveInput.Length() == 0) {
            RotateTowards(Velocity, ClimbDetection);
        } else {
            RotateTowards(wishDirection, ClimbDetection);
        }
        RotateTowards(Velocity.Normalized(), this);
        this.Velocity = Velocity.Normalized().Lerp(wishDirection, 0.1f) * Velocity.Length();
        this.ApplyFloorSnap();
    }
    private void AirMove() {
        if (IsOnFloor()) {return;}
        if (moveInput.Length() == 0) {
            RotateTowards(Velocity, ClimbDetection);
        } else {
            RotateTowards(wishDirection, ClimbDetection);
        }
        if (ShapeCast.IsColliding() && !CrownRayCast.IsColliding()) {
            GrabLedge();
        }
        this.Velocity += gravity;
        return;
    }
    private float GetRunStateProperties(float modifier) {
        switch (modifier) {
            case 1: return 1.4f;
            case 2: return 6.2f;
            default: return 1f; 
        }
    }
    private void GrabLedge() {
        var ledgeAngle = Godot.Mathf.RadToDeg(ShapeCast.GetCollisionNormal(0).AngleTo(Vector3.Up));
        if (ledgeAngle < 80 || ledgeAngle > 140) {return;}
        ShapeCastRayCast.ForceRaycastUpdate();
        LeapPlayerTo(ShapeCastRayCast.GetCollisionPoint() - new Vector3(0,1.5f,0) + ShapeCast.GetCollisionNormal(0) * 0.2f);
        RotateTowards(ShapeCast.GetCollisionNormal(0) * -1, this);
        movementState = MovementStates.Ledge;
    }
    private void Climb() {
        if (!Input.IsActionPressed("highProfile") || 
            !ShapeCastRayCast.IsColliding() || 
            !ShapeCast.IsColliding() ||
            ShapeCastRayCast.GetCollisionNormal().AngleTo(Vector3.Up) > this.FloorMaxAngle) {
                return;
        }
        if (CrownRayCast.IsColliding()) {
            GrabLedge();
            return;
        }
        ShapeCastRayCast.ForceRaycastUpdate();
        LeapPlayerTo(ShapeCastRayCast.GetCollisionPoint());
    }
    private void LeapPlayerTo(Vector3 targetPosition) {
        this.GlobalPosition = targetPosition;
        //movementState = MovementStates.Leap;
        //leapPosition = targetPosition;
    }
    private void ShapeCastMethod() {
        ShapeCastRayCast.GlobalPosition = ShapeCast.IsColliding() ? 
            new Vector3(ShapeCast.GetCollisionPoint(0).X, ShapeCastRayCast.GlobalPosition.Y, ShapeCast.GetCollisionPoint(0).Z) - (ShapeCast.GetCollisionNormal(0) * new Vector3(0.2f, 0, 0.2f)) :
            new Vector3(ShapeCast.GlobalPosition.X, this.GlobalPosition.Y + 4, ShapeCast.GlobalPosition.Z);
    }
    private void RotateTowards(Godot.Vector3 lookDirection, Node3D inputNode) {
        if (lookDirection.Length() <= 0.0001f) {return;}
        var targetRotation = (float)System.Math.Atan2(lookDirection.X * -1, lookDirection.Z * -1);
        inputNode.GlobalRotation = new Godot.Vector3(Rotation.X, targetRotation, Rotation.Z);
        //Rotation = new Godot.Vector3(Rotation.X, Godot.Mathf.LerpAngle(Rotation.Y, targetRotation, 0.1f), Rotation.Z);
    }
    private void Interact() {
        if (!Input.IsActionJustReleased("interact")) {return;}
        var overlappingNodes = InteractionBox.GetOverlappingAreas().Concat(InteractionBox.GetOverlappingBodies());
        var interactables = overlappingNodes.Where(i => i.IsInGroup("Interactables") && i.HasMethod("Activate"));
        var closestInteractable = interactables.OrderBy(i => Position.DistanceTo(i.Position)).FirstOrDefault();
        if (closestInteractable == null) {return;}
        if (closestInteractable.HasMethod("AssignTarget")) {
            closestInteractable.Call("AssignTarget", this);
        }
        closestInteractable.Call("Activate");
    }
    private void DebugSpawnCube(Vector3 inputPosition) {
        MeshInstance3D cube = new MeshInstance3D();
        cube.Mesh = new BoxMesh();
        cube.TopLevel = true;
        ((BoxMesh)cube.Mesh).Size = new Vector3(1, 1, 1);
        AddChild(cube);
        cube.GlobalPosition = inputPosition;
    }
}