using Godot;
using System;
using System.Collections.Generic;

public partial class UIDrawingLayer : Node2D
{
    private Vector2 start;
    private Vector2 end;
    private Color lineColor = new Color(1.0f, 0.0f, 1.0f); //紫色
    private float lineWidth = 0.5f;

    private List<Vector2> tracePoints = new List<Vector2>();

    public void DrawRectAroundTerrainPositions(Vector2I terrainStartPos, Vector2I terrainEndPos)
    {
        start = terrainStartPos * 16;
        end = terrainEndPos * 16;

        if (end.X >= start.X)
        {
            end.X += 16;
        }
        else
        {
            start.X += 16;
        }

        if (end.Y >= start.Y)
        {
            end.Y += 16;
        }
        else
        {
            start.Y += 16;
        }

        QueueRedraw(); //计划重绘
    }

    public void Clear()
    {
        start = Vector2.Zero;
        end = Vector2.Zero;
        QueueRedraw(); //计划重绘
    }

    public override void _Draw()
    {
        DrawLine(start, new Vector2(end.X, start.Y), lineColor, lineWidth, true);
        DrawLine(new Vector2(end.X, start.Y), end, lineColor, lineWidth, true);
        DrawLine(end, new Vector2(start.X, end.Y), lineColor, lineWidth, true);
        DrawLine(new Vector2(start.X, end.Y), start, lineColor, lineWidth, true);

        for (int i = 1; i < tracePoints.Count; i++)
        {
            DrawLine(tracePoints[i], tracePoints[i - 1], new Color(1.0f, 0.0f, 0.0f), 0.5f, true); // RED
        }
    }

    public void TracePoints(List<Vector2> points)
    {
        tracePoints = points;
        QueueRedraw(); //计划重绘
    }
}
