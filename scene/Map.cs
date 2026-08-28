using Data;
using Godot;
using Godot.Collections;
using HexGrid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime;
using APC = ActionProcessor.AttackProcessor;
using MM = MouseManager;
public partial class Map : TileMapLayer
{
    //  服务于最小路径算法的字段序列
    private Array<Vector2I> _hexOffsetCoors;    // 地格的偏移坐标数组（用数组是因为返回地格序列的方法的返回值是Godot内置的泛型数组，还是用列表泛型更方便，性能应该也差不到哪里去）
    public List<AxialCoor> HexAxialCoors { get; set; } = new List<AxialCoor>();   // 地格的轴向坐标列表
    private TileData[] _hexDates;   // 每个地格的信息对象
    private List<AxialCoor> _coorWithFZoc = new List<AxialCoor>(); // 有友方控制区的地格序列
    private List<AxialCoor> _coorWithEZoc = new List<AxialCoor>(); // 有敌方控制区的地格序列
    private List<AxialCoor> _coorWithZoc = new List<AxialCoor>(); // 有控制区的地格序列（全部的控制区，无论敌我）
    public List<(AxialCoor coor, TeamEnum team)> CoorWithUnit { get; set; } = new List<(AxialCoor coor, TeamEnum team)>();   // 有算子位于其上的地格，元素是一个元组，地格坐标和其上的单位的阵营

    /* 用于Dijkstra算法的表示游戏地图的图结构，是一个元素为列表的数组。
	 * 每一个列表表示一个地格，列表的元素是元组，代表着与当前地格邻接的地格。
	 * 元组的两个元素是邻接地格的索引和权重（地格的进入成本）
	 * 被每个列表代表的地格将用其索引在不同的序列中以不同的形式获取，因此不需要在这个图结构中存储地格的额外信息 */
    private List<(int to, float weight)>[] _hexGraph;
    public List<(int to, float weight)>[] GraphForRPath { get; set; }  // 实际上是一个权重均为1的图，用来算两个地格之间的纯距离
    private List<Vector2I> _canMoveCoors = new List<Vector2I>();    // 算子可以移动至的地格
    private int[] _prev;


    // 对节点的引用
    private MapInteraction _mapInteraction;
    private MapInteraction2 _mapInteraction2;
    private MapInteraction3 _mapInteraction3;
    private Sprite2D _attackIcon;
    private Main _main;

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
        _mapInteraction3 = GetNode<MapInteraction3>("MapInteraction3");
        _attackIcon = GetNode<Sprite2D>("AttackIcon");
        _main = GetParent<Main>();


        _hexOffsetCoors = GetUsedCells();   // 获取地格序列
        int length = _hexOffsetCoors.Count;

        // 这两个因为是数组所以需要获取到地格序列的长度后才能声明固定长度的数组
        _hexDates = new TileData[length];
        _hexGraph = new List<(int to, float weight)>[length];
        GraphForRPath = new List<(int to, float weight)>[length];

        // 这个循坏将地格的数据处理后插入序列（注意数组不允许插入元素，而是修改某个索引的值，但为了方便期间还是叫插入）
        for (int i = 0; i < length; i++)
        {
            var coor = _hexOffsetCoors[i];

            // 插入地格的信息对象
            TileData hex = GetCellTileData(coor);
            _hexDates[i] = hex;

            // 插入地格的轴向坐标
            var axialCoor = AxialCoor.OffsetToAxial(coor);
            HexAxialCoors.Add(axialCoor);

            // 为图结构先生成空的代表地格的列表
            _hexGraph[i] = new List<(int to, float weight)>();
            GraphForRPath[i] = new List<(int to, float weight)>();

        }

        // 这个循环为图结构中的列表插入相应的元组
        for (int i = 0; i < length; i++)
        {
            // TODO：可以用方法重构了
            // 一系列用轴向坐标求得的邻接点的地格坐标
            List<AxialCoor> neighbors = HexAxialCoors[i].GetNeighborCoor(HexAxialCoors);

            foreach (var neighbor in neighbors)
            {
                AddGraph(neighbor, i);
            }

        }

        //GD.Print($"偏移坐标：{_hexOffsetCoors[39]}，轴向坐标：{HexAxialCoors[39]}，移动力成本：{_hexDates[39].GetCustomDataByLayerId(0)}");

