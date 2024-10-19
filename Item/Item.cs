using Godot;
using System;

[Tool]
public partial class Item : Node
{
    [Export] public int tileMapIndex;
    [Export] public Vector2I tileMapPos;
    [Export] public UI.PlacingMode placingMode;
    [Export] public float buildDifficulty = 1;

    [Export] public int count;
    [Export] public float weight;

    protected Terrain terrain;
    protected TaskManager taskManager;
    protected ItemManager itemManager;

    public Vector2 position;

    public override void _Ready()
    {
        AddToGroup("Item");

        terrain = GetNode<Terrain>("../../Terrain");
        taskManager = GetNode<TaskManager>("../../TaskManager");
        itemManager = GetNode<ItemManager>("../../ItemManager");
    }

    public override void _Process(double delta)
    {
        
    }

    public virtual Vector2I GetTileMapPos()
    {
        return tileMapPos;
    }

    public virtual bool CanHarvest()
    {
        return false;
    }

    public virtual int GetCount()
    {
        return count;
    }

    public virtual string GetName()
    {
        return Name;
    }
}
