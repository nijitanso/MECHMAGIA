using Godot;
using System;
using System.Collections.Generic;

public partial class MapInteraction2 : TileMapLayer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}



	public void GreenHighlight(Vector2I coor)
	{
        SetCell(coor, 0, new Vector2I(0, 0));
    }

    public void RemoveGreenHighlight(List<Vector2I> coors)
    {
        foreach (var coor in coors)
        {
            EraseCell(coor);
        }
        
    }



}
