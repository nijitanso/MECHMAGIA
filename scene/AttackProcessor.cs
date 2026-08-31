using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using Godot;

namespace ActionProcessor
{
    /// <summary>
    /// 处理玩家触发的进攻事件，记录信息
    /// </summary>
    public partial class AttackProcessor : Node
    {

        public static AttackProcessor Inst { get; private set; }   // 单例属性，其他类通过这个单例来使用MouseManager

        // 为了确保 Instance 在场景树中唯一，使用 _EnterTree 方法来设置 Instance（单例）
        public override void _EnterTree()
        {
            Inst = this;
        }


        /// <summary>
        /// CombatResult(战斗结果)的枚举
        /// </summary>
        public enum CREnum
        {
            Null = 0,
            DR = 1,
            DR2 = 2,
            DR3 = 3,
            DE = 4,
            AR = 5,
            AR1 = 6,
            AR2 = 7,
            AE = 8
        }

        /// <summary>
        /// 战斗结果表，数组第一层是骰子点数（索引对应骰子+1），第二层是点数比（索引直接对应比值，1/2则向下取整为0）
        /// </summary>
        public CREnum[][] CRT = new CREnum[][]
        {
            new CREnum[] { CREnum.AE, CREnum.AR1, CREnum.AR, CREnum.Null, CREnum.DR, CREnum.DR, CREnum.DR2},
            new CREnum[] { CREnum.AR2, CREnum.AR, CREnum.Null, CREnum.DR, CREnum.DR, CREnum.DR2, CREnum.DR3},
            new CREnum[] { CREnum.AR1, CREnum.Null, CREnum.DR, CREnum.DR, CREnum.DR2, CREnum.DR3, CREnum.DE},
            new CREnum[] { CREnum.AR, CREnum.DR, CREnum.DR, CREnum.DR2, CREnum.DR3, CREnum.DE, CREnum.DE},
            new CREnum[] { CREnum.Null, CREnum.DR, CREnum.DR2, CREnum.DR3, CREnum.DE, CREnum.DE, CREnum.DE},
            new CREnum[] { CREnum.DR, CREnum.DR2, CREnum.DR3, CREnum.DE, CREnum.DE, CREnum.DE, CREnum.DE}
        };

        public int Ratio { get; set; }
        public int Dice { get; set; }
        public CREnum CR { get; set; }
        public List<UnitInfo> Attackers { get; set; }
        public List<UnitInfo> Defenders { get; set; }


        /// <summary>
        /// 获取一个从1-6的骰子点数
        /// </summary>
        /// <returns></returns>
        public int GetDice()
        {
            int dice = Random.Shared.Next(1, 6);
            return dice;
        }

        /// <summary>
        /// 对攻击能否发生做一次判断，同时将点数比存入属性Ratio
        /// </summary>
        /// <param name="friends"></param>
        /// <param name="enemies"></param>
        public bool AttackCheck(List<UnitInfo> friends, List<UnitInfo> enemies)
        {

            /* 将参与战斗的双方单位储存起来，在处理CR时可以使用（不需要依靠选中和悬浮关系，免去了凌乱的顺序），赋值方式是浅拷贝（ToList方法），
             即新建一个装有原来对象引用（序列里的元素是UnitInfo实例）的新序列，这样修改旧序列就不会影响这里的了，
             毕竟所有的引用都被复制了一遍而不是对原来那个序列的引用*/
            Attackers = friends.ToList();
            Defenders = enemies.ToList();


            // 双方的总点数，用float是因为需要小数除法
            float totalAP = 0;
            float totalDP = 0;

            foreach (var unit in friends)
            {
                totalAP += unit.AP;
            }

            foreach (var unit in enemies)
            {
                totalDP += unit.DP;
            }

            float ratio = totalAP / totalDP;

            if (ratio < 0.5) return false;  // 攻击/防御小于1/2则无法发起进攻

            Ratio = (int)System.Math.Floor(ratio);

            return true;
        }

        public void StartAttack()
        {
            Dice = GetDice();
            try
            {
                CR = CRT[Dice - 1][Ratio];
            }
            catch (IndexOutOfRangeException)
            {
                GD.PrintErr("CRT索引越界");
            }

            OnAttack();
        }

        public void OnAttack()
        {
            EmitSignal(SignalName.Attack);
        }

        [Signal] public delegate void AttackEventHandler(); // 此处不传递任何事件参数，所有所需的参数会通过访问AttackProcessor单例直接获得（因为信号能传递的参数限制太多了）
    }
}