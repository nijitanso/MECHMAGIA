using ActionProcessor;
using Godot;
using System;

public partial class AttackMessage : Label
{



    public override void _Ready()
    {
        
    }


    public override void _Process(double delta)
    {
    }

    public async void Display()
    {
        int ratio = AttackProcessor.Inst.Ratio;
        int dice = AttackProcessor.Inst.Dice;
        string ratioText;

        if (ratio == 0)
        {
            ratioText = "1/2";
        }
        else
        {
            ratioText = $"{ratio}/1";
        }


        string text = $"进攻：点数比为{ratioText}，骰子点数为{dice}，战斗结果为{AttackProcessor.Inst.CR}";

        Visible = true;
        Text = text ;

        await ToSignal(GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);

        Visible = false;

    }
}
