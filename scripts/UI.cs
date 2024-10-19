using Godot;
using System;
using System.Linq;

public partial class UI : CanvasLayer
{
    private ItemManager itemManager;
    private TaskManager taskManager;
    private Terrain terrain;
    private UIDrawingLayer drawLayer;

    private UIMode currentUIMode = UIMode.NORMAL;
    private Task.Orders currentOrder = Task.Orders.Cancel;
    private Node placingPrototype = null;

    private Vector2 startPlacingPos = Vector2.Zero;

    private float buttonHeight = 0.05f;

    public enum PlacingMode 
    { 
        SINGLE, 
        ROW, 
        RECTANGLE 
    }

    public enum UIMode 
    { 
        NORMAL, 
        PLACING, 
        DRAGGING, 
        ORDERING, 
        ORDERDRAGGING 
    }

    public override void _Ready()
    {
        itemManager = GetNode<ItemManager>("../ItemManager");
        taskManager = GetNode<TaskManager>("../TaskManager");
        terrain = GetNode<Terrain>("../Terrain");
        drawLayer = GetNode<UIDrawingLayer>("../UIDrawingLayer");
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("click_cancel"))
        {
            placingPrototype = null;
            terrain.ClearLayer((int)Terrain.TerrainLayer.UIGhosts);
            drawLayer.Clear();
            OnItemSelect(null);
            ChangeUIMode(UIMode.NORMAL);
        }

