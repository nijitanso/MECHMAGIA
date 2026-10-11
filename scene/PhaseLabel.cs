using Godot;
using System;
using Data;
using TM = Managers.TurnManager;

public partial class PhaseLabel : Label
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        TM.Inst.SwitchToMovementPhase += SetText;
        TM.Inst.SwitchToAttackPhase += SetText;

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }




    public void SetText(TeamEnum team)
    {
        TM.TurnPhaseEnum nowTurnPhase = TM.Inst.TurnPhase;

        string text = "";

        switch (team)
        {
            case TeamEnum.Friend:
                text += "我方";
                break;
            case TeamEnum.Enemy:
                text += "敌方";
                break;
            default:
                break;
        }

        switch (nowTurnPhase)
        {
            case TM.TurnPhaseEnum.MovementPhase:
                text += "机动";
                break;
            case TM.TurnPhaseEnum.AttackPhase:
                text += "进攻";
                break;
            default:
                break;
        }

        Text = text;
    }
}
