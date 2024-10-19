using Godot;
using System;

public partial class ClickTarget : Area2D
{
    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event.IsActionPressed("click"))
        {
            GetParent().OnClick();
        }
    }
}