        switch (currentUIMode)
        {
            case UIMode.NORMAL:
                if (Input.IsActionJustPressed("click"))
                {
                    startPlacingPos = GetMousePositionToTerrainPos();
                }

                if (Input.IsActionJustReleased("click"))
                {
                    if (new Vector2I(Mathf.RoundToInt(startPlacingPos.X), Mathf.RoundToInt(startPlacingPos.Y)) == GetMousePositionToTerrainPos())
                    {
                        var itemAtPos = terrain.GetItemAtPosition(new Vector2I(Mathf.RoundToInt(startPlacingPos.X), Mathf.RoundToInt(startPlacingPos.Y)));
                        OnItemSelect(itemAtPos);
                    }
                }
                break;

            case UIMode.PLACING:
                DrawPrototypeAtCursor();
                if (Input.IsActionJustPressed("click"))
                {
                    startPlacingPos = GetMousePositionToTerrainPos();
                    ChangeUIMode(UIMode.DRAGGING);
                }
                break;

            case UIMode.DRAGGING:
                DrawPrototypeInLine();
                if (Input.IsActionJustReleased("click"))
                {
                    PlaceConstructionOrders();
                    ChangeUIMode(UIMode.PLACING);
                }
                break;

            case UIMode.ORDERING:
                DrawCrosshairAtCursor();
                if (Input.IsActionJustPressed("click"))
                {
                    startPlacingPos = GetMousePositionToTerrainPos();
                    ChangeUIMode(UIMode.ORDERDRAGGING);
                }
                break;

            case UIMode.ORDERDRAGGING:
                DrawOrderingRectangle();
                if (Input.IsActionJustReleased("click"))
                {
                    PlaceOrders();
                    drawLayer.Clear();
                    ChangeUIMode(UIMode.ORDERING);
                }
                break;
        }
    }

    private void ChangeUIMode(UIMode mode)
    {
        currentUIMode = mode;
    }

    private void OnItemSelect(Node item)
    {
        GetNode<Panel>("pnl_selected_item").SelectItem(item);
        if (item != null)
        {
            DrawCrosshairAtCursor();
        }
    }

    private void PlaceConstructionOrders()
    {
        GD.Print("build " + placingPrototype.Name);
        foreach (var pos in terrain.GetUsedCells((int)Terrain.TerrainLayer.UIGhosts))
        {
            terrain.PlaceConstructionOrder((Construction)placingPrototype, pos);
        }
        terrain.ClearLayer((int)Terrain.TerrainLayer.UIGhosts);
    }

    private void DrawPrototypeAtCursor()
    {
        terrain.ClearLayer((int)Terrain.TerrainLayer.UIGhosts);
        terrain.SetCell(Terrain.TerrainLayer.UIGhosts, GetMousePositionToTerrainPos(), placingPrototype.TileMapIndex, placingPrototype.TileMapPos);
    }

    private void DrawCrosshairAtCursor()
    {
        terrain.ClearLayer((int)Terrain.TerrainLayer.UIGhosts);
        terrain.SetCell((int)Terrain.TerrainLayer.UIGhosts, GetMousePositionToTerrainPos(), 2, new Vector2I(0, 2));
    }

    private void DrawOrderingRectangle()
    {
        var mousePos = GetMousePositionToTerrainPos();
        drawLayer.DrawRectAroundTerrainPositions((Vector2I)startPlacingPos, mousePos);
        DrawCrosshairAtCursor();
    }

    private void DrawPrototypeInLine()
    {
        var mousePos = GetMousePositionToTerrainPos();
        var diffX = Mathf.Abs(mousePos.X - startPlacingPos.X);
        var diffY = Mathf.Abs(mousePos.Y - startPlacingPos.Y);

        terrain.ClearLayer((int)Terrain.TerrainLayer.UIGhosts);

        if (diffX > diffY) // Horizontal
        {
            var range = Enumerable.Range((int)startPlacingPos.X, (int)(mousePos.X - startPlacingPos.X + 1)).ToArray();
            if (mousePos.X < startPlacingPos.X)
            {
                range = Enumerable.Range((int)mousePos.X, (int)(startPlacingPos.X - mousePos.X + 1)).Reverse().ToArray();
            }

            foreach (var i in range)
            {
                terrain.SetCell(Terrain.TerrainLayer.UIGhosts, new Vector2I(i, (int)startPlacingPos.Y), placingPrototype.TileMapIndex, placingPrototype.TileMapPos);
            }
        }
        else
        {
            var range = Enumerable.Range((int)startPlacingPos.Y, (int)(mousePos.Y - startPlacingPos.Y + 1)).ToArray();
            if (mousePos.Y < startPlacingPos.Y)
            {
                range = Enumerable.Range((int)mousePos.Y, (int)(startPlacingPos.Y - mousePos.Y + 1)).Reverse().ToArray();
            }

            foreach (var i in range)
            {
                terrain.SetCell(Terrain.TerrainLayer.UIGhosts, new Vector2I((int)startPlacingPos.X, i), placingPrototype.TileMapIndex, placingPrototype.TileMapPos);
            }
        }
    }

    private Vector2I GetMousePositionToTerrainPos()
    {
        var mousePos = GetViewport().GetMousePosition();
        return new Vector2I((int)(mousePos.X / 16), (int)(mousePos.Y / 16));
    }

    public void BeginPlacing(Node buildable)
    {
        GD.Print("build: " + buildable.GetPath());
        placingPrototype = buildable.Instantiate();
        ChangeUIMode(UIMode.PLACING);
        terrain.SetLayerModulate((int)Terrain.TerrainLayer.UIGhosts, new Color(1, 1, 1, 0.4f));
        CloseMenus();
    }

    private void PlaceOrders()
    {
        var mousePos = GetMousePositionToTerrainPos();
        var targetItems = terrain.FindOrderableItems((Vector2I)startPlacingPos, mousePos, currentOrder);
        foreach (var item in targetItems)
        {
            taskManager.AddOrder(currentOrder, item);
        }
    }

    public void BeginOrdering(Task.Orders order)
    {
        GD.Print("received order: " + order);
        currentOrder = order;
        ChangeUIMode(UIMode.ORDERING);
        CloseMenus();
    }

    private void CloseMenus()
    {
        foreach (var child in GetChildren())
        {
            child.CloseMenus();
        }
    }
}
