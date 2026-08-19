using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Data
{
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

                UnitData.UnitSetup = unitDataJson.UnitSetup;

            }
            catch (Exception)
            {

                GD.PrintErr("序列化UnitSetup.json失败！");
            }
        }
    }

    public static class UnitData
    {
        public static List<UnitInfo> UnitSetup { get; set; }
    }

    public class UnitInfo
    {
        public int ID { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        private Vector2I coor;
        [JsonIgnore] public Vector2I Coor
        {
            get { return new Vector2I(PosX, PosY); }
            set { coor = value; }
        }

    }
}