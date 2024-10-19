using Godot;
using System;

public partial class PlayerCharacter : CharacterBody2D
{
    private TileMap terrain;
    private Pathfinding pathfinding;
    private ItemManager itemManager;

    private const float SPEED = 300.0f;

    private Vector2[] path = new Vector2[0];

    public override void _Ready()
    {
        terrain = GetNode<TileMap>("../Terrain");
        pathfinding = GetNode<Pathfinding>("../Pathfinding");
        itemManager = GetNode<ItemManager>("../ItemManager");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("click"))
        {
            Vector2 pos = Position / terrain.RenderingQuadrantSize;
            Vector2 targetPos = GetGlobalMousePosition() / terrain.RenderingQuadrantSize;

            path = pathfinding.RequestPath((Vector2I)pos, (Vector2I)targetPos);
        }

        if (path.Length > 0)
        {
            Vector2 direction = GlobalPosition.DirectionTo(path[0]);
            float terrainDifficulty = pathfinding.GetTerrainDifficulty(Terrain.TerrainLayer.Base, position / terrain.rendering_quadrant_size);
            Velocity = direction * SPEED * (1 / terrainDifficulty);

            if (Position.DistanceTo(path[0]) < SPEED * delta)
            {
                Array.Resize(ref path, path.Length - 1);
            }
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
    }
}