        ClickCoor += MM.Inst.SelectSwitchToMap;

        _main.UnitsUpdate += UnitInitAndUPdate;

    }



    /// <summary>
    /// 这个方法用来为代表地图的图结构的元素列表插入邻接点信息元组
    /// </summary>
    /// <param name="n">邻接点的元组</param>
    /// <param name="i">索引</param>
    public void AddGraph(AxialCoor n, int i)
    {
        int indexN = HexAxialCoors.IndexOf(n); // 因为是求出的轴向坐标，所以先通过轴向坐标的地格序列查到对应的索引（没查到返回-1）

        // 判断索引是否越界
        if (indexN >= 0)
        {
            /* 获取地格的进入成本（作为图结构中邻接点之间的权重）
			 单个地格的信息对象有一个返回指定自定义属性的值的方法（根据IP的比较好用）*/
            float weightN = (float)_hexDates[indexN].GetCustomDataByLayerId(0);

            _hexGraph[i].Add((indexN, weightN));    // 插入
            GraphForRPath[i].Add((indexN, 1));    // 插入
        }
    }

    /// <summary>
    /// 事件处理器，响应Main发出的UnitsUpdate，初始化和更新算子状态
    /// </summary>
    /// <param name="units"></param>
    public void UnitInitAndUPdate(Array<Counter> units)
    {
        GetCoorWithUnit(units);
        GetZoc(CoorWithUnit);
    }


    /// <summary>
    /// 计算有算子在上面的地格序列，每次调用时清空原来的地格序列，并重新计算新的地格序列
    /// </summary>
    /// <param name="units"></param>
    public void GetCoorWithUnit(Array<Counter> units)
    {
        CoorWithUnit.Clear();  // 方法开始前先清空旧序列

        foreach (var unit in units) // 遍历Main节点传入的事件参数（算子的实例序列），分离出一个元组存入地格序列（地格坐标和算子的阵营）
        {
            CoorWithUnit.Add((unit.UnitInfo.CoorOfAxial, unit.UnitInfo.Team));
        }
    }

    public void SeperateTeam(List<AxialCoor> friendCoors, List<AxialCoor> enemyCoors, List<(AxialCoor coor, TeamEnum team)> coors)
    {
        foreach (var (coor, team) in coors)
        {
            switch (team)
            {
                case TeamEnum.Friend:
                    friendCoors.Add(coor);
                    break;
                case TeamEnum.Enemy:
                    enemyCoors.Add(coor);
                    break;
                case TeamEnum.Neutral:
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// 根据一个有单位的地格坐标序列获取友方ZOC序列和敌方ZOC序列
    /// </summary>
    /// <param name="coors"></param>
    public void GetZoc(List<(AxialCoor coor, TeamEnum team)> coors)
    {
        // 每次被调用时都要先清空旧序列以便更新
        _coorWithFZoc.Clear();
        _coorWithEZoc.Clear();

        // 将不同阵营的地格区分，用两个序列储存
        List<AxialCoor> friendCoors = new List<AxialCoor>();
        List<AxialCoor> enemyCoors = new List<AxialCoor>();

        // 根据阵营将地格分成两个列表
        SeperateTeam(friendCoors,enemyCoors,coors);

        // 这里两段类似的遍历有点啰嗦，但是又不想写方法复用了，更麻烦╰（‵□′）╯
        foreach (var coor in friendCoors)
        {
            List<AxialCoor> neighborCoors = coor.GetNeighborCoor(HexAxialCoors);   // 先获取地格的邻接地格（ZOC一般就是单位的一环内）

            foreach (var neighbor in neighborCoors)
            {
                //  做一次判断以防存入相同的地格坐标
                if (!_coorWithFZoc.Contains(neighbor))
                {
                    _coorWithFZoc.Add(neighbor);
                }
            }
        }

        foreach (var coor in enemyCoors)
        {
            List<AxialCoor> neighborCoors = coor.GetNeighborCoor(HexAxialCoors);

            foreach (var neighbor in neighborCoors)
            {
                if (!_coorWithEZoc.Contains(neighbor))
                {
                    _coorWithEZoc.Add(neighbor);
                }
            }
        }

        _coorWithZoc = _coorWithFZoc.Union(_coorWithEZoc).ToList();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        UpdateMouseTracking();
    }

    public override void _Input(InputEvent @event)
    {
        Vector2I mouseCoorPos = LocalToMap(GetLocalMousePosition());

        List<UnitInfo> friends = new List<UnitInfo>();
        List<UnitInfo> enemies = new List<UnitInfo>();

        friends.Add(MM.Inst.SelectedUnit);
        enemies.Add(MM.Inst.HoveringUnit);

        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && MM.Inst.HoverState == MM.HoverStateEnum.Map)
            {
                ClickCell(mouseCoorPos);
            }

            if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {


                if (_attackIcon.Visible && APC.Inst.AttackCheck(friends, enemies))
                {
                    APC.Inst.StartAttack();
                }
                else
                {
                    OnSelectCoor(mouseCoorPos);
                }
            }



        }
    }


    public void DisplayAttackIcon(Vector2I coor)
    {
        _attackIcon.Position = MapToLocal(coor);
        _attackIcon.Visible = true;
    }

    public void HideAttackIcon()
    {
        _attackIcon.Visible = false;
    }

    public void QuitCell(Vector2I coor)
    {
        _mapInteraction.QuitPreciousCell(coor, PreciousClickedCell);
        _isAnyCellHighlight = false;
    }

    public void EnterCell(Vector2I coor)
    {
        _mapInteraction.EnterNewCell(coor, PreciousClickedCell);
        _isAnyCellHighlight = true;
    }

    public void ClickCell(Vector2I coor)
    {
        _mapInteraction.ClickCell(coor, PreciousClickedCell);

        if (coor != PreciousClickedCell)    // 若重复点击已选中的地格则不会取消黄色高光
        {
            _mapInteraction.DisclickCell(PreciousClickedCell);
        }

        PreciousClickedCell = coor;
        OnClickCoor(coor);
    }

    /// <summary>
    /// 这个事件一般来说是作为事件处理器使用，当有算子被选中时取消对当前地格的选中
    /// </summary>
    /// <param name="coor"></param>
    public void DisclickCellForUnit(UnitInfo unitInfo)
    {
        _mapInteraction.DisclickCell(PreciousClickedCell);
        PreciousClickedCell = new Vector2I(-999, -999);
    }

    public void DetectAttackIcon(Vector2I coor)
    {
        // 将长串的枚举状态判断结果先用意义明确的变量表示，让下面的if语句可读性更高，逻辑更清晰
        bool isEnemy = MM.Inst.HoveringUnit.Team != MM.Inst.SelectedUnit.Team;
        bool isHovering = MM.Inst.HoverState == MM.HoverStateEnum.Counter;

        AxialCoor axialCoor = MM.Inst.SelectedUnit.CoorOfAxial;
        List<AxialCoor> neighbors = axialCoor.GetNeighborCoor(HexAxialCoors);

        bool isNeighbor = neighbors.Contains(MM.Inst.HoveringUnit.CoorOfAxial);

        if (MM.Inst.SelectState != MM.SelectStateEnum.Counter) return;

        if (isEnemy && !_attackIcon.Visible && isNeighbor)
        {
            //GD.Print("显示");
            DisplayAttackIcon(coor);
        }

        if ((!isEnemy || !isHovering) && _attackIcon.Visible)
        {

            HideAttackIcon();
        }
        //GD.Print($"悬停单位：{MM.Inst.HoveringUnit.Team}");
        //GD.Print($"选中单位：{MM.Inst.SelectedUnit.Team}");
    }

    public void Test(Vector2I[] path)
    {
        List<Vector2I> list = new List<Vector2I>(path);
        _mapInteraction2.GreenHighlight(list);
    }

    // TODO：目前这一个方法里面处理太多鼠标检测逻辑了，要找时间将他们拆分成子方法
    /// <summary>
    /// 在_Process中每帧检测一次鼠标状态，以实现鼠标悬停在地格和算子上时的边框高亮
    /// </summary>
    public void UpdateMouseTracking()
    {
        Vector2I mouseCoorPos = LocalToMap(GetLocalMousePosition());    // 鼠标当前在地图上的地格坐标

        //GD.Print($"当前鼠标所在的偏移坐标：{mouseCoorPos}");

        //  获取当前鼠标所在的坐标上的瓦片 ID，如果返回 -1，说明鼠标所在的坐标上没有瓦片
        int cellId = GetCellSourceId(mouseCoorPos);
        if (cellId != -1)
        {
            DetectAttackIcon(mouseCoorPos);


            // 判断从MouseManager类中的状态枚举来区分鼠标是悬停在地格上还是算子上，这段if语句是独立的判断算子悬停高亮的逻辑
            if (MM.Inst.HoverState == MM.HoverStateEnum.Counter)
            {

                // 若此前有被高亮的地格则取消该地格的高亮
                if (_isAnyCellHighlight)
                {
                    QuitCell(mouseCoorPos);
                }
            }
            else if (!_isAnyCellHighlight)
            {
                EnterCell(mouseCoorPos);
            }

            // 悬停高亮的判断是检测当前的鼠标所在地格坐标是否与上一次被记录的地格坐标相同
            if (mouseCoorPos != PreciousCell)
            {
                EnterCell(mouseCoorPos);

                // 判断鼠标是否从地图边缘进入，若是则不用取消上一个高亮的地格（因为根本没有）（说实话这段逻辑有点蠢，不过能用就用也不想改了）
                if (PreciousCell != new Vector2I(-999, -999))
                {
                    QuitCell(PreciousCell);
                }

                PreciousCell = mouseCoorPos;    // 更新被记录的地格
            }
        }
        // 如果此时鼠标不在地图上而在虚空，则取消上一次高亮和更新被记录的地格
        else
        {
            // 这个判断存在的意义是为了不要反复调用取消高光的方法，否则会造成性能浪费和不必要的异常
            if (PreciousCell != new Vector2I(-999, -999))
            {
                QuitCell(PreciousCell);
            }

            PreciousCell = new Vector2I(-999, -999);
        }
    }

    private List<AxialCoor> GetCorrZocs(TeamEnum team)
    {
        List<AxialCoor> zocs;

        switch (team)
        {
            case TeamEnum.Friend:
                zocs = _coorWithEZoc;
                break;
            case TeamEnum.Enemy:
                zocs = _coorWithFZoc;
                break;
            case TeamEnum.Neutral:
                zocs = _coorWithEZoc;
                break;
            default:
                zocs = _coorWithEZoc;
                break;
        }

        return zocs;
    }

    /// <summary>
    /// 这个方法用于在算子被选中时计算可移动范围并显示绿色高亮
    /// </summary>
    /// <param name="CoorOnHex">算子在被选择时所在的地格坐标（偏移）</param>
    /// <param name="MP">算子的移动力</param>
    /// <param name="_">对不使用的事件参数用_弃元</param>
    public void GetHexMpList(UnitInfo unitInfo)
    {
        List<Vector2I> tempCoors = new List<Vector2I>(); // 用一个临时序列储存可移动至的地格坐标，因为每次调用这个方法时都要重置_canMoveCoors
        int start = _hexOffsetCoors.IndexOf(unitInfo.Coor); // Dijkstra算法需要的开始节点的索引

        List<AxialCoor> zocs = GetCorrZocs(unitInfo.Team);
        /* 调用Dijkstra算法返回一个元组，元组的第一个元素就是所有地格距离当前地格的最小距离（移动力成本）
		此处语法是元组的解构 */


        if (!zocs.Contains(unitInfo.CoorOfAxial))
        {
            var (weights, prev) = Dijkstra.FindShortestPaths(_hexGraph, start, HexAxialCoors, zocs);
            _prev = prev;

            // 遍历这个最小距离序列，将移动力成本小于等于算子移动力的地格坐标插入_canMoveCoors
            for (int i = 0; i < weights.Length; i++)
            {
                if (unitInfo.MP >= weights[i])
                {
                    var coor = _hexOffsetCoors[i];
                    //GD.Print($"可以进入的地格：{coor}，移动力：{unitInfo.MP}，移动力消耗：{weights[i]}");

                    tempCoors.Add(coor);    // 先插入临时序列

                }
            }
        }
        else
        {
            List<AxialCoor> tempCoorsWithAxial = unitInfo.CoorOfAxial.GetNeighborCoor(HexAxialCoors);
            foreach (var coor in tempCoorsWithAxial)
            {
                if (HexAxialCoors.Contains(coor))
                {
                    tempCoors.Add(coor.AxialToOffset());
                }
            }
        }



        _mapInteraction2.GreenHighlight(tempCoors);  // 调用第二个地图交互节点的绿色高亮方法
        _canMoveCoors = tempCoors;  // 更新可移动地格序列


    }



    public void RemoveGreen()
    {
        _mapInteraction2.RemoveGreenHighlight(_canMoveCoors);
    }

    public void ShowZoc(UnitInfo unit)
    {
        switch (unit.Team)
        {
            case TeamEnum.Friend:
                _mapInteraction3.ShowZoc(_coorWithEZoc);
                break;
            case TeamEnum.Enemy:
                _mapInteraction3.ShowZoc(_coorWithFZoc);
                break;
            case TeamEnum.Neutral:
                break;
            default:
                break;
        }
    }

    public void RemoveZoc()
    {
        _mapInteraction3.RemoveZoc(HexAxialCoors);
    }


    [Signal] public delegate void SelectCoorEventHandler(Array<Vector2I> coors);

    /// <summary>
    /// 当传入的地格坐标在_canMoveCoors里就触发SelectCoor事件，同时移除显示移动范围的高光（因为此时算子绑定该事件的方法已将算子移动）
    /// </summary>
    /// <param name="cell"></param>
    protected virtual void OnSelectCoor(Vector2I coor)
    {

        if (!_canMoveCoors.Contains(coor)) return;

        List<Vector2I> path;
        UnitInfo unit = MM.Inst.SelectedUnit;

        List<AxialCoor> zocs = GetCorrZocs(unit.Team);

        if (!zocs.Contains(unit.CoorOfAxial))
        {
            int index = _hexOffsetCoors.IndexOf(coor);
            path = Dijkstra.GetPath(_prev, index, _hexOffsetCoors, _hexOffsetCoors.IndexOf(MM.Inst.SelectedUnit.Coor));
        }
        else
        {
            path = new List<Vector2I>() { coor };
        }

        EmitSignal(SignalName.SelectCoor, new Array<Vector2I>(path));

    }


    [Signal] public delegate void ClickCoorEventHandler(Vector2I coor);

    protected virtual void OnClickCoor(Vector2I coor)
    {
        EmitSignal(SignalName.ClickCoor, coor);
    }


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
    public static (float[] weights, int[] prev) FindShortestPaths(List<(int to, float weight)>[] graph, int start, List<AxialCoor> axials, List<AxialCoor> zocs)
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

            if (zocs.Contains(axials[v]))
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

    public static List<Vector2I> GetPath(int[] prev, int targetIndex, Array<Vector2I> coors, int start)
    {
        List<Vector2I> path = new List<Vector2I>();
        path.Add(coors[targetIndex]);
        int index = targetIndex;

        while (prev[index] != start)
        {
            index = prev[index];
            path.Add(coors[index]);

        }

        path.Reverse();

        return path;
    }


    public static Vector2I[] GetRetreatPath(AxialCoor start, Map map, TeamEnum team)
    {
        Vector2I[] rPath = new Vector2I[3];
        AxialCoor nowCoor = start;
        List<AxialCoor> enemyCoors = new List<AxialCoor>();

        foreach (var (coor, enemyTeam) in map.CoorWithUnit)
        {
            if (team != enemyTeam) enemyCoors.Add(coor);
        }
        

        for (int i = 0; i < 3; i++)
        {
            List<AxialCoor> neighbors = nowCoor.GetNeighborCoor(map.HexAxialCoors);
            float initDist = 0.0f;

            foreach (var neighbor in neighbors)
            {
                float totalDist = 0;
                foreach (var enemy in enemyCoors)
                {
                    float dist = GetTwoCoorDist(map.HexAxialCoors.IndexOf(neighbor), map.HexAxialCoors.IndexOf(enemy), map.GraphForRPath);
                    totalDist += dist;
                }

                float averageDist = totalDist/ enemyCoors.Count;
                if (averageDist > initDist)
                {
                    rPath[i] = neighbor.AxialToOffset();
                    //GD.Print(neighbor.AxialToOffset());
                    nowCoor = neighbor;
                    initDist = averageDist;
                }  
            }

            
        }

        return rPath;
    }

    public static float GetTwoCoorDist(int start, int end, List<(int to, float weight)>[] graph)
    {
        var (weights, _) = FindShortestPaths(graph, start);

        return weights[end];
    }

}
