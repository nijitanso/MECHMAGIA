using Godot;
using HexGrid;
using System;
using System.Collections.Generic;

public partial class MapInteraction3 : TileMapLayer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ShowZoc(List<AxialCoor> coors)
	{

		foreach (var coor in coors)
		{
			
            SetCell(coor.AxialToOffset(), 0, new Vector2I(0, 0));
        }
    }

	public void RemoveZoc(List<AxialCoor> coors)
	{
		foreach (var coor in coors)
		{
			EraseCell(coor.AxialToOffset());
		}
	}
}
