using Godot;
using System;
using System.Collections.Generic;
using MM = MouseManager;

namespace Data
{
    /// <summary>
    /// 表示算子堆叠的类
    /// </summary>
    public partial class UnitStack : RefCounted
    {
        public UnitStack(UnitInfo unit, Vector2 position, Main main)
        {
            _stackMask = GD.Load<PackedScene>("res://scene/StackMask.tscn");

            AddUnit(unit);
            CoorPosition = position;

            _main = main;

            StackIncreasedTo2 += FormMask;
            StackDecreasedTo1 += RemoveMask;
        }

        private void FormMask()
        {
            GD.Print("forming mask");
            StackMask = _stackMask.Instantiate<Area2D>();
            StackMask.Position = new Vector2(CoorPosition.X + 5, CoorPosition.Y - 5);
            _main.AddChild(StackMask);

            StackMask.MouseEntered += MM.Inst.SetIsStackHoveredAsTrue;
            StackMask.MouseExited += MM.Inst.SetIsStackHoveredAsFalse;

        }

        private void RemoveMask()
        {
            if (StackMask != null)
            {
                StackMask.QueueFree();
                StackMask = null;
            }
        }

        public List<UnitInfo> Units { get; set; } = new List<UnitInfo>();
        public Vector2 CoorPosition { get; set; }
        private PackedScene _stackMask;
        public Area2D StackMask { get; set; } = null;
        private Main _main;



        public void AddUnit(UnitInfo unit)
        {
            Units.Add(unit);

            if (Units.Count > 1 && StackMask == null)
            {
                OnStackIncreasedTo2();
            }

            OnStackChanged();

        }
        public void RemoveUnit(UnitInfo unit)
        {
            Units.Remove(unit);

            if (Units.Count == 1)
            {
                OnStackDecreasedTo1();
            }

            OnStackChanged();
        }
        public int GetCount()
        {
            return Units.Count;
        }

        public int UnitIndexOf(UnitInfo unit)
        {
            return Units.IndexOf(unit);
        }

        public void HighLight(List<Counter> counters, UnitInfo hoveringunit)
        {
            foreach (var unit in Units)
            {

                Counter counter = counters.Find(c => c.UnitInfo == unit);

                if (unit == hoveringunit)
                {

                    counter.Modulate = new Color(1, 1, 1, 1); // 设置为不透明
                    continue;
                }

                counter.Modulate = new Color(1, 1, 1, 0.1f); // 设置为半透明
            }
        }

        protected virtual void OnStackIncreasedTo2()
        {
            StackIncreasedTo2?.Invoke(); // ?.是null条件运算符，表示如果StackUpdated不为null，则调用它
        }

        public event Action StackIncreasedTo2;

        protected virtual void OnStackDecreasedTo1()
        {
            StackDecreasedTo1?.Invoke();
        }

        public event Action StackDecreasedTo1;

        protected virtual void OnStackChanged()
        {
            EmitSignal(SignalName.StackChanged);
        }

        [Signal] public delegate void StackChangedEventHandler();

    }
}