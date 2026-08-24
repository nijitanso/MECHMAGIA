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

            if (counter != null)
            {
                Units.Add(counter);
                _friendUnits.AddChild(counter);
            }
            else
            {
                GD.Print("有counter实例化失败！");
            }

            
            

        }
    }

    
}
