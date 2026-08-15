using Godot;
using HexGrid;
using System;
using System.Collections.Generic;

public partial class MapInteraction : TileMapLayer
{
    //  设置初始的“前一个格子”坐标
    public Vector2I PreciousCell { get; set; } = new Vector2I(-999, -999);
    public Vector2I PreciousClickedCell { get; set; } = new Vector2I(-999, -999);
    public List<Vector2I> CellOffsetCoors { get; set; }


    private Counter _counter;

    private bool _isAnyCellHighlight = false;

    //  设置鼠标左键按下的状态和按下的时间，用于区分点击和长按

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _counter = GetNode<Counter>("/root/Game/Counter");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //Test();
        //  获取当前鼠标在网格中的坐标
        Vector2 localMousePosition = GetLocalMousePosition();
        Vector2I currentCellMousePosition = LocalToMap(localMousePosition);
        AxialCoordinates currentCellAxialPosition = AxialCoordinates.OffsetToAxial(currentCellMousePosition);
        //GD.Print($"当前鼠标所在的偏移坐标：{currentCellMousePosition}");
        //GD.Print($"当前鼠标所在的轴向坐标：{currentCellAxialPosition}");

        //  获取当前鼠标点击的按钮，返回值是一个 long 类型的整数，表示鼠标按钮的掩码，1表示左键，2表示右键，4表示中键，8表示第四个按钮，16表示第五个按钮，以此类推


        //GD.Print($"当前鼠标所在的坐标：{currentCellMousePosition}，当前鼠标点击的按钮掩码：{clickedMouseButton}");

        //  获取当前鼠标所在的坐标上的瓦片 ID，如果返回 -1，说明鼠标所在的坐标上没有瓦片
        int cellId = GetCellSourceId(currentCellMousePosition);
        if (cellId != -1)
        {

            //  判断鼠标是否点击了左键，如果是则调用 ClickCell 方法
            if (MouseManager.Instance.ClickedMouseButton == 1 && MouseManager.Instance.MouseLeftHoldingTime <= 0.01 && !_counter.IsHovering)
            {
                ClickCell(currentCellMousePosition);
            }

            if (MouseManager.Instance.ClickedMouseButton == 2 && MouseManager.Instance.MouseLeftHoldingTime <= 0.01)
            {
                OnSelectCoor(currentCellMousePosition);
            }

            if (_counter.IsHovering)
            {
                
                if (_isAnyCellHighlight)
                {
                    QuitPreciousCell(currentCellMousePosition);
                    _isAnyCellHighlight = false;
                }

            }
            else
            {
                EnterNewCell(currentCellMousePosition);
            }

            if (currentCellMousePosition != PreciousCell)
            {

                EnterNewCell(currentCellMousePosition);


                //  判断鼠标是否从没有瓦片的坐标进入，如果是则不用调用 QuitPreciousCell 方法，因为根本没有先前瓦片
                if (PreciousCell != new Vector2I(-999, -999))
                {
                    QuitPreciousCell(PreciousCell);
                }
                PreciousCell = currentCellMousePosition;

            }


        }
        else
        {
            if (PreciousCell != new Vector2I(-999, -999))
            {
                QuitPreciousCell(PreciousCell);
                PreciousCell = new Vector2I(-999, -999);
            }
        }
    }

    public void EnterNewCell(Vector2I cellPosition)
    {
        //  高光实现逻辑是直接修改当前坐标的瓦片
        if (cellPosition != PreciousClickedCell)
        {
            SetCell(cellPosition, 2, new Vector2I(0, 0));
            _isAnyCellHighlight = true;
        }

        //GD.Print($"Enter New Cell: {cellPosition}");
    }

    public void QuitPreciousCell(Vector2I cellPosition)
    {
        GD.Print("退出方法被触发");
        if (cellPosition != PreciousClickedCell)
        {
            SetCell(cellPosition, 2, new Vector2I(1, 0));
        }

        //GD.Print($"Quit Precious Cell: {cellPosition}");
    }

    public void ClickCell(Vector2I cell)
    {
        GD.Print("点击方法被触发");
        if (cell != PreciousClickedCell)
        {
            GD.Print("进入判断体");
            SetCell(cell, 2, new Vector2I(2, 0));
            SetCell(PreciousClickedCell, 2, new Vector2I(1, 0));
            GD.Print($"Click Cell: {cell}");
            PreciousClickedCell = cell;
        }
    }

    public void OnSelectCoor(Vector2I cell)
    {
        EmitSignal(SignalName.SelectCoor, cell);
    }


    [Signal] public delegate void SelectCoorEventHandler(Vector2I coor);
}
