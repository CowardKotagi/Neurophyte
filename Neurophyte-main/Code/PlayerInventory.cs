using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class PlayerInventory : Node
{
    public List<int> inventory = new List<int>();
    private ItemDatabase itemDatabase;
    public override void _Ready() {
        itemDatabase = GetNode<ItemDatabase>("/root/ItemDatabase");
    }
    public void AddItemToInventory(int itemId) {
        if (itemDatabase.GetItemProperty("Id", itemId)?.ValueKind != JsonValueKind.Number || itemDatabase.GetItemProperty("Id", itemId)?.GetInt32() != itemId) {
            GD.Print($"Invalid item ID: {itemId}");
            return;
        }
        GD.Print($"Added {itemDatabase.GetItemProperty("Name", itemId)} to inventory.");
        inventory.Add(itemId);
        GD.Print($"Current Inventory: [{string.Join(",", inventory)}]");
    }
}