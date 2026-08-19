using Godot;
using Godot.Collections;
using HexGrid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class Map : TileMapLayer
{
    //  服务于最小路径算法的5个字段序列
    private Array<Vector2I> _hexOffsetCoors;    // 地格的偏移坐标数组（用数组是因为返回地格序列的方法的返回值是Godot内置的泛型数组，还是用列表泛型更方便，性能应该也差不到哪里去）
    private List<AxialCoordinates> _hexAxialCoors = new List<AxialCoordinates>();   // 地格的轴向坐标列表
    private TileData[] _hexDates;   // 每个地格的信息对象

    /* 用于Dijkstra算法的表示游戏地图的图结构，是一个元素为列表的数组。
     * 每一个列表表示一个地格，列表的元素是元组，代表着与当前地格邻接的地格。
     * 元组的两个元素是邻接地格的索引和权重（地格的进入成本）
     * 被每个列表代表的地格将用其索引在不同的序列中以不同的形式获取，因此不需要在这个图结构中存储地格的额外信息 */
    private List<(int to, float weight)>[] _hexGraph;
    private List<Vector2I> _canMoveCoors = new List<Vector2I>();    // 算子可以移动至的地格


    // 对节点的引用
    private Counter _counter;
    private MapInteraction _mapInteraction;
    private MapInteraction2 _mapInteraction2;

    // 用于实现鼠标进入地格时的高亮互动的属性和字段，现在看来不太优雅
    // TODO：尝试改进这个逻辑
    public Vector2I PreciousCell { get; set; } = new Vector2I(-999, -999);
    public Vector2I PreciousClickedCell { get; set; } = new Vector2I(-999, -999);
    private bool _isAnyCellHighlight = false;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // 获取对节点的引用
        _mapInteraction = GetNode<MapInteraction>("MapInteraction");
        _mapInteraction2 = GetNode<MapInteraction2>("MapInteraction2");
        _counter = GetNode<Counter>("/root/Main/Counter");

        
        _hexOffsetCoors = GetUsedCells();   // 获取地格序列
        int length = _hexOffsetCoors.Count;

        // 这两个因为是数组所以需要获取到地格序列的长度后才能声明固定长度的数组
        _hexDates = new TileData[length];
        _hexGraph = new List<(int to, float weight)>[length];

        // 这个循坏将地格的数据处理后插入序列（注意数组不允许插入元素，而是修改某个索引的值，但为了方便期间还是叫插入）
        for (int i = 0; i < length; i++)
        {
            var coor = _hexOffsetCoors[i];

            // 插入地格的信息对象
            TileData hex = GetCellTileData(coor);
            _hexDates[i] = hex;

            // 插入地格的轴向坐标
            var axialCoor = AxialCoordinates.OffsetToAxial(coor);
            _hexAxialCoors.Add(axialCoor);

            // 为图结构先生成空的代表地格的列表
            _hexGraph[i] = new List<(int to, float weight)>();

        }

        // 这个循环为图结构中的列表插入相应的元组
        for (int i = 0; i < length; i++)
        {
            // 一系列用轴向坐标求得的邻接点的地格坐标
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

        _counter.SelectUnit += GetHexMpList;    // 绑定算子的选中事件


    }

    /// <summary>
    /// 这个方法用来为代表地图的图结构的元素列表插入邻接点信息元组
    /// </summary>
    /// <param name="n">邻接点的元组</param>
    /// <param name="i">索引</param>
    public void AddGraph(AxialCoordinates n, int i)
    {
        int indexN = _hexAxialCoors.IndexOf(n); // 因为是求出的轴向坐标，所以先通过轴向坐标的地格序列查到对应的索引（没查到返回-1）

        // 判断索引是否越界
        if (indexN >= 0)
        {
            /* 获取地格的进入成本（作为图结构中邻接点之间的权重）
             单个地格的信息对象有一个返回指定自定义属性的值的方法（根据IP的比较好用）*/
            float weightN = (float)_hexDates[indexN].GetCustomDataByLayerId(0);

            _hexGraph[i].Add((indexN, weightN));    // 插入
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //  获取当前鼠标在网格中的坐标
        Vector2 localMousePosition = GetLocalMousePosition();
        Vector2I currentCellMousePosition = LocalToMap(localMousePosition);
        AxialCoordinates currentCellAxialPosition = AxialCoordinates.OffsetToAxial(currentCellMousePosition);
        GD.Print($"当前鼠标所在的偏移坐标：{currentCellMousePosition}");
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

    /// <summary>
    /// 这个方法用于在算子被选中时计算可移动范围并显示绿色高亮
    /// </summary>
    /// <param name="CoorOnHex">算子在被选择时所在的地格坐标（偏移）</param>
    /// <param name="MP">算子的移动力</param>
    private void GetHexMpList(Vector2I CoorOnHex, int MP)
    {
        List<Vector2I> tempCoors = new List<Vector2I>(); // 用一个临时序列储存可移动至的地格坐标，因为每次调用这个方法时都要重置_canMoveCoors
        int start = _hexOffsetCoors.IndexOf(CoorOnHex); // Dijkstra算法需要的开始节点的索引

        /* 调用Dijkstra算法返回一个元组，元组的第一个元素就是所有地格距离当前地格的最小距离（移动力成本）
        此处语法是元组的解构 */
        var (weights, _) = Dijkstra.FindShortestPaths(_hexGraph, start);


        // 遍历这个最小距离序列，将移动力成本小于等于算子移动力的地格坐标插入_canMoveCoors
        for (int i = 0; i < weights.Length; i++)
        {
            if (MP >= weights[i])
            {
                var coor = _hexOffsetCoors[i];
                GD.Print($"可以进入的地格：{coor}，移动力：{MP}，移动力消耗：{weights[i]}");

                tempCoors.Add(coor);    // 先插入临时序列
                
            }
        }

        _mapInteraction2.GreenHighlight(tempCoors);  // 调用第二个地图交互节点的绿色高亮方法
        _canMoveCoors = tempCoors;  // 更新可移动地格序列


    }


    /// <summary>
    /// 当传入的地格坐标在_canMoveCoors里就触发SelectCoor事件，同时移除显示移动范围的高光（因为此时算子绑定该事件的方法已将算子移动）
    /// </summary>
    /// <param name="cell"></param>
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

/// <summary>
/// 实现Dijkstra算法的工具类
/// </summary>
public class Dijkstra
{




    /// <summary>
    /// 算法的核心实现方法，接收一个表示地图的图结构和起始顶点的索引
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="start"></param>
    /// <returns>返回一个元组，两个元素分别是地格的最小距离序列和到达每一个地格所要经过的前驱地格（用于算出最小路径具体会经过的地格）</returns>
    public static (float[] weights, int[] prev) FindShortestPaths(List<(int to, float weight)>[] graph, int start)
    {
        // 初始化要返回的两个序列
        int n = graph.Length;
        float[] weights = new float[n];
        int[] prev = new int[n];
        // 这里用最大的浮点数和永远越界的索引来初始化，表示目前还没有任何顶点被处理
        System.Array.Fill(weights, float.MaxValue);
        System.Array.Fill(prev, -1);

        weights[start] = 0; // 距离初始顶点的权重当然为0


        // 使用C#内置的优先队列（默认就是最小堆，即优先级最小的优先出列）
        var pq = new PriorityQueue<int, float>();
        pq.Enqueue(start, 0);   // 先将初始顶点入队（后面自然也是先从这个顶点开始处理）

        // 算法的主循环，当优先队列还有元素时进入循环（每完成一次循环就会有新的邻接点入队，当队列空了说明所有顶点都已被处理）
        while (pq.Count > 0)
        {
            pq.TryDequeue(out int v, out float p);  // 方法返回布尔值，用输出参数来获取出队的元素（用这个方法是因为要使用顶点的权重）

            /* 优先队列里相同的顶点不会只有一次被插入，因为路径不同，到达顶点的总权重也不同，只要有更短的路径被发现都会重新进行入队，同时更新在最短路径序列中的对应权重。
             * 明确的一点是weights[v]永远是当前最短的路径，而p只是入队的时候这个被保存的有可能过时了的路径。
               而最短路径在这个顶点出队后就确定下来了（这个是算法的数学原理，不用深究，只需要按照其思路用代码实现即可），
               也就是说这些过时的路径都不是第一次出队的（即在之前找到的比较长的路径），所以这个判断实际的作用就是不要重复处理已被确定最短路径的顶点*/
            if (p != weights[v])
            {
                continue;
            }

            // 遍历当前所在顶点的所有邻接点，t是邻接点的索引，w是其权重
            foreach (var (t, w) in graph[v])
            {
                float newWeight = w + p;    // 更新从初始顶点到这个顶点的这个路径的总权重，p所代表的含义就是已确定的确定的初始顶点到上一个顶点的最短路径（这个顶点是上一个顶点的邻接点）
                
                // 如果这个新路径的总权重比原来存着的要小，则更新，再将这个顶点的新总权重入队优先队列。这个顶点将会和其他的在这一轮入队的顶点中比较权重谁更小（然后作为新一轮的起点）
                if (newWeight < weights[t])
                {
                    weights[t] = newWeight;
                    prev[t] = v;
                    pq.Enqueue(t, newWeight);
                }
            }

        }

        return (weights, prev);

    }
}
