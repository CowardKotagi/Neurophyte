using Godot;
using System;
public partial class ItemPickup : Area3D {
    [Export] private int itemId;
    private Area3D ItemArea;
    private Node3D target;
    private PlayerInventory TargetInventory;
    public override void _Ready() {
    }
    private void Activate() {
        if (target == null) {return;}
        if (target.GetNode<Node>("Inventory") == null) {return;}
        TargetInventory = target.GetNode<PlayerInventory>("Inventory");
        TargetInventory.AddItemToInventory(itemId);
        this.QueueFree();
    }
    private void AssignTarget(Node3D inputTarget) {
        target = inputTarget;
    }
}
