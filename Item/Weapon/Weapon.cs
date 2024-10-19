using Godot;
using System;

public partial class Weapon : Item
{
    public override void _Ready()
    {
        base._Ready();
        AddToGroup("Weapon");
    }

    public override void _Process(double delta)
    {

    }
}
