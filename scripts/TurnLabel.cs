using Godot;
using System;
using TM = Managers.TurnManager;

public partial class TurnLabel : Label
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

        TM.Inst.NextTurn += SetText;

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}




	public void SetText()
    {
		string nowTurn = TM.Inst.Turn.ToString();

		string text = $"第{nowTurn}回合";

        Text = text;
    }
}
