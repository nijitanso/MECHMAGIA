using Godot;
using System;
using System.Collections.Generic;
using Data;
using System.Linq;


namespace Managers
{
    /// <summary>
    /// 单例，用于管理每回合中单位行动的状态，以方便调试和实现撤回功能
    /// </summary>
    public partial class StateManager : Node
    {
        public static StateManager Inst { get; private set; }

        public StateSnapshot LastState { get; set; } = null;
        public StateSnapshot CurrentState { get; set; } = null;
        public bool IsUndone { get; set; } = false;

        public override void _EnterTree()
        {
            Inst = this;
            TurnManager.Inst.NextPhase += ClearState;  // 每次进入下一阶段时清除所有保存的状态
        }

        /// <summary>
        /// 保存当前游戏状态的快照
        /// </summary>
        /// <param name="counters"></param>
        public void SaveState(Dictionary<int, Counter> counters)
        {
            GD.Print("Saving state snapshot");
            LastState = new(counters);  // 保存是比实际操作快的，所以实际上保存的是操作前的状态
            CurrentState = null;  // 保存状态时将当前状态清空，表示目前没有撤回的状态
            IsUndone = false;  // 每次保存状态时都将撤回状态标记重置为false,表示目前不处于撤回状态

        }

        /// <summary>
        /// 将游戏状态（目前只是算子位置和移动力）撤回（应用LastState快照）。撤回成功将返回true
        /// </summary>
        /// <param name="main"></param>
        public bool UndoState(Main main)
        {
            GD.Print("Undoing state snapshot");
            if (LastState == null || IsUndone) return false;

            CurrentState = new(main.Counters);   // 将当前状态保存，以便之后可以再次还原，撤回只能一步
            ApplyState(main, LastState);
            IsUndone = true;
            
            return true;
        }

        /// <summary>
        /// 恢复被撤回的状态（应用CurrentState快照）。恢复成功将返回true
        /// </summary>
        /// <param name="main"></param>
        public bool RedoState(Main main)
        {
            GD.Print(IsUndone);
            if (CurrentState == null || !IsUndone) return false;

            ApplyState(main, CurrentState);
            IsUndone = false;

            return true;
        }

        /// <summary>
        /// 应用状态快照保存的信息
        /// </summary>
        /// <param name="main"></param>
        public void ApplyState(Main main, StateSnapshot state)
        {
            foreach (var id in main.Counters.Keys)
            {
                UnitInfo unitInfo = main.Counters[id].UnitInfo;

                unitInfo.Coor = state.Units[id].Coor;
                main.Counters[id].RefreshPosition();  // 刷新算子在场景中的位置

                unitInfo.MoveLeft = state.Units[id].MoveLeft;
                if (IsUndone)
                {
                    main.Counters[id].DisconnectMovement();  // 如果是撤回到之前的状态，就断开算子与移动力的事件连接和可移动图标
                }
                else
                {
                    main.Counters[id].RefreshMovement();  // 刷新算子在场景中的移动性
                }

                unitInfo.StackIndex = state.Units[id].StackIndex;
            }
        }
        
        /// <summary>
        /// 清除所有保存的状态
        /// </summary>
        public void ClearState()
        {
            LastState = null;
            CurrentState = null;
            IsUndone = false;
        }
    }
}

namespace Data
{
    /// <summary>
    /// 用于保存状态的快照类，储存每个状态时的UnitInfo的深拷贝和堆叠状态
    /// </summary>
    public partial class StateSnapshot : RefCounted
    {
        /// <summary>
        /// 将传入的算子字典深拷贝
        /// </summary>
        /// <param name="counters"></param>
        public StateSnapshot(Dictionary<int, Counter> counters)
        {
            Units = counters.ToDictionary(kvp => kvp.Key, kvp => new UnitInfo(kvp.Value.UnitInfo));
        }

        public Dictionary<int, UnitInfo> Units { get; set; }


    }
}

