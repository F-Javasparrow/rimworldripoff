using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Tool]
public partial class Terrain : TileMap
{
    private TaskManager taskManager;
    private Pathfinding pathFinding;
    private ItemManager itemManager;
    private UIDrawingLayer drawLayer;

    [Export] public bool isGenerateTerrain { get; set; }
    [Export] public bool isClearTerrain { get; set; }

    [Export] public int MapWidth { get; set; }
    [Export] public int MapHeight { get; set; }

    [Export] public int TerrainSeed { get; set; }

    [Export] public float GrassThreshold { get; set; }
    [Export] public float Grass2Threshold { get; set; }
    [Export] public float DirtThreshold { get; set; }
    [Export] public float RockThreshold { get; set; }

    private float lastPlantUpdate = 0;

    private Dictionary<Vector2I, Construction> constructions = new Dictionary<Vector2I, Construction>();
    private Dictionary<Vector2I, Item> items = new Dictionary<Vector2I, Item>();
    private List<Item> queuedForDeletion = new List<Item>();

    public enum TerrainLayer 
    { 
        Base = 0, 
        Built = 1, 
        Items = 2, 
        ConstructionGhosts = 3, 
        UIGhosts = 4 
    }

    public override void _Ready()
    {
        taskManager = GetNode<TaskManager>("../TaskManager");
        pathFinding = GetNode<Pathfinding>("../Pathfinding");
        itemManager = GetNode<ItemManager>("../ItemManager");
        drawLayer = GetNode<UIDrawingLayer>("../UIDrawingLayer");

        SetLayerModulate((int)TerrainLayer.Items, new Color(1, 1, 1, 1));
        SetLayerModulate((int)TerrainLayer.ConstructionGhosts, new Color(1, 1, 1, 0.4f));
        GenerateTerrain();
    }

    public override void _Process(double delta)
    {
        if ((Time.GetTicksMsec() - lastPlantUpdate) > 100)
        {
            foreach (var item in items)
            {
                if(item is Plant plant)
                {
                    plant.UPdate((Time.GetTicksMsec() - lastPlantUpdate) * 10);
                }
            }

            lastPlantUpdate = Time.GetTicksMsec();
        }

        if (isGenerateTerrain)
        {
            isGenerateTerrain = false;
            GenerateTerrain();
        }

        if (isClearTerrain)
        {
            isClearTerrain = false;
            Clear();
            items.Clear();
            constructions.Clear();
        }

        RemoveQueuedItemsFromWorld();
    }

    public List<Item> FindOrderableItems(Vector2I from, Vector2I to, Task.Orders order)
    {
        var itemsInRect = GetItemsInRect(from, to);
        var targetItems = new List<Item>();

        foreach (var item in itemsInRect)
        {
            switch (order)
            {
                case Task.Orders.Cancel:
                    break;
                case Task.Orders.Deconstruct:
                    break;
                case Task.Orders.Mine:
                    break;
                case Task.Orders.Chop:
                    break;
                case Task.Orders.Harvest:
                    if (item.CanHarvest())
                    {
                        targetItems.Add(item);
                    }
                    break;
            }
        }

        return targetItems;
    }

    public List<Item> GetSurroundingItems(Vector2I terrainPos)
    {
        var surroundingPos = new List<Vector2I>
        {
            terrainPos + new Vector2I(1, 1),
            terrainPos + new Vector2I(0, 1),
            terrainPos + new Vector2I(-1, 1),
            terrainPos + new Vector2I(1, 0),
            terrainPos + new Vector2I(-1, 0),
            terrainPos + new Vector2I(1, -1),
            terrainPos + new Vector2I(0, -1),
            terrainPos + new Vector2I(-1, -1)
        };

        var surroundingItems = new List<Item>();
        foreach (var pos in surroundingPos)
        {
            if (items.TryGetValue(pos, out var item))
            {
                surroundingItems.Add(item);
            }
        }

        return surroundingItems;
    }

    public List<Item> GetItemsInRect(Vector2I from, Vector2I to)
    {
        var list = new List<Item>();
        for (int x = Mathf.Min(from.X, to.X); x <= Mathf.Max(from.x, to.x); x++)
        {
            for (int y = Mathf.Min(from.Y, to.Y); y <= Mathf.Max(from.y, to.y); y++)
            {
                if (items.TryGetValue(new Vector2I(x, y), out var item))
                {
                    list.Add(item);
                }
            }
        }
        return list;
    }

    public Item GetItemAtPosition(Vector2I terrainPos)
    {
        if (items.TryGetValue(terrainPos, out var item))
        {
            return item;
        }
        else if (constructions.TryGetValue(terrainPos, out var construction))
        {
            return construction;
        }
        return null;
    }

    public void UpdateItemTile(Item item)
    {
        SetCell((int)TerrainLayer.Items, WorldToTerrainPos(item.Position), item.TileMapIndex, item.GetTileMapPos());
    }

    public Item PlaceItemByName(string itemName, Vector2I terrainPos, int amount = 1)
    {
        var prototype = itemManager.FindItemPrototype(itemName);
        var item = (Item)prototype.Duplicate();
        item.count = amount;
        return PlaceItem(item, terrainPos);
    }

    public Item PlaceItem(Item item, Vector2I terrainPos)
    {
        if (!IsInRange(terrainPos))
        {
            return null;
        }

        var positions = GetSpiralPositions(terrainPos, 25);
        foreach (var pos in positions)
        {
            if (IsPositionEmpty(pos))
            {
                int layer = 0;
                if (item is Item)
                {
                    layer = (int)TerrainLayer.Items;
                    items[pos] = item;
                }
                else if (item is Wall wall)
                {
                    layer = (int)TerrainLayer.Built;
                    constructions[pos] = item;
                }

                item.Position = TerrainToWorldPos(pos);
                item.Terrain = this;
                if (item is Plant plant)
                {
                    plant.RandomizeAge();
                }

                SetCell(layer, pos, item.TileMapIndex, item.GetTileMapPos());

                return item;
            }
        }

        return null;
    }

    public bool IsInRange(Vector2I pos)
    {
        return pos.X >= 0 && pos.Y >= 0 && pos.X < MapWidth && pos.Y < MapHeight;
    }

    public void RemoveItemFromWorld(Item item, int amount = 0)
    {
        if (amount != 0)
        {
            item.count -= amount;
        }

        if (amount == 0 || item.count <= 0)
        {
            queuedForDeletion.Add(item);
        }
    }

    public void RemoveQueuedItemsFromWorld()
    {
        foreach (var item in queuedForDeletion)
        {
            int layer = 0;
            var terrainPos = WorldToTerrainPos(item.Position);
            if (item is Item)
            {
                layer = (int)TerrainLayer.Items;
                items.Remove(terrainPos);
            }
            else if (item is Wall)
            {
                layer = (int)TerrainLayer.Built;
                constructions.Remove(terrainPos);
            }
            SetCell(layer, terrainPos, -1);
        }
        queuedForDeletion.Clear();
    }

    public Item FindNearestItem(string itemCategory, Vector2 worldPosition)
    {
        GD.Print("Requirest " + itemCategory);
        if (items.Count == 0)
        {
            return null;
        }

        Item nearestItem = null;
        float nearestDistance = float.MaxValue;

        foreach (var item in items)
        {
            if (IsItemInCategory(item, itemCategory))
            {
                float distance = worldPosition.DistanceTo(item.Position);
                if (nearestItem == null || distance < nearestDistance)
                {
                    nearestItem = item;
                    nearestDistance = distance;
                }
            }
        }
        return nearestItem;
    }

    public bool IsItemInCategory(Item item, string itemCategory)
    {
        return item.IsInGroup(itemCategory) || item.Name == itemCategory;
    }

