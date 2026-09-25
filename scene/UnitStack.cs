using Godot;
using System;

using System.Collections.Generic;
using MM = Managers.MouseManager;

namespace Data
{
    /// <summary>
    /// 表示算子堆叠的类
    /// </summary>
    public partial class UnitStack : RefCounted
    {
        /// <summary>
        /// 构造算子堆叠
        /// </summary>
        /// <param name="unit">初始加入堆叠的算子</param>
        /// <param name="position">堆叠在场景中的坐标位置</param>
        /// <param name="main">主场景引用，用于挂载遮罩节点</param>
        public UnitStack(UnitInfo unit, Vector2 position, Main main)
        {
            _stackMask = GD.Load<PackedScene>("res://scene/StackMask.tscn");

            AddUnit(unit);
            CoorPosition = position;

            _main = main;

            StackIncreasedTo2 += FormMask;
            StackDecreasedTo1 += RemoveMask;
        }

        /// <summary>
        /// 堆叠数量增加到 2 时生成遮罩，并绑定鼠标进入/离开事件
        /// </summary>
        private void FormMask()
        {
            GD.Print("forming mask");
            StackMask = _stackMask.Instantiate<Area2D>();
            StackMask.Position = new Vector2(CoorPosition.X + 5, CoorPosition.Y - 5);
            _main.AddChild(StackMask);

            StackMask.MouseEntered += SetHoveringStack;
            StackMask.MouseExited += MM.Inst.RemoveHoveringStack;

        }

        /// <summary>
        /// 将当前堆叠设置为鼠标悬停的堆叠
        /// </summary>
        public void SetHoveringStack()
        {
            MM.Inst.SetHoveringStack(this);
        }

        /// <summary>
        /// 堆叠数量减少到 1 时移除遮罩
        /// </summary>
        private void RemoveMask()
        {
            if (StackMask != null)
            {
                StackMask.QueueFree();
                StackMask = null;
            }
        }

        /// <summary>
        /// 堆叠中包含的所有算子
        /// </summary>
        public List<UnitInfo> Units { get; set; } = new List<UnitInfo>();
        /// <summary>
        /// 堆叠在场景中的坐标位置
        /// </summary>
        public Vector2 CoorPosition { get; set; }

        /// <summary>
        /// 各算子在场景中的矩形区域
        /// </summary>
        public List<Rect2> UnitRects { get; set; } = new List<Rect2>();

        /// <summary>
        /// 遮罩场景资源
        /// </summary>
        private PackedScene _stackMask;
        /// <summary>
        /// 当前显示的遮罩节点，未生成时为 null
        /// </summary>
        public Area2D StackMask { get; set; } = null;
        /// <summary>
        /// 主场景引用
        /// </summary>
        private Main _main;



        /// <summary>
        /// 向堆叠中添加一个算子
        /// </summary>
        /// <param name="unit">要加入的算子</param>
        public void AddUnit(UnitInfo unit)
        {
            Units.Add(unit);
            UnitRects.Add(unit.Rect);

            if (Units.Count > 1 && StackMask == null)
            {
                OnStackIncreasedTo2();
            }

            OnStackChanged();

        }
        /// <summary>
        /// 从堆叠中移除一个算子
        /// </summary>
        /// <param name="unit">要移除的算子</param>
        public void RemoveUnit(UnitInfo unit)
        {
            Units.Remove(unit);
            UnitRects.Remove(unit.Rect);


            if (Units.Count == 1)
            {
                OnStackDecreasedTo1();
            }

            OnStackChanged();
        }
        /// <summary>
        /// 获取堆叠中算子的数量
        /// </summary>
        /// <returns>算子数量</returns>
        public int GetCount()
        {
            return Units.Count;
        }

        /// <summary>
        /// 获取指定算子在堆叠中的索引
        /// </summary>
        /// <param name="unit">要查询的算子</param>
        /// <returns>算子的索引，不存在时返回 -1</returns>
        public int UnitIndexOf(UnitInfo unit)
        {
            return Units.IndexOf(unit);
        }

        /// <summary>
        /// 在传入的总算子实例里遍历本堆叠中的部分算子，将悬停的算子设置为不透明，而其余算子设置为半透明。
        /// 之所以要由Main.HighlightFromStack来调用是因为只有它持有总算子实例的序列
        /// </summary>
        /// <param name="counters">所有在场上的算子实例</param>
        /// <param name="hoveringunit">当前鼠标悬停的算子</param>
        public void HighLight(Dictionary<int, Counter> counters, UnitInfo hoveringunit)
        {
            foreach (var unit in Units)
            {

                Counter counter = counters[unit.ID];

                if (unit == hoveringunit)
                {

                    counter.Modulate = new Color(1, 1, 1, 1); // 设置为不透明
                    continue;
                }

                counter.Modulate = new Color(1, 1, 1, 0.1f); // 设置为半透明
            }
        }




        /// <summary>
        /// 触发 StackIncreasedTo2 事件
        /// </summary>
        protected virtual void OnStackIncreasedTo2()
        {
            StackIncreasedTo2?.Invoke(); // ?.是null条件运算符，表示如果StackUpdated不为null，则调用它
        }

        /// <summary>
        /// 堆叠数量增加到 2 时触发
        /// </summary>
        public event Action StackIncreasedTo2;

        /// <summary>
        /// 触发 StackDecreasedTo1 事件
        /// </summary>
        protected virtual void OnStackDecreasedTo1()
        {
            StackDecreasedTo1?.Invoke();
        }

        /// <summary>
        /// 堆叠数量减少到 1 时触发
        /// </summary>
        public event Action StackDecreasedTo1;

        /// <summary>
        /// 触发 StackChanged 信号
        /// </summary>
        protected virtual void OnStackChanged()
        {
            EmitSignal(SignalName.StackChanged);
        }

        /// <summary>
        /// 堆叠内容发生变化时发出的信号
        /// </summary>
        [Signal] public delegate void StackChangedEventHandler();

    }
}
