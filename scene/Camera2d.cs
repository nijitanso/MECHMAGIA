using Godot;
using System;

public partial class Camera2d : Camera2D
{
    public Vector2 preciousMousePosition { get; set; }
    [Export] public float zoomSpeed { get; set; } = 1.0f;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        preciousMousePosition = GetGlobalMousePosition();

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2 currentMousePosition = GetGlobalMousePosition();
        float deltaX =  currentMousePosition.X - preciousMousePosition.X;
        float deltaY = currentMousePosition.Y - preciousMousePosition.Y;

        long clickedMouseButton = (long)(Input.GetMouseButtonMask());

        if (clickedMouseButton == 4)
        {
            Vector2 offset = Offset;
            offset.X -= zoomSpeed * deltaX;
            offset.Y -= zoomSpeed * deltaY;
            Offset = offset;
        }

        if (clickedMouseButton == 0)
        {
            preciousMousePosition = currentMousePosition;
        }
    }
}
