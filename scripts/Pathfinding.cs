using Godot;
using System;

[Tool]
public partial class Pathfinding : Node2D
{
    [Export] public Vector2I Start { get; set; }
    [Export] public Vector2I End { get; set; }
    [Export] public bool Calculate { get; set; }

    private AStarGrid2D astarGrid = new AStarGrid2D();
    private Vector2[] path = Array.Empty<Vector2>();
    private Terrain terrain;

    public override void _Ready()
    {
        terrain = GetNode<Terrain>("../Terrain");
        InitPathfinding();
    }

    public override void _Draw()
    {
        GD.Print("Redrawing");
        if (path.Length > 0)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                DrawLine(path[i], path[i + 1], Colors.Purple);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Calculate)
        {
            Calculate = false;
            InitPathfinding();
            RequestPath(Start, End);
        }
    }

    public Vector2[] RequestPath(Vector2I start, Vector2I end, bool nextTo = false)
    {
        path = astarGrid.GetPointPath(start, end);

        for (int i = 0; i < path.Length; i++)
        {
            path[i] += new Vector2(terrain.RenderingQuadrantSize / 2, terrain.RenderingQuadrantSize / 2);
        }

        if (nextTo && path.Length > 1)
        {
            Array.Resize(ref path, path.Length - 1);
        }

        QueueRedraw();
        return path;
    }

    private void InitPathfinding()
    {
        astarGrid.Region = new Rect2I(0, 0, terrain.MapWidth, terrain.MapHeight);
        astarGrid.CellSize = new Vector2(16, 16);
        astarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
        astarGrid.Update();

        for (int x = 0; x < terrain.MapWidth; x++)
        {
            for (int y = 0; y < terrain.MapHeight; y++)
            {
                if (GetTerrainDifficulty((int)Terrain.TerrainLayer.Base, new Vector2I(x, y)) == -1)
                {
                    astarGrid.SetPointSolid(new Vector2I(x, y));
                }
                else
                {
                    astarGrid.SetPointWeightScale(new Vector2I(x, y), GetTerrainDifficulty((int)Terrain.TerrainLayer.Base, new Vector2I(x, y)));
                }
            }
        }
    }

    public void AddConstructionToPathfinding(int layer, Vector2I terrainPos)
    {
        astarGrid.SetPointWeightScale(terrainPos, GetTerrainDifficulty(layer, terrainPos));
    }

    public int GetTerrainDifficulty(int layer, Vector2I coords)
    {
        int sourceId = terrain.GetCellSourceId(layer, coords, false);
        var source = (TileSetAtlasSource)terrain.TileSet.GetSource(sourceId);
        var atlasCoords = terrain.GetCellAtlasCoords(layer, coords, false);
        var tileData = source.GetTileData(atlasCoords, 0);

        return (int)tileData.GetCustomData("walk_difficulty");
    }
}
