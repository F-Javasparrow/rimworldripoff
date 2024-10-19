using Godot;
using System;

public partial class ProjectileWeapon : Node
{
    public override void _Ready()
    {
        base._Ready();
        AddToGroup("ProjectileWeapon");
    }

    public override void _Process(double delta)
    {

    }
}
