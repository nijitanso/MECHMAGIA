using Godot;
using HexGrid;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Data
{
    /// <summary>
    /// 友军，敌军，中立部队的枚举
    /// </summary>
    public enum TeamEnum
    {
        Friend = 0,
        Enemy = 1,
        Neutral = 2
    }



    /// <summary>
    /// 从JSON读取单位数据的数据类，注意这个类只用来读取JSON，创建的实例将会把数据传给真正被使用的静态类
    /// </summary>
    public class UnitDataJson
    {
        public List<UnitInfo> UnitSetup { get; set; }


        public static void Initialize()
        {
            try
            {
                string json = FileAccess.GetFileAsString("res://data/UnitSetup.json");
                UnitDataJson unitDataJson = JsonSerializer.Deserialize<UnitDataJson>(json);
                GD.Print("序列化UnitSetup.json成功");

                UnitData.UnitSetup = unitDataJson.UnitSetup;    // 将数据传给静态类


            }
            catch (Exception)
            {
                GD.PrintErr("序列化UnitSetup.json失败！");
            }
        }
    }

    /// <summary>
    /// 存储了单位数据的静态类
    /// </summary>
    public static class UnitData
    {
        public static List<UnitInfo> UnitSetup { get; set; }
    }

    /// <summary>
    /// 描述了一个算子的信息，继承Godot中的标准数据类基类Resource以方便和引擎的其他API交互（如信号的参数传输）
    /// </summary>
    public partial class UnitInfo : RefCounted
    {
        public int ID { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public TeamEnum Team { get; set; }
        public int AP { get; set; }
        public int DP { get; set; }
        public int MP { get; set; }
        // 这个JsonIgnore特性表示它将在序列化时被JSON忽略，这个属性是用来拼装一些Godot特有的类型时所使用的
        [JsonIgnore]
        public Vector2I Coor
        {
            get { return new Vector2I(PosX, PosY); }
            set
            {
                PosX = value.X;
                PosY = value.Y;
            }
        }
        [JsonIgnore]
        public AxialCoor CoorOfAxial
        {
            get
            {
                return AxialCoor.OffsetToAxial(Coor);
            }
        }


    }
}