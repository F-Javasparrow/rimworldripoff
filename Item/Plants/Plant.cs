using Godot;
using Godot.Collections;

[Tool]
public partial class Plant : Item
{
    public float harvestProgress = 0;
    public float harvestDifficulty = 4;

    [Export] public string harvestItem = "Berries";
    public Vector2I harvestAmount = new Vector2I(5, 15);

    public float growth = 1;
    public int rot = -1;
    public int currentGrowthLevel = 0;
    public int lastHarvestTime;
    [Export] public float harvestGrowthInterval = 5000;
    public bool harvestable = false;
    public int despawnDeadTreeInterval = 100;

    public bool allowNeighbours = false;
    public float growthAtLastSeed = 0;

    [Export] public Array growthTileMapPos = new Array();
    [Export] public Array growthIntervals = new Array();
    [Export] public Vector2I hasFruitTileMapPos = new Vector2I(-1, -1);
    [Export] public Vector2I deadTileMapPos = new Vector2I(-1, -1);

    public override void _Ready()
    {
        base._Ready();
        AddToGroup("Plant");
    }

    public override void _Process(double delta)
    {
        
    }

    public void Update(float delta)
    {
        var rng = new RandomNumberGenerator();

        if (terrain.GetSurroundingItems(terrain.WorldToTerrainPos(position)).Count > 0 && !allowNeighbours)
        {
            growth -= delta * 0.5f;
            if (growth <= 0)
            {
                terrain.RemoveItemFromWorld(this);
            }
        }
        else
        {
            growth += delta * rng.RandfRange(0.7f, 1.3f);
        }

        if (rot >= 0)
        {
            rot += (int)growth;
            if (rot > 10 * 1000)
            {
                terrain.RemoveItemFromWorld(this);
            }
        }

        if (!IsMature())
        {
            if (growth > (float)growthIntervals[currentGrowthLevel] * 1000)
            {
                currentGrowthLevel++;
                terrain.UpdateItemTile(this);
                lastHarvestTime = (int)growth;
            }
        }
        else
        {
            if (growth > (float)growthIntervals[growthIntervals.Count - 1] * 1000 * 4)
            {
                if (rng.RandiRange(0, despawnDeadTreeInterval * 1000) < delta)
                {
                    rot = 0;
                    terrain.UpdateItemTile(this);
                }
            }

            if (hasFruitTileMapPos != new Vector2I(-1, -1))
            {
                if (growth - lastHarvestTime > harvestGrowthInterval * 1000)
                {
                    harvestable = true;
                    terrain.UpdateItemTile(this);
                }
            }

            if (growth - growthAtLastSeed > 1000 * 60)
            {
                growthAtLastSeed = growth;
                var terrainPos = terrain.WorldToTerrainPos(position);
                var possiblePositions = terrain.GetSpiralPositions(terrainPos, 49);
                var newTree = terrain.PlaceItemByName(Name, possiblePositions[rng.RandiRange(1, possiblePositions.Count - 1)]);
            }
        }
    }

    public bool IsMature()
    {
        return currentGrowthLevel >= growthTileMapPos.Count - 1;
    }

    public override Vector2I GetTileMapPos()
    {
        if (rot >= 0)
            return deadTileMapPos;
        else if (harvestable)
            return hasFruitTileMapPos;
        else
            return (Vector2I)growthTileMapPos[currentGrowthLevel];
    }

    public void RandomizeAge()
    {
        var rng = new RandomNumberGenerator();

        if (rng.RandiRange(0, 10) <= 5)
            allowNeighbours = true;

        growth = rng.RandiRange(0, (int)growthIntervals[growthIntervals.Count - 1]) * 150;

        for (int i = 0; i < growthIntervals.Count - 1; i++)
        {
            if (growth > (float)growthIntervals[i] * 1000)
                currentGrowthLevel = i;
        }

        terrain.UpdateItemTile(this);
    }

    public override bool CanHarvest()
    {
        return harvestable;
    }

    public  bool TryHarvest(float amount)
    {
        var rng = new RandomNumberGenerator();
        harvestProgress += amount / harvestDifficulty;

        if (harvestProgress >= 1)
        {
            harvestProgress = 0;
            lastHarvestTime = (int)growth;
            harvestable = false;
            terrain.PlaceItemByName(harvestItem, terrain.WorldToTerrainPos(position), rng.RandiRange(harvestAmount.X, harvestAmount.Y));
            terrain.UpdateItemTile(this);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void OnClick()
    {
        taskManager.AddTask(Task.BaseTaskType.Harvest, this);
    }
}

