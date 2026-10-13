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
        public TeamEnum Team { get; set; } = TeamEnum.Friend;




        /// <summary>
        /// 在当前回合中切换阶段。阶段是敌我双方交替进行的：玩家移动 → 敌人移动 → 玩家攻击 → 敌人攻击 → 下一回合
        /// </summary>
        public void MoveToNextPhase()
        {
            GD.Print("ChangePhrase");

            OnNextPhase();


            if (TurnState == TurnStateEnum.PlayerTurn)
            {
                TurnState = TurnStateEnum.EnemyTurn;
                Team = TeamEnum.Enemy;

                SwitchPhase(TeamEnum.Enemy);
            }
            else
            {
                TurnState = TurnStateEnum.PlayerTurn;
                Team = TeamEnum.Friend;

                if (TurnPhase == TurnPhaseEnum.MovementPhase)
                {
                    TurnPhase = TurnPhaseEnum.AttackPhase;
                }
                else
                {
                    TurnPhase = TurnPhaseEnum.MovementPhase;

                    MoveToNextTurn();
                }

                SwitchPhase(TeamEnum.Friend);
            }


        }

        public void MoveToNextTurn()
        {
            Turn++;
            OnNextTurn();
        }


        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent)
            {
                if ((keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.Space) && keyEvent.Pressed)
                {
                    MoveToNextPhase();
                }
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



        protected virtual void OnNextPhase()
        {
            EmitSignal(SignalName.NextPhase);
        }

        [Signal] public delegate void NextPhaseEventHandler();

        protected virtual void OnNextTurn()
        {
            EmitSignal(SignalName.NextTurn);
        }

        [Signal] public delegate void NextTurnEventHandler();

    }
}


