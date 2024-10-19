using Godot;
using System;

public partial class CameraController : Camera2D
{
    [Export] public float ZoomSpeed { get; set; } = 10f;

    private Vector2 zoomTarget;
    private Vector2 dragStartMousePos = Vector2.Zero;
    private Vector2 dragStartCameraPos = Vector2.Zero;
    private bool isDragging = false;

    public override void _Ready()
    {
        zoomTarget = Zoom;
    }

    public override void _Process(double delta)
    {
        ProcessZoom((float)delta);
        SimplePan((float)delta);
        ClickAndDrag();
    }

    private void ProcessZoom(float delta)
    {
        if (Input.IsActionJustPressed("camera_zoom_in"))
        {
            zoomTarget *= 1.1f;
        }

        if (Input.IsActionJustPressed("camera_zoom_out"))
        {
            zoomTarget *= 0.9f;
        }

        Zoom = Zoom.Lerp(Zoom, ZoomSpeed * delta);
    }

    private void SimplePan(float delta)
    {
        Vector2 moveAmount = Vector2.Zero;

        if (Input.IsActionPressed("camera_move_right"))
        {
            moveAmount.X += 1;
        }

        if (Input.IsActionPressed("camera_move_left"))
        {
            moveAmount.X -= 1;
        }

        if (Input.IsActionPressed("camera_move_up"))
        {
            moveAmount.Y -= 1;
        }

        if (Input.IsActionPressed("camera_move_down"))
        {
            moveAmount.Y += 1;
        }

        moveAmount = moveAmount.Normalized();
        Position += moveAmount * delta * 1000 * (1 / Zoom.X);
    }

    private void ClickAndDrag()
    {
        if (!isDragging && Input.IsActionJustPressed("camera_pan"))
        {
            dragStartMousePos = GetViewport().GetMousePosition();
            dragStartCameraPos = Position;
            isDragging = true;
        }

        if (isDragging && Input.IsActionJustReleased("camera_pan"))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector2 moveVector = GetViewport().GetMousePosition() - dragStartMousePos;
            Position = dragStartCameraPos - moveVector * (1 / Zoom.X);
        }
    }
}
