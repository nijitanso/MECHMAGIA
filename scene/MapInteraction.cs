using Godot;
using HexGrid;
using System;
using System.Collections.Generic;

public partial class MapInteraction : TileMapLayer
{
    //  设置初始的“前一个格子”坐标
    

    //  设置鼠标左键按下的状态和按下的时间，用于区分点击和长按

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //Test();
        
    }

    public void EnterNewCell(Vector2I cellPosition, Vector2I PreciousClickedCell)
    {
        //  高光实现逻辑是直接修改当前坐标的瓦片
        if (cellPosition != PreciousClickedCell)
        {
            SetCell(cellPosition, 2, new Vector2I(0, 0));
        }

        //GD.Print($"Enter New Cell: {cellPosition}");
    }

    public void QuitPreciousCell(Vector2I cellPosition, Vector2I PreciousClickedCell)
    {
        //GD.Print("退出方法被触发");
        if (cellPosition != PreciousClickedCell)
        {
            SetCell(cellPosition, 2, new Vector2I(1, 0));
        }

        //GD.Print($"Quit Precious Cell: {cellPosition}");
    }

    public void ClickCell(Vector2I cell, Vector2I PreciousClickedCell)
    {
        //GD.Print("点击方法被触发");
        if (cell != PreciousClickedCell)
        {
            //GD.Print("进入判断体");
            SetCell(cell, 2, new Vector2I(2, 0));
            SetCell(PreciousClickedCell, 2, new Vector2I(1, 0));
            //GD.Print($"Click Cell: {cell}");
        }
    }

    
}
