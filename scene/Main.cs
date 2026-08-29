using Godot;
using System;
using System.Runtime.InteropServices.JavaScript;
using Data;
using System.Collections.Generic;

public partial class Main : Node2D
{

    private PackedScene _counter;
    private Node _friendUnits;
    private Map _map;

    public List<Counter> Units { get; set; } = new List<Counter>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _counter = GD.Load<PackedScene>("res://scene/Counter.tscn");
        _friendUnits = GetNode<Node>("FriendUnits");
        _map = GetNode<Map>("Map");



        UnitDataJson.Initialize();


        Init();

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void Init()
    {
        InstantiateUnits();
    }

    public void InstantiateUnits()
    {
        foreach (var unit in UnitData.UnitSetup)
        {
            Counter counter = _counter.Instantiate<Counter>();
            counter.UnitInfo = unit;

            // 将每个算子的MoveUnit的事件都用UnitsUpdate的触发方法绑定，每次算子移动时都要通知节点们更新算子状态
            counter.MoveUnit += OnUnitsUpdate;
            counter.RemoveCounter += RemoveCounterFromTree;

            if (counter != null)
            {
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

    public void RemoveCounterFromTree(Counter counter)
    {
        RemoveChild(counter);
    }

    public void OnUnitsUpdate()
    {
        // 因为要传一个序列，所以要转成Godot的内置序列（可以被Variant类型容纳），接收参数的一方再转回C#原生类型
        EmitSignal(SignalName.UnitsUpdate, new Godot.Collections.Array<Counter>(Units));  
    }

  
    [Signal] public delegate void UnitsUpdateEventHandler(Godot.Collections.Array<Counter> units);


}
