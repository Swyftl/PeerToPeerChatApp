using Godot;
using System;

[GlobalClass]
public partial class UserData : Node
{
    public static UserData Instance { get; private set; }

    public String username { get; set; }
}
