using Godot;
using System;

public partial class Food : Item
{
    public enum FoodType 
    { 
        OMNIVORE = 0, 
        VEGETARIAN = 1, 
        CARNIVORE = 2 
    }

    public enum FoodQuality 
    { 
        RUBBISH = 0, 
        SIMPLE = 1, 
        GOOD = 2, 
        FANCY = 3 
    }

    [Export] public float nutrition = 1.0f;
    [Export] public FoodType foodType;
    [Export] public FoodQuality foodQuality;

    public override void _Ready()
    {
        base._Ready();
        AddToGroup("Food");
    }

    public override void _Process(double delta)
    {
        
    }
}
