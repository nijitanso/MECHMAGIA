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

        public List<StateSnapshot> states = [];

        public override void _EnterTree()
        {
            Inst = this;
        }

        /// <summary>
        /// 往快照序列中压入一个快照
        /// </summary>
        /// <param name="counters"></param>
        public void SaveState(Dictionary<int, Counter> counters)
        {
            GD.Print("Saving state snapshot");
            StateSnapshot state = new(counters);
            states.Add(state);
        }

        /// <summary>
        /// 弹出最后一个快照，并将游戏状态（目前只是算子位置和移动力）恢复为弹出的那个快照所保存的状态
        /// </summary>
        /// <param name="main"></param>
        public void UndoState(Main main)
        {
            GD.Print("Undoing state snapshot");
            if (states.Count > 0)
            {
                // 从末尾弹出最后一个状态快照
                StateSnapshot lastState = states[^1];
                states.RemoveAt(states.Count - 1);

                // 恢复算子位置和移动力
                foreach (var id in main.Counters.Keys)
                {
                    main.Counters[id].UnitInfo.Coor = lastState.Units[id].Coor;
                    main.Counters[id].RefreshPosition();  // 刷新算子在场景中的位置
                    main.Counters[id].UnitInfo.MoveLeft = lastState.Units[id].MoveLeft;
                    main.Counters[id].UnitInfo.StackIndex = lastState.Units[id].StackIndex;
                }

            }
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

