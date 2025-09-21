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
        if (_usernameInput.Text.Length is > 2 and <= 20)
        {
            var userData = GetNode<UserData>("/root/UserData");
            userData.username = _usernameInput.Text;
            // Change the scene to main

            var mainScene = GD.Load<PackedScene>("res://main.tscn");

            GetTree().ChangeSceneToPacked(mainScene);
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Enter"))
        {
            _on_button_pressed();
        }
    }
}
