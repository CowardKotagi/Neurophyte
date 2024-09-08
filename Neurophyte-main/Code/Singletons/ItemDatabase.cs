using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
public partial class ItemDatabase : Node
{
    public string dataFilePath = "res://Assets/Data/Items.json";
    private JsonDocument parsedData;
    public override void _Ready(){
        LoadItemData();
        GD.Print(GetItemProperty("Name", 1001));
    }
    public void LoadItemData(){
        if (!FileAccess.FileExists(dataFilePath)){return;}
        var dataFile = FileAccess.Open(dataFilePath, FileAccess.ModeFlags.Read);
        string jsonData = dataFile.GetAsText();
        parsedData = JsonDocument.Parse(jsonData);
    }
    public JsonElement? GetItemProperty(string inputProperty, int itemId){
        if (parsedData == null) {return null;}
        var root = parsedData.RootElement;
        if (root.ValueKind != JsonValueKind.Array){return null;}
        foreach (var item in root.EnumerateArray()){
            var idProperty = item.GetProperty("Id");
            if (idProperty.ValueKind == JsonValueKind.Number && idProperty.GetInt32() == itemId){
                return item.GetProperty(inputProperty);
            }
        }
        return null;
    }
}