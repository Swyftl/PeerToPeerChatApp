using Godot;
using System;

public partial class Start : Control
{
    private LineEdit _usernameInput;
    
    public override void _Ready()
    {
        _usernameInput = GetNode<LineEdit>("LineEdit");
    }
    
    public void _on_button_pressed()
    {
        var userData = GetNode<UserData>("/root/UserData");
        userData.username = _usernameInput.Text;
        // Change the scene to main
        
        var mainScene = GD.Load<PackedScene>("res://main.tscn");
        
        GetTree().ChangeSceneToPacked(mainScene);
    }
}
