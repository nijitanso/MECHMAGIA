using Godot;
using System;
using Data;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Counter = 1,
        Null = 2,
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

    public void SetIsStackHoveredAsTrue()
    {
        IsStackHovered = true;
    }

    public void SetIsStackHoveredAsFalse()
    {
        IsStackHovered = false;
    }

    /// <summary>
    /// 事件处理器，响应算子的MouseExited事件，切换鼠标的悬停实体类型为地格（这里以后内容多了后可能逻辑有问题，因为离开了算子并不代表鼠标一定在地格之上）
    /// </summary>
    public void HoverSwitchToMap()
    {
        HoverState = HoverStateEnum.Map;
    }

    public async void ShowTestMessage()
    {
        while (true)
        {
            GD.Print(HoverState);
            await Task.Delay(500);

            /*
            string a = "";
            foreach (var unit in SelectedUnits)
            {
                a += unit.ID.ToString();
            }
            if (a != "")
            {
                GD.Print(a);
            }

            */
        }

        


        
    }

    /// <summary>
    /// 事件处理器，响应算子的SelectUnit事件，先触发SwitchCounter事件，
    /// 通知所有算子取消选中（逻辑是先取消所有被选中算子，再选中所点击的算子），切换鼠标的选中实体类型为算子
    /// </summary>
    /// <param name="_"></param>
    /// <param name="_1"></param>
    /// <param name="ID"></param>
    public void SelectSwitchToCounter(UnitInfo unitInfo)
    {
        OnSwitchCounter();

        SelectState = SelectStateEnum.Counter;
    }

    /// <summary>
    /// 将选中状态设为地格，并记录此时被选中的地格坐标
    /// </summary>
    /// <param name="coor"></param>
    public void SelectSwitchToMap(Vector2I coor)
    {
        SelectState = SelectStateEnum.Map;
        SelectedMapCoor = coor;

    }

    /// <summary>
    /// 将此时悬停的单位设置为参数传入的那个UnitInfo实例
    /// </summary>
    /// <param name="unit"></param>
    public void SetHoveringUnit(UnitInfo unit)
    {
        HoveringUnit = unit;
    }



    /// <summary>
    /// 事件处理器，响应Counter的SelectUnit事件。将事件传入的UnitInfo实例加入SelectedUnits序列中
    /// </summary>
    /// <param name="unit"></param>
    public void SetSelectedUnits(UnitInfo unit)
    {
        SelectedUnits.Add(unit);
    }

    /// <summary>
    /// 清空选中算子序列
    /// </summary>
    public void ClearSelectedUnits()
    {
        SelectedUnits.Clear();
    }

    /// <summary>
    /// 事件处理器，响应Counter的MultiDeselectUnit事件。从SelectedUnits序列中移除事件传入的算子
    /// </summary>
    /// <param name="unit"></param>
    public void RemoveSelectedUnits(UnitInfo unit)
    {
        SelectedUnits.Remove(unit);
    }

    /// <summary>
    /// 设置悬停单位为ID为-1的空单位
    /// </summary>
    public void SetHoveringUnitToNull()
    {
        HoveringUnit = _nullUnit;
    }

    public static MouseManager Inst { get; private set; }   // 单例属性，其他类通过这个单例来使用MouseManager

    // 为了确保 Instance 在场景树中唯一，使用 _EnterTree 方法来设置 Instance（单例）
    public override void _EnterTree()
    {
        Inst = this;
    }


    // 这些是过时的属性，用处不大
    public bool IsMouseLeftHolding { get; set; } = false;
    public double MouseLeftHoldingTime { get; set; }
    public long ClickedMouseButton { get; set; }

    // 鼠标管理器的状态枚举属性
    public HoverStateEnum HoverState { get; set; }
    public SelectStateEnum SelectState { get; set; } = SelectStateEnum.Null;

    public bool IsStackHovered { get; set; } = false;

    // 被选中或悬停的实例的具体属性
    public Vector2I SelectedMapCoor { get; set; }
    public List<UnitInfo> SelectedUnits { get; set; } = new List<UnitInfo>();
    public UnitInfo HoveringUnit { get; set; } = new UnitInfo();    // 当HoveringUnit.ID == -1时，说明没有悬停的算子


    private readonly UnitInfo _nullUnit = new UnitInfo() { ID = -1 };    // 私有字段用来储存没有悬停在算子上时HoveringUnit所引用的对象

    // 对节点的引用
    private Label mouseClickTimeShower;
    private Map _map;
    // Called when the node enters the scene tree for the first time.


    public override void _Ready()
    {
        // 等待所有节点被加载进场景树后进行初始化
        CallDeferred(nameof(InitializeNode));

        //ShowTestMessage();

    }

    /// <summary>
    /// 初始化获得这些对节点的引用（因为单例加载进场景树的顺序比普通节点要快，所以要延后调用这个方法）
    /// </summary>
    private void InitializeNode()
    {
        mouseClickTimeShower = GetNode<Label>("/root/Main/MouseClickTimeShower");
        _map = GetNode<Map>("/root/Main/Map");
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


        // 这里面是非常过时的代码
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



    public void OnEnterCounter()
    {
        EmitSignal(SignalName.EnterCounter);
    }

    [Signal] public delegate void EnterCounterEventHandler();


    public void OnSwitchCounter()
    {
        EmitSignal(SignalName.SwitchCounter);
    }

    [Signal] public delegate void SwitchCounterEventHandler();
}
