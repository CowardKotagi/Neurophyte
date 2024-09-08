using Godot;
using System;
public partial class InventoryUserInterface : Control {
    private VBoxContainer vBoxContainer;
    private ItemDatabase itemDatabase;
    private CharacterBody3D Player;
    private PlayerInventory playerInventory;
    private bool wasVisible = false; // add this variable
     public override void _Ready() {
        vBoxContainer = GetNode<VBoxContainer>("VBoxContainer");
        itemDatabase = GetNode<ItemDatabase>("/root/ItemDatabase");
        Player = GetNode<CharacterBody3D>("/root/Node3D/Entities/Player");
        playerInventory = Player.GetNode<PlayerInventory>("Inventory");
    }
    public override void _PhysicsProcess(double delta) {
        bool isVisible = Input.IsActionPressed("inventory");
        if (!wasVisible && isVisible) {
            foreach (Node child in vBoxContainer.GetChildren()) {
                if (child is HBoxContainer) {
                    child.QueueFree();
                }
            }
            foreach (int itemId in playerInventory.inventory) {
                SpawnBox(itemId);
            }
        }
        wasVisible = isVisible;
        this.Visible = isVisible;
    }
    public void SpawnBox(int itemId) {
        PackedScene boxResource = GD.Load<PackedScene>("res://InventoryItem.tscn");
        HBoxContainer box = (HBoxContainer)boxResource.Instantiate();
        box.Name = itemId.ToString();
        Label nameLabel = box.GetNode<Label>("Name");
        Label descriptionLabel = box.GetNode<Label>("Description");
        TextureRect iconTextureRect = box.GetNode<TextureRect>("Icon");
        nameLabel.Text = itemDatabase.GetItemProperty("Name", itemId).ToString();
        descriptionLabel.Text = itemDatabase.GetItemProperty("Description", itemId).ToString();
        vBoxContainer.AddChild(box);
    }
}