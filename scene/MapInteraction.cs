using Godot;
using System;

public partial class MapInteraction : TileMapLayer
{
    //  设置初始的“前一个格子”坐标
    public Vector2I PreciousCell { get; set; } = new Vector2I(-999, -999);
    public Vector2I PreciousClickedCell { get; set; } = new Vector2I(-999, -999);

    //  设置鼠标左键按下的状态和按下的时间，用于区分点击和长按
    public bool IsMouseLeftHolding { get; set; } = false;
    public double MouseLeftHoldingTime { get; set; }

    private Label mouseClickTimeShower;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        mouseClickTimeShower = GetNode<Label>("MouseClickTimeShower");
        if (mouseClickTimeShower == null)
        {
            GD.PrintErr("没有找到 MouseClickTimeShower 节点！");
        }
        else
        {
            mouseClickTimeShower.Text = "找到MouseClickTimeShower 节点！";
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //Test();
        //  获取当前鼠标在网格中的坐标
        Vector2 localMousePosition = GetLocalMousePosition();
        Vector2I currentCellMousePosition = LocalToMap(localMousePosition);

        //  获取当前鼠标点击的按钮，返回值是一个 long 类型的整数，表示鼠标按钮的掩码，1表示左键，2表示右键，4表示中键，8表示第四个按钮，16表示第五个按钮，以此类推
        long clickedMouseButton = (long)(Input.GetMouseButtonMask());
        if (clickedMouseButton == 1)
        {
            if (!IsMouseLeftHolding)
            {
                IsMouseLeftHolding = true;
                MouseLeftHoldingTime = 0;
            }
            else
            {
                MouseLeftHoldingTime += delta;
            }
        }
        else
        {
            if (IsMouseLeftHolding)
            {
                IsMouseLeftHolding = false;
                MouseLeftHoldingTime = 0;
            }
        }

        mouseClickTimeShower.Text = $"鼠标左键按下时间：{MouseLeftHoldingTime:F2} 秒";

        GD.Print($"当前鼠标所在的坐标：{currentCellMousePosition}，当前鼠标点击的按钮掩码：{clickedMouseButton}");

        //  获取当前鼠标所在的坐标上的瓦片 ID，如果返回 -1，说明鼠标所在的坐标上没有瓦片
        int cellId = GetCellSourceId(currentCellMousePosition);
        if (cellId != -1)
        {

            //  判断鼠标是否点击了左键，如果是则调用 ClickCell 方法
            if (clickedMouseButton == 1 && MouseLeftHoldingTime <= 0.01)
            {
                ClickCell(currentCellMousePosition);
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
        }

        GD.Print($"Enter New Cell: {cellPosition}");
    }

    public void QuitPreciousCell(Vector2I cellPosition)
    {
        if (cellPosition != PreciousClickedCell)
        {
            SetCell(cellPosition, 2, new Vector2I(1, 0));
        }

        GD.Print($"Quit Precious Cell: {cellPosition}");
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
}
