using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class ItemManager : Node
{
    public enum ItemCategory
    { 
        ITEM = 0, 
        FOOD = 1, 
        WEAPON = 2, 
        MELEEWEAPON = 3, 
        PROJECTILEWEAPON = 4 
    }
    public string[] itemCategories = { "Item", "Food", "Weapons", "MeleeWeapon", "ProjectileWeapon" };

    private List<PackedScene> foodPrototypes = new List<PackedScene>();
    private List<PackedScene> itemPrototypes = new List<PackedScene>();
    private List<PackedScene> constructionPrototypes = new List<PackedScene>();
    private List<Node> itemsInWorld = new List<Node>();

    public override void _Ready()
    {
        LoadFood();
        LoadItemPrototypes();
        LoadConstructionPrototypes();
    }

    public override void _Process(double delta)
    {
        
    }

    public Vector2 MapToWorldPosition(int mapPosX, int mapPosY)
    {
        return new Vector2(mapPosX * 16 + 8, mapPosY * 16 + 8);
    }

    public static Vector2I WorldToMapPosition(Vector2 worldPos)
    {
        return new Vector2I((int)(worldPos.X / 16), (int)(worldPos.Y / 16));
    }

    public void RemoveItemFromWorld(Node item)
    {
        RemoveChild(item);
        itemsInWorld.Remove(item);
    }

    public PackedScene FindItemPrototype(string itemName)
    {
        foreach (var item in itemPrototypes)
        {
            if (item.ResourcePath == itemName)
            {
                return item;
            }
        }
        return null;
    }

    public void SpawnItemByName(string itemName, int amount, Vector2I mapPosition)
    {
        Node newItem = null;

        foreach (var item in itemPrototypes)
        {
            if (item.ResourcePath == itemName)
            {
                newItem = item.Instantiate<Node>();
                newItem.Set("count", amount);
            }
        }

        if (newItem != null)
        {
            AddChild(newItem);
            itemsInWorld.Add(newItem);
            if (newItem is Node2D node2D)
            {
                node2D.Position = MapToWorldPosition(mapPosition.X, mapPosition.Y);
            }
        }
    }

    public void SpawnItem(PackedScene item, Vector2I mapPosition)
    {
        Node newItem = item.Instantiate<Node>();
        AddChild(newItem);
        itemsInWorld.Add(newItem);
        if (newItem is Node2D node2D)
        {
            node2D.Position = MapToWorldPosition(mapPosition.X, mapPosition.Y);
        }
    }

    public Node FindNearestItem(ItemCategory itemCategory, Vector2 worldPosition)
    {
        if (itemsInWorld.Count == 0)
            return null;

        Node nearestItem = null;
        float nearestDistance = float.MaxValue;

        foreach (var item in itemsInWorld)
        {
            if (IsItemInCategory(item, itemCategory) && item is Node2D node2D)
            {
                float distance = worldPosition.DistanceTo(node2D.Position);
                if (nearestItem == null || distance < nearestDistance)
                {
                    nearestItem = item;
                    nearestDistance = distance;
                }
            }
        }
        return nearestItem;
    }

    public bool IsItemInCategory(Node item, ItemCategory itemCategory)
    {
        return item.IsInGroup(itemCategories[(int)itemCategory]);
    }

    public void LoadItemPrototypes()
    {
        var allFileNames = _DirContents("res://Item/", ".tscn");
        foreach (var fileName in allFileNames)
        {
            itemPrototypes.Add(GD.Load<PackedScene>(fileName));
            GD.Print(fileName);
        }
    }

    public void LoadConstructionPrototypes()
    {
        var allFileNames = _DirContents("res://Constructions/", ".tscn");
        foreach (var fileName in allFileNames)
        {
            constructionPrototypes.Add(GD.Load<PackedScene>(fileName));
            GD.Print(fileName);
        }
    }

    public void LoadFood()
    {
        var path = "res://Item/Food";
        var dir = DirAccess.Open(path);
        dir.ListDirBegin();
        while (true)
        {
            var fileName = dir.GetNext();
            if (fileName == "")
                break;
            else if (fileName.EndsWith(".tscn"))
                foodPrototypes.Add(GD.Load<PackedScene>(path + "/" + fileName));
        }
        dir.ListDirEnd();
    }

    private static List<string> _DirContents(string path, string suffix)
    {
        var dir = DirAccess.Open(path);
        if (dir == null)
        {
            GD.Print($"访问地址时出现错误: {path}");
            return new List<string>();
        }

        var files = new List<string>();
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            fileName = fileName.Replace(".remap", "");
            if (dir.CurrentIsDir())
            {
                files.AddRange(_DirContents($"{path}/{fileName}", suffix));
            }
            else if (fileName.EndsWith(suffix))
            {
                files.Add($"{path}/{fileName}");
            }
            fileName = dir.GetNext();
        }
        return files;
    }

    private static string _PathToName(string path)
    {
        var parts = path.Split('/');
        var name = parts[parts.Length - 1];
        return name.Replace(".tscn", "");
    }
}
