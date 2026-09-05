using Data;
using Godot;
using System;

namespace Managers
{
    public partial class TurnManager : Node
    {
        public static TurnManager Inst { get; private set; }

        public override void _EnterTree()
        {
            Inst = this;
        }

        public enum TurnStateEnum
        {
            PlayerTurn = 0,
            EnemyTurn = 1
        }

        public enum TurnPhaseEnum
        {
            MovementPhase = 0,
            AttackPhase = 1
        }




        public int Turn { get; set; } = 1;
        public TurnStateEnum TurnState { get; set; } = TurnStateEnum.PlayerTurn;
        public TurnPhaseEnum TurnPhase { get; set; } = TurnPhaseEnum.MovementPhase;




        /// <summary>
        /// 在当前回合中切换阶段。阶段是敌我双方交替进行的：玩家移动 → 敌人移动 → 玩家攻击 → 敌人攻击 → 下一回合
        /// </summary>
        public void NextPhase()
        {
            if (TurnState == TurnStateEnum.PlayerTurn)
            {
                TurnState = TurnStateEnum.EnemyTurn;
                SwitchPhase(TeamEnum.Enemy);
            }
            else
            {
                TurnState = TurnStateEnum.PlayerTurn;

                if (TurnPhase == TurnPhaseEnum.MovementPhase)
                {
                    TurnPhase = TurnPhaseEnum.AttackPhase;
                }
                else
                {
                    TurnPhase = TurnPhaseEnum.MovementPhase;

                    NextTurn();
                }

                SwitchPhase(TeamEnum.Friend);
            }
        }

        public void NextTurn()
        {
            Turn++;
        }


        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent)
            {
                if (keyEvent == Key.Enter)
            }
        }





        protected virtual void SwitchPhase(TeamEnum team)
        {
            if (TurnPhase == TurnPhaseEnum.MovementPhase)
            {
                EmitSignal(SignalName.SwitchToMovementPhase, (int)team);
            }
            else
            {
                EmitSignal(SignalName.SwitchToAttackPhase, (int)team);
            }

        }



        [Signal] public delegate void SwitchToMovementPhaseEventHandler(TeamEnum team);
        [Signal] public delegate void SwitchToAttackPhaseEventHandler(TeamEnum team);
    }
}