    public List<Vector2I> GetSpiralPositions(Vector2I origin, int maxCount)
    {
        var positions = new List<Vector2I>();
        int x = 0, y = 0;

        for (int i = 0; i < maxCount; i++)
        {
            positions.Add(new Vector2I(origin.X + x, origin.Y + y));
            if (Mathf.Abs(x) <= Mathf.Abs(y) && (x != y || x >= 0))
            {
                x += (y >= 0) ? 1 : -1;
            }
            else
            {
                y += (x >= 0) ? -1 : 1;
            }
        }
        return positions;
    }

    public bool IsPositionEmpty(Vector2I terrainPos)
    {
        return !items.ContainsKey(terrainPos) && !constructions.ContainsKey(terrainPos);
    }

    public void PlaceConstructionOrder(Construction placingPrototype, Vector2I terrainPos)
    {
        if (constructions.ContainsKey(terrainPos))
        {
            GD.Print("Construction order already exists at this position: " + terrainPos);
            return;
        }

        var newConstruction = (Construction)placingPrototype.Duplicate();
        constructions[terrainPos] = newConstruction;
        newConstruction.Position = TerrainToWorldPos(terrainPos);
        newConstruction.Terrain = this;
        taskManager.AddBuildOrder(newConstruction);
        SetCell((int)TerrainLayer.ConstructionGhosts, terrainPos, newConstruction.TileMapIndex, newConstruction.GetTileMapPos());
    }

    public void OnConstructionComplete(Construction construction)
    {
        EraseCell((int)TerrainLayer.ConstructionGhosts, WorldToTerrainPos(construction.Position));
        SetCell((int)TerrainLayer.Built, WorldToTerrainPos(construction.Position), construction.TileMapIndex, construction.GetTileMapPos());
        pathFinding.AddConstructionToPathfinding((int)TerrainLayer.Built, WorldToTerrainPos(construction.Position));
    }

    public void GenerateTerrain()
    {
        GD.Print("生成地形...");

        Clear();
        items.Clear();
        constructions.Clear();

        var noise = new FastNoiseLite();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;

        var rng = new RandomNumberGenerator();

        noise.Seed = TerrainSeed == 0 ? (int)rng.Randi() : TerrainSeed;

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                float noiseValue = noise.GetNoise2D(x, y);
                if (noiseValue > GrassThreshold)
                {
                    SetCell(0, new Vector2I(x, y), 0, new Vector2I(0, 0));
                    if (rng.RandiRange(0, 20) == 0)
                    {
                        // PlaceItemByName("Tree", new Vector2i(x, y));
                    }
                    else if (rng.RandiRange(0, 50) == 0)
                    {
                        // PlaceItemByName("Berry Bush", new Vector2i(x, y));
                    }
                }
                else if (noiseValue > Grass2Threshold)
                {
                    SetCell(0, new Vector2I(x, y), 0, new Vector2I(1, 0));
                    if (rng.RandiRange(0, 40) == 0)
                    {
                        // PlaceItemByName("Pine Tree", new Vector2i(x, y));
                    }
                }
                else if (noiseValue > DirtThreshold)
                {
                    SetCell(0, new Vector2I(x, y), 0, new Vector2I(2, 0));
                    if (rng.RandiRange(0, 100) == 0)
                    {
                        // PlaceItemByName("Pine Tree", new Vector2i(x, y));
                    }
                }
                else if (noiseValue > RockThreshold)
                {
                    SetCell(0, new Vector2I(x, y), 0, new Vector2I(3, 0));
                }
                else
                {
                    SetCell(0, new Vector2I(x, y), 0, new Vector2I(0, 1));
                }
            }
        }
    }

    public Vector2I WorldToTerrainPos(Vector2 worldPos)
    {
        return new Vector2I((int)(worldPos.X / TileSet.TileSize.X), (int)(worldPos.Y / TileSet.TileSize.Y));
    }

    public Vector2 TerrainToWorldPos(Vector2I terrainPos)
    {
        return new Vector2(terrainPos.X * TileSet.TileSize.X, terrainPos.Y * TileSet.TileSize.Y);
    }
}
