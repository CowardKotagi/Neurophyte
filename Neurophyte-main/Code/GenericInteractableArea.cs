using Godot;
using System;

public partial class GenericInteractableArea : Area3D{
	[Export] public NodePath targetPath;
	public Node target;
    public override void _Ready(){
        target = GetNode(targetPath);
        if (target == null){
            GD.PrintErr("Target node not found:" + targetPath);
        }
    }
	public void Activate(){
        if (target == null){return;}
		target.Call("ReceiveActivation");
	}
}
