using Godot;
using System;

public partial class MouseManager : Node
{
    /// <summary>
    /// 鼠标目前选中的实体，只区分选中实体类型，而不区分具体选中的实体
    /// </summary>
    public enum SelectStateEnum
    {
        /// <summary>
        /// 此时鼠标选中了一个地格
        /// </summary>
        Map = 0,
        /// <summary>
        /// 此时鼠标选中了一个算子
        /// </summary>
        Counter = 1
    }

    /// <summary>
    /// 鼠标目前悬停的实体，只区分悬停实体类型，而不区分具体悬停的实体
    /// </summary>
    public enum HoverStateEnum
    {
        /// <summary>
        /// 此时鼠标悬停于一个地格
        /// </summary>
        Map = 0,
        /// <summary>
        /// 此时鼠标悬停于一个算子
        /// </summary>
        Counter = 1
    }

    /// <summary>
    /// 事件处理器，响应算子的MouseEntered事件，切换鼠标的悬停实体类型为算子
    /// </summary>
    public void HoverSwitchToCounter()
    {
        HoverState = HoverStateEnum.Counter;
    }

    /// <summary>
    /// 事件处理器，响应算子的MouseExited事件，切换鼠标的悬停实体类型为地格（这里以后内容多了后可能逻辑有问题，因为离开了算子并不代表鼠标一定在地格之上）
    /// </summary>
    public void HoverSwitchToMap()
    {
        HoverState = HoverStateEnum.Map;
    }

    /// <summary>
    /// 事件处理器，响应算子的SelectUnit事件，切换切换鼠标的选中实体类型为算子，同时储存此时被选中的算子的ID，并触发SwitchCounter事件，
    /// 通知没有被选中的算子做出反应
    /// </summary>
    /// <param name="_"></param>
    /// <param name="_1"></param>
    /// <param name="ID"></param>
    public void SelectSwitchToCounter(Vector2I _, int _1, int ID)
    {
        SelectState = SelectStateEnum.Counter;
        SelectedUnitID = ID;

        OnSwitchCounter();
    }

    public static MouseManager Inst { get; private set; }   // 单例属性，其他类通过这个单例来使用MouseManager

    // 为了确保 Instance 在场景树中唯一，使用 _EnterTree 方法来设置 Instance（单例）
    public override void _EnterTree()
    {
        Inst = this;
    }



    public bool IsMouseLeftHolding { get; set; } = false;
    public double MouseLeftHoldingTime { get; set; }
    public long ClickedMouseButton { get; set; }
    public HoverStateEnum HoverState { get; set; }
    public SelectStateEnum SelectState { get; set; }
    public int SelectedUnitID { get; set; }

    private Label mouseClickTimeShower;
    // Called when the node enters the scene tree for the first time.


    public override void _Ready()
    {
        // 等待所有节点被加载进场景树后进行初始化
        CallDeferred(nameof(InitializeNode));
    }

    private void InitializeNode()
    {
        mouseClickTimeShower = GetNode<Label>("/root/Main/MouseClickTimeShower");
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

       

        ClickedMouseButton = (long)(Input.GetMouseButtonMask());
        if (ClickedMouseButton == 1)
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

        if (mouseClickTimeShower != null)
            mouseClickTimeShower.Text = $"鼠标左键按下时长：{MouseLeftHoldingTime:F2}";
    }


    public void OnSwitchCounter()
    {
        EmitSignal(SignalName.SwitchCounter);
    }

    [Signal] public delegate void SwitchCounterEventHandler();
}
