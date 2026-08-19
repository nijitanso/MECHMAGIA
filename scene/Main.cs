using Godot;
using System;
using System.Runtime.InteropServices.JavaScript;
using Data;

public partial class Main : Node2D
{

    private PackedScene _counter;
    private Node _familyUnits;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _counter = GD.Load<PackedScene>("res://scene/Counter.tscn");
        _familyUnits = GetNode<Node>("FamilyUnits");



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
            counter.CoorOnHex = unit.Coor;
            _familyUnits.AddChild(counter);

        }
    }
}
