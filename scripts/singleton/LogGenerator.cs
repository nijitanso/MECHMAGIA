using Godot;
using Data;
using System.Collections.Generic;

namespace Generator
{
    /// <summary>
    /// 提供一系列日志文本的格式化方法
    /// </summary>
    public static partial class LogGenerator
    {
        public enum LogType
        {
            Move,
            Attack,
            PhaseChange,
        }


        public static ActionLog ActionLog { get; set; }

        // 下面两个属性用于在生成日志时分布式获取攻击方和防御方的单位信息
        public static List<UnitInfo> Attackers { get; set; }
        public static List<UnitInfo> Defenders { get; set; }
        public static TeamEnum AttackTeam { get; set; }

        public static void GetMoveLog(UnitInfo unit, Vector2I from, Vector2I to, TeamEnum team)
        {
            string teamName = (team == TeamEnum.Enemy) ? "[color=#FF4500A6][i]敌方" : "[color=#00FF00A6][i]我方";

            string log = $"[机动] {teamName} 单位 {unit.ID}[/i][/color] 从 {from} 机动至 {to}。";

            ActionLog.AddLog(log, LogType.Move);


        }

        public static void GetAttackLog(string battleResult)
        {
            string teamName1 = (AttackTeam == TeamEnum.Enemy) ? "[color=#00FF00A6][i]我方" : "[color=#FF4500A6][i]敌方";
            string teamName2 = (AttackTeam == TeamEnum.Enemy) ? "[color=#FF4500A6][i]敌方" : "[color=#00FF00A6][i]我方";

            battleResult = battleResult.Substring(3);   // 去掉前两个字符，避免重复显示进攻或防御的标识

            string aids = "";
            string dids = "";

            // 将参与战斗的单位的ID都列出来
            foreach (var unit in Attackers)
            {
                if (aids != "")
                {
                    aids += $" {unit.ID}";
                }
                else
                {
                    aids += unit.ID.ToString();
                }
            }

            foreach (var unit in Defenders)
            {
                if (dids != "")
                {
                    dids += $" {unit.ID}";
                }
                else
                {
                    dids += unit.ID.ToString();
                }
            }

            string log = $"[进攻] {teamName1} 单位 {aids}[/i][/color] 对 {teamName2} 单位 {dids}[/i][/color] 发动进攻，{battleResult}";

            ActionLog.AddLog(log, LogType.Attack);


        }

        public static void GetPhaseChangeLog(int nowTurn, string nowPhase)
        {
            string log = $"[阶段] 进行到第 {nowTurn} 回合 {nowPhase} 阶段";

            ActionLog.AddLog(log, LogType.PhaseChange);
        }

    }
}

