using Godot;
using Godot.Collections;
using HexGrid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class Map : TileMapLayer
{
    //  四个数组，服务于最小路径算法
    private Array<Vector2I> _hexOffsetCoors;    // 地格的偏移坐标数组（用数组是因为知道地格总数……但好像下面的也都知道，还是用列表泛型更方便，性能应该也差不到哪里去）
    private List<AxialCoordinates> _hexAxialCoors = new List<AxialCoordinates>();   // 地格的轴向坐标列表
    private TileData[] _hexDates;
    private List<(int to, float weight)>[] _hexGraph;
    private List<Vector2I> _canMoveCoors = new List<Vector2I>();

    
    private Counter _counter;
    private MapInteraction _mapInteraction;
    private MapInteraction2 _mapInteraction2;

    public Vector2I PreciousCell { get; set; } = new Vector2I(-999, -999);
    public Vector2I PreciousClickedCell { get; set; } = new Vector2I(-999, -999);
    public List<Vector2I> CellOffsetCoors { get; set; }

    private bool _isAnyCellHighlight = false;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _mapInteraction = GetNode<MapInteraction>("MapInteraction");
        _mapInteraction2 = GetNode<MapInteraction2>("MapInteraction2");
        _counter = GetNode<Counter>("/root/Game/Counter");




        _hexOffsetCoors = GetUsedCells();

        int length = _hexOffsetCoors.Count;

        _hexDates = new TileData[length];

        _hexGraph = new List<(int to, float weight)>[length];

        for (int i = 0; i < length; i++)
        {
            var coor = _hexOffsetCoors[i];
            TileData hex = GetCellTileData(coor);
            _hexDates[i] = hex;

            var axialCoor = AxialCoordinates.OffsetToAxial(coor);
            _hexAxialCoors.Add(axialCoor);

            _hexGraph[i] = new List<(int to, float weight)>();

        }

        for (int i = 0; i < length; i++)
        {
            AxialCoordinates axialCoor = _hexAxialCoors[i];
            AxialCoordinates n1 = new AxialCoordinates(axialCoor.Q + 1, axialCoor.R);
            AxialCoordinates n2 = new AxialCoordinates(axialCoor.Q + 1, axialCoor.R - 1);
            AxialCoordinates n3 = new AxialCoordinates(axialCoor.Q, axialCoor.R - 1);
            AxialCoordinates n4 = new AxialCoordinates(axialCoor.Q - 1, axialCoor.R);
            AxialCoordinates n5 = new AxialCoordinates(axialCoor.Q - 1, axialCoor.R + 1);
            AxialCoordinates n6 = new AxialCoordinates(axialCoor.Q, axialCoor.R + 1);

            AddGraph(n1, i);
            AddGraph(n2, i);
            AddGraph(n3, i);
            AddGraph(n4, i);
            AddGraph(n5, i);
            AddGraph(n6, i);


        }

        //GD.Print($"偏移坐标：{_hexOffsetCoors[39]}，轴向坐标：{_hexAxialCoors[39]}，移动力成本：{_hexDates[39].GetCustomDataByLayerId(0)}");


        _counter.SelectUnit += GetHexMpList;


    }

    public void AddGraph(AxialCoordinates n, int i)
    {

        int indexN = _hexAxialCoors.IndexOf(n);

        // 判断索引是否越界
        if (indexN >= 0)
        {
            float weightN = (float)_hexDates[indexN].GetCustomDataByLayerId(0);
            _hexGraph[i].Add((indexN, weightN));
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //  获取当前鼠标在网格中的坐标
        Vector2 localMousePosition = GetLocalMousePosition();
        Vector2I currentCellMousePosition = LocalToMap(localMousePosition);
        AxialCoordinates currentCellAxialPosition = AxialCoordinates.OffsetToAxial(currentCellMousePosition);
        //GD.Print($"当前鼠标所在的偏移坐标：{currentCellMousePosition}");
        //GD.Print($"当前鼠标所在的轴向坐标：{currentCellAxialPosition}");

        //  获取当前鼠标点击的按钮，返回值是一个 long 类型的整数，表示鼠标按钮的掩码，1表示左键，2表示右键，4表示中键，8表示第四个按钮，16表示第五个按钮，以此类推


        //GD.Print($"当前鼠标所在的坐标：{currentCellMousePosition}，当前鼠标点击的按钮掩码：{clickedMouseButton}");

        //  获取当前鼠标所在的坐标上的瓦片 ID，如果返回 -1，说明鼠标所在的坐标上没有瓦片
        int cellId = GetCellSourceId(currentCellMousePosition);
        if (cellId != -1)
        {

            //  判断鼠标是否点击了左键，如果是则调用 ClickCell 方法
            if (MouseManager.Instance.ClickedMouseButton == 1 && MouseManager.Instance.MouseLeftHoldingTime <= 0.01 && !_counter.IsHovering)
            {
                var temPreciousClickedCell = PreciousClickedCell;   // 因为引用参数不能传入属性（属性返回的是变量的值而不是内存地址），所以要用一个临时变量做中转
                _mapInteraction.ClickCell(currentCellMousePosition, ref temPreciousClickedCell);
                PreciousClickedCell = temPreciousClickedCell;
            }

            if (MouseManager.Instance.ClickedMouseButton == 2 && MouseManager.Instance.MouseLeftHoldingTime <= 0.01)
            {
                OnSelectCoor(currentCellMousePosition);
            }

            if (_counter.IsHovering)
            {

                if (_isAnyCellHighlight)
                {
                    _mapInteraction.QuitPreciousCell(currentCellMousePosition, PreciousClickedCell);
                    _isAnyCellHighlight = false;
                }

            }
            else
            {
                _mapInteraction.EnterNewCell(currentCellMousePosition, PreciousClickedCell, _isAnyCellHighlight);
            }

            if (currentCellMousePosition != PreciousCell)
            {

                _mapInteraction.EnterNewCell(currentCellMousePosition, PreciousClickedCell, _isAnyCellHighlight);


                //  判断鼠标是否从没有瓦片的坐标进入，如果是则不用调用 QuitPreciousCell 方法，因为根本没有先前瓦片
                if (PreciousCell != new Vector2I(-999, -999))
                {
                    _mapInteraction.QuitPreciousCell(PreciousCell, PreciousClickedCell);
                }
                PreciousCell = currentCellMousePosition;

            }


        }
        else
        {
            if (PreciousCell != new Vector2I(-999, -999))
            {
                _mapInteraction.QuitPreciousCell(PreciousCell, PreciousClickedCell);
                PreciousCell = new Vector2I(-999, -999);
            }
        }

        
        


    }

    private void GetHexMpList(Vector2I CoorOnHex, int MP)
    {
        List<Vector2I> tempCoors = new List<Vector2I>(); // 用一个临时序列储存可移动至的地格坐标，因为每次调用这个方法时都要重置_canMoveCoors
        int start = _hexOffsetCoors.IndexOf(CoorOnHex); // Dijkstra算法需要的开始节点的索引

        //  元组的解构
        var (weights, _) = Dijkstra.FindShortestPaths(_hexGraph, start);


        


        for (int i = 0; i < weights.Length; i++)
        {
            if (MP >= weights[i])
            {
                var coor = _hexOffsetCoors[i];
                GD.Print($"可以进入的地格：{coor}，移动力：{MP}，移动力消耗：{weights[i]}");

                tempCoor.Add(coor);
                
                _mapInteraction2.GreenHighlight(coor);
            }
        }

        
        _canMoveCoors = tempCoor;


    }



    protected virtual void OnSelectCoor(Vector2I cell)
    {
        if (_canMoveCoors.Contains(cell))
        {
            _mapInteraction2.RemoveGreenHighlight(_canMoveCoors);
            EmitSignal(SignalName.SelectCoor, cell);
        }
        
    }


    [Signal] public delegate void SelectCoorEventHandler(Vector2I coor);

}

public class Dijkstra
{





    public static (float[] weights, int[] prev) FindShortestPaths(List<(int to, float weight)>[] graph, int start)
    {
        int n = graph.Length;
        float[] weights = new float[n];
        int[] prev = new int[n];

        System.Array.Fill(weights, int.MaxValue);
        System.Array.Fill(prev, -1);

        weights[start] = 0;

        var pq = new PriorityQueue<int, float>();
        pq.Enqueue(start, 0);

        while (pq.Count > 0)
        {
            pq.TryDequeue(out int g, out float p);

            if (p != weights[g])
            {
                continue;
            }

            foreach (var (t, w) in graph[g])
            {
                float newWeight = w + p;
                if (newWeight < weights[t])
                {
                    weights[t] = newWeight;
                    prev[t] = g;
                    pq.Enqueue(t, newWeight);
                }
            }

        }

        return (weights, prev);

    }
}
