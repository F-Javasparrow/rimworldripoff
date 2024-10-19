using Godot;
using System;

public partial class MeleeWeapon : Weapon
{
    public override void _Ready()
    {
        base._Ready();
        AddToGroup("MeleeWeapon");
    }

    public override void _Process(double delta)
    {

    }
}
