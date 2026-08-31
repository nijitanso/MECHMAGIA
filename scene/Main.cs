using Godot;
using System;
using System.Runtime.InteropServices.JavaScript;
using Data;
using System.Collections.Generic;

public partial class Main : Node2D
{
    // 对节点的引用
    private PackedScene _counter;
    private Node _friendUnits;
    private Map _map;

    public List<Counter> Units { get; set; } = new List<Counter>(); // 用于储存挂载在场景树的Counter实例的序列

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // 获取对节点的引用
        _counter = GD.Load<PackedScene>("res://scene/Counter.tscn");
        _friendUnits = GetNode<Node>("FriendUnits");
        _map = GetNode<Map>("Map");

        UnitDataJson.Initialize();  // 从JSON中获取单位的数据，将数据存储到UnitData这个静态类中（但似乎这个类意义并不大，因为数据全在UnitInfo实例里）


        Init(); // 初始化

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    /// <summary>
    /// 统一的初始化方法
    /// </summary>
    public void Init()
    {
        InstantiateUnits();
    }

    /// <summary>
    /// 初始化单位
    /// </summary>
    public void InstantiateUnits()
    {
        // 遍历静态类UnitData的UnitInfo实例序列
        foreach (var unit in UnitData.UnitSetup)
        {
            // 创建Counter实例，将UnitInfo赋值给其属性，让其获得数据
            Counter counter = _counter.Instantiate<Counter>();
            counter.UnitInfo = unit;

            // 将每个算子的MoveUnit的事件都用UnitsUpdate的触发方法绑定，每次算子移动时都要通知节点们更新算子状态
            counter.MoveUnit += OnUnitsUpdate;
            // 单位承受歼灭CR时触发该事件
            counter.RemoveCounter += RemoveCounterFromTree;

            if (counter != null)
            {
                // 将这个创建好的Counter实例加入序列和挂载到Main节点下
                Units.Add(counter);
                this.AddChild(counter);
            }
            else
            {
                GD.Print("有counter实例化失败！");
            }

            OnUnitsUpdate();  // 算子实例化完毕，通知其他节点更新算子状态

        }
    }

    /// <summary>
    /// 事件处理器，响应counter的RemoveCounter事件。将事件传入的Counter实例从场景树上移除
    /// </summary>
    /// <param name="counter"></param>
    public void RemoveCounterFromTree(Counter counter)
    {
        counter.UnitInfo.Coor = new Vector2I(-999, -999);   // 移动到“弃牌堆”（不是
        RemoveChild(counter);
        Units.Remove(counter);

        OnUnitsUpdate();
    }

    public void OnUnitsUpdate()
    {
        // 因为要传一个序列，所以要转成Godot的内置序列（可以被Variant类型容纳），接收参数的一方再转回C#原生类型
        EmitSignal(SignalName.UnitsUpdate, new Godot.Collections.Array<Counter>(Units));  
    }

  
    [Signal] public delegate void UnitsUpdateEventHandler(Godot.Collections.Array<Counter> units);


}
