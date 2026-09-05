using Data;
using Godot;
using HexGrid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using APC = ActionProcessor.AttackProcessor;
using MM = Managers.MouseManager;
public partial class Map : TileMapLayer
{
    public async void ShowTestMessage()
    {
        while (true)
        {
            GD.Print(UnitStacks.Count);
            await Task.Delay(500);

            /*
            string a = "";
            foreach (var unit in SelectedUnits)
            {
                a += unit.ID.ToString();
            }
            if (a != "")
            {
                GD.Print(a);
            }

            */
        }

    }


    //  服务于最小路径算法的字段序列
    private Godot.Collections.Array<Vector2I> _hexOffsetCoors;    // 地格的偏移坐标数组（用数组是因为返回地格序列的方法的返回值是Godot内置的泛型数组，还是用列表泛型更方便，性能应该也差不到哪里去）
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
    public bool IsGrgphExisted { get; set; } = false;
    public bool IsUnitStacksExisted { get; set; } = false;
    private List<Vector2I> _canMoveCoors = new List<Vector2I>();    // 算子可以移动至的地格
    private int[] _prev;    // 用来建立最短路径的具体路径的前驱节点序列

    public Dictionary<Vector2I, UnitStack> UnitStacks { get; set; } = new Dictionary<Vector2I, UnitStack>();


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

            // 为图结构生成空的代表地格的列表
            _hexGraph[i] = new List<(int to, float weight)>();
            GraphForRPath[i] = new List<(int to, float weight)>();

        }

        //GD.Print($"偏移坐标：{_hexOffsetCoors[39]}，轴向坐标：{HexAxialCoors[39]}，移动力成本：{_hexDates[39].GetCustomDataByLayerId(0)}");

        ClickCoor += MM.Inst.SelectSwitchToMap;

        // 响应UnitsUpdate事件，更新算子在地图上的状态
        _main.UnitsUpdate += UnitInitAndUPdate;
        _main.UnitsUpdate += AddGraph;


        //ShowTestMessage();

    }

    /// <summary>
    /// 当算子第一次Update时为图结构添加元素
    /// </summary>
    /// <param name="_"></param>
    public void AddGraph(Godot.Collections.Array<Counter> _)
    {
        if (IsGrgphExisted) return;

        // 这个循环为图结构中的列表插入相应的元组
        for (int i = 0; i < _hexOffsetCoors.Count; i++)
        {
            // 一系列用轴向坐标求得的邻接点的地格坐标
            List<AxialCoor> neighbors = HexAxialCoors[i].GetNeighborCoor(HexAxialCoors);

            foreach (var neighbor in neighbors)
            {
                AddCoorInGraph(neighbor, i);
            }
        }

        IsGrgphExisted = true;
    }

    /// <summary>
    /// 这个方法用来为代表地图的图结构的元素列表插入邻接点信息元组
    /// </summary>
    /// <param name="n">邻接点的元组</param>
    /// <param name="i">索引</param>
    public void AddCoorInGraph(AxialCoor n, int i)
    {
        int indexN = HexAxialCoors.IndexOf(n); // 因为是求出的轴向坐标，所以先通过轴向坐标的地格序列查到对应的索引（没查到返回-1）
        float weightN;

        // 判断索引是否越界
        if (indexN >= 0)
        {
            /* 获取地格的进入成本（作为图结构中邻接点之间的权重）
			 单个地格的信息对象有一个返回指定自定义属性的值的方法（根据IP的比较好用）*/

            weightN = (float)_hexDates[indexN].GetCustomDataByLayerId(0);

            _hexGraph[i].Add((indexN, weightN));    // 插入
            GraphForRPath[i].Add((indexN, 1));    // 插入
        }
    }

    /// <summary>
    /// 事件处理器，响应Main发出的UnitsUpdate，初始化和更新算子状态
    /// </summary>
    /// <param name="units"></param>
    public void UnitInitAndUPdate(Godot.Collections.Array<Counter> units)
    {
        GetCoorWithUnit(units);
        GetZoc(CoorWithUnit);
        GetUnitStacks(units);
    }


    /// <summary>
    /// 计算有算子在上面的地格序列，每次调用时清空原来的地格序列，并重新计算新的地格序列
    /// </summary>
    /// <param name="units"></param>
    public void GetCoorWithUnit(Godot.Collections.Array<Counter> units)
    {
        CoorWithUnit.Clear();  // 方法开始前先清空旧序列
        foreach (var unit in units) // 遍历Main节点传入的事件参数（算子的实例序列），分离出一个元组存入地格序列（地格坐标和算子的阵营）
        {
            CoorWithUnit.Add((unit.UnitInfo.CoorOfAxial, unit.UnitInfo.Team));
        }
    }

    public void GetUnitStacks(Godot.Collections.Array<Counter> units)
    {
        //GD.Print(units.Count);
        if (!IsUnitStacksExisted)
        {
            foreach (var unit in units)
            {
                UnitStacks[unit.UnitInfo.Coor] = new UnitStack(unit.UnitInfo, unit.Position, _main);
                //GD.Print(1);
            }

            IsUnitStacksExisted = true;
            OnStackReady(UnitStacks);
        }

    }

    /// <summary>
    /// 将CoorWithUnit序列中敌我算子分开，考虑到有可能复用就用方法封装了
    /// </summary>
    /// <param name="friendCoors"></param>
    /// <param name="enemyCoors"></param>
    /// <param name="coors"></param>
    public void SeperateTeam(List<AxialCoor> friendCoors, List<AxialCoor> enemyCoors, List<(AxialCoor coor, TeamEnum team)> coors)
    {
        // 元组美丽时刻，好灵活啊
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
        SeperateTeam(friendCoors, enemyCoors, coors);

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

        _coorWithZoc = _coorWithFZoc.Union(_coorWithEZoc).ToList(); // 将两个序列合并为一个总ZOC序列
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        UpdateMouseTracking();
    }

    public override void _Input(InputEvent @event)
    {
        Vector2I mouseCoorPos = LocalToMap(GetLocalMousePosition());    // 先获取当前鼠标所在位置

        bool isHoveringCounter = MM.Inst.HoverState == MM.HoverStateEnum.Counter;
        bool isHoveringMap = MM.Inst.HoverState == MM.HoverStateEnum.Map;

        // 用于发起攻击时的双方阵营算子序列
        List<UnitInfo> friends = MM.Inst.SelectedUnits;
        List<UnitInfo> enemies = new List<UnitInfo>();
        if (MM.Inst.IsStackHovered)
        {
            enemies = MM.Inst.HoveringStack.Units;
        }
        


        if (@event is InputEventMouseButton mouseEvent)
        {
            // 判断左键时是否悬停在地图上
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && isHoveringMap)
            {
                ClickCell(mouseCoorPos);
            }

            // 判断右键
            if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {
                if (MM.Inst.SelectedUnits.Count == 0)
                {
                    _mapInteraction.DisclickCell(PreciousClickedCell);
                    PreciousClickedCell = new Vector2I(-999, -999);
                    OnRightBotton();
                }
                else if (!_canMoveCoors.Contains(mouseCoorPos) && !_attackIcon.Visible)
                {
                    OnRightBotton();
                }

                
                // 检测攻击图标是否可见和是否满足攻击条件
                if (_attackIcon.Visible && APC.Inst.AttackCheck(friends, enemies))
                {
                    APC.Inst.StartAttack();
                }
                // 不能进攻就移动算子
                else
                {
                    OnSelectCoor(mouseCoorPos);
                }

                if (isHoveringCounter)
                {
                    EnterAStack(mouseCoorPos);
                }
            }



        }
    }

    public void ProcessUnitRetreat(Vector2I newCoor, Vector2I oldCoor, Counter counter)
    {
        AxialCoor axialCoor = AxialCoor.OffsetToAxial(newCoor);
        UnitStack stack;
        UnitInfo unit = counter.UnitInfo;

        TeamEnum friendTeam = (unit.Team == TeamEnum.Enemy) ? TeamEnum.Enemy : TeamEnum.Friend;

        if (!CoorWithUnit.Contains((axialCoor, friendTeam)))
        {
            stack =  FormAStackForRetreat(newCoor, oldCoor, unit);
            GD.Print("形成堆叠");
        }
        else
        {
            stack = EnterAStackForRetreat(newCoor, oldCoor, unit);
            GD.Print("进入堆叠");
        }

        counter.FormStack(stack);
    }

    /// <summary>
    /// 将当前选中的算子加入到鼠标悬停的地格的算子堆叠中，并触发OnEnterStack事件通知Counter
    /// </summary>
    /// <param name="coor"></param>
    public void EnterAStack(Vector2I coor)
    {
        if (!_canMoveCoors.Contains(coor)) return;
        if (MM.Inst.SelectedUnits.Count == 0 || MM.Inst.SelectedUnits.Count > 1) return;

        UnitStack stack = UnitStacks[coor];
        UnitInfo unit = MM.Inst.SelectedUnits[0];

        QuitAStack(unit);   // 先从原来的堆叠中移除
        stack.AddUnit(unit);


        OnEnterStack(coor, stack);
    }

    public UnitStack EnterAStackForRetreat(Vector2I newCoor, Vector2I oldCoor, UnitInfo unit)
    {

        UnitStack stack = UnitStacks[newCoor];

        QuitAStackForRetreat(unit, oldCoor);   // 先从原来的堆叠中移除
        stack.AddUnit(unit);

        return stack;
    }

    /// <summary>
    /// 在进入新地格时创建一个新的算子堆叠，并将当前选中的算子加入到该堆叠中
    /// </summary>
    /// <param name="coor"></param>
    public UnitStack FormAStack(Vector2I coor)
    {

        UnitInfo unit = MM.Inst.SelectedUnits[0];

        QuitAStack(unit);   // 先从原来的堆叠中移除

        UnitStacks[coor] = new UnitStack(unit, MapToLocal(coor), _main);

        return UnitStacks[coor];

    }

    public UnitStack FormAStackForRetreat(Vector2I newCoor, Vector2I oldCoor, UnitInfo unit)
    {
        QuitAStackForRetreat(unit, oldCoor);   // 先从原来的堆叠中移除

        UnitStacks[newCoor] = new UnitStack(unit, MapToLocal(newCoor), _main);

        return UnitStacks[newCoor];

    }

    /// <summary>
    /// 从当前选中的算子所在的地格的算子堆叠中移除该算子，并当此堆叠为空时从字典中移除该堆叠的键值对
    /// </summary>
    /// <param name="unit"></param>
    public void QuitAStack(UnitInfo unit)
    {
        UnitStack stack = UnitStacks[unit.Coor];
        stack.RemoveUnit(unit);

        // 如果堆叠为空则从字典中移除该堆叠的键值对
        if (stack.GetCount() == 0)
        {
            UnitStacks.Remove(unit.Coor);
        }
    }

    public void QuitAStackForRetreat(UnitInfo unit, Vector2I coor)
    {
        UnitStack stack = UnitStacks[coor];
        stack.RemoveUnit(unit);

        // 如果堆叠为空则从字典中移除该堆叠的键值对
        if (stack.GetCount() == 0)
        {
            UnitStacks.Remove(coor);
        }
    }


    /// <summary>
    /// 将攻击图标节点移动到对应位置并显示
    /// </summary>
    /// <param name="coor"></param>
    public void DisplayAttackIcon(Vector2I coor)
    {
        _attackIcon.Position = MapToLocal(coor);
        _attackIcon.Visible = true;
    }

    /// <summary>
    /// 隐藏攻击图标节点
    /// </summary>
    public void HideAttackIcon()
    {
        _attackIcon.Visible = false;
    }

    /// <summary>
    /// 取消地格的白色边框，即悬停状态
    /// </summary>
    /// <param name="coor"></param>
    public void QuitCell(Vector2I coor)
    {
        _mapInteraction.QuitPreciousCell(coor, PreciousClickedCell);
        _isAnyCellHighlight = false;
    }

    /// <summary>
    /// 显示地格的白色边框，即悬停状态
    /// </summary>
    /// <param name="coor"></param>
    public void EnterCell(Vector2I coor)
    {
        _mapInteraction.EnterNewCell(coor, PreciousClickedCell);
        _isAnyCellHighlight = true;
        OnQuitStack();
    }

    /// <summary>
    /// 点击选中地格，显示黄色边框，触发ClickCoor方法
    /// </summary>
    /// <param name="coor"></param>
    public void ClickCell(Vector2I coor)
    {
        _mapInteraction.ClickCell(coor, PreciousClickedCell);

        if (coor != PreciousClickedCell)    // 若重复点击已选中的地格则不会取消黄色边框
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

    /// <summary>
    /// 显示或隐藏攻击图标，变相判断能否发起进攻
    /// </summary>
    /// <param name="coor"></param>
    public void DetectAttackIcon(Vector2I coor)
    {

        // 将长串的枚举状态判断结果先用意义明确的变量表示，让下面的if语句可读性更高，逻辑更清晰
        bool isHovering = MM.Inst.HoverState == MM.HoverStateEnum.Counter;
        bool isSelectUnit = MM.Inst.SelectState == MM.SelectStateEnum.Counter;
        bool isNeighbor = false;
        bool isEnemy = false;

        // 判断有没有选中单位，避免索引越界（空序列必定越界）
        if (MM.Inst.SelectedUnits.Count != 0)
        {
            isEnemy = MM.Inst.HoveringUnit.Team != MM.Inst.SelectedUnits[0].Team;
        }

        List<UnitInfo> tSelectedUnits = MM.Inst.SelectedUnits;  // 将需要遍历的序列临时储存，避免频繁调用getter访问属性
        AxialCoor hoveringUnit = MM.Inst.HoveringUnit.CoorOfAxial;
        AxialCoor axialCoor;

        foreach (var unit in tSelectedUnits)
        {
            axialCoor = unit.CoorOfAxial;

            // 判断悬停的单位是否处于所有选中单位共有的相邻地格，只要有一个选中单位的相邻地格未处于就将isNeighbor设为false并跳出循环
            if (axialCoor.GetNeighborCoor(HexAxialCoors).Contains(hoveringUnit))
            {
                isNeighbor = true;
            }
            else
            {
                isNeighbor = false;
                break;
            }

        }

        // 如果根本就没选中单位就直接隐藏攻击图标并返回
        if (!isSelectUnit)
        {
            HideAttackIcon();
            return;
        }

        if (isEnemy && !_attackIcon.Visible && isNeighbor)
        {
            DisplayAttackIcon(coor);
        }

        if ((!isEnemy || !isHovering) && _attackIcon.Visible)
        {
            HideAttackIcon();
        }
        //GD.Print($"悬停单位：{MM.Inst.HoveringUnit.Team}");
        //GD.Print($"选中单位：{MM.Inst.SelectedUnits.Team}");
    }


    // TODO：目前这一个方法里面处理太多鼠标检测逻辑了，要找时间将他们拆分成子方法
    /// <summary>
    /// 在_Process中每帧检测一次鼠标状态，以实现鼠标悬停在地格和算子上时的边框高亮
    /// </summary>
    public void UpdateMouseTracking()
    {
        Vector2I mouseCoorPos = LocalToMap(GetLocalMousePosition());    // 鼠标当前在地图上的地格坐标
        bool IsHoveringCounter = MM.Inst.HoverState == MM.HoverStateEnum.Counter;   // 鼠标当前是否悬停在算子上

        //GD.Print($"当前鼠标所在的偏移坐标：{mouseCoorPos}");

        //  获取当前鼠标所在的坐标上的瓦片 ID，如果返回 -1，说明鼠标所在的坐标上没有瓦片
        int cellId = GetCellSourceId(mouseCoorPos);
        if (cellId != -1)
        {
            DetectAttackIcon(mouseCoorPos);

            //GD.Print(MM.Inst.HoverState);
            // 判断从MouseManager类中的状态枚举来区分鼠标是悬停在地格上还是算子上，这段if语句是独立的判断算子悬停高亮的逻辑
            if (IsHoveringCounter)
            {

                // 若此前有被高亮的地格则取消该地格的高亮
                if (_isAnyCellHighlight)
                {
                    QuitCell(mouseCoorPos);
                }
            }
            else if (!_isAnyCellHighlight && !MM.Inst.IsStackHovered)
            {
                EnterCell(mouseCoorPos);
            }

            //GD.Print(MM.Inst.IsStackHovered);
            // 悬停高亮的判断是检测当前的鼠标所在地格坐标是否与上一次被记录的地格坐标相同
            if (mouseCoorPos != PreciousCell && !IsHoveringCounter && !MM.Inst.IsStackHovered)
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

    /// <summary>
    /// 建立从当前选中算子所在地格到传入的地格的有效最短路径序列
    /// </summary>
    /// <param name="coor"></param>
    /// <returns></returns>
    public List<Vector2I> BulidValidPath(Vector2I coor)
    {
        List<Vector2I> path;
        UnitInfo unit = MM.Inst.SelectedUnits[0];   // 只有在单选算子的情况下才会执行到这（不是的话在上面的if就返回了），单选序列只有0索引的一个单位

        List<AxialCoor> zocs = GetCorrZocs(unit.Team);

        if (!zocs.Contains(unit.CoorOfAxial))
        {
            int index = _hexOffsetCoors.IndexOf(coor);
            path = Dijkstra.GetPath(_prev, index, _hexOffsetCoors, _hexOffsetCoors.IndexOf(unit.Coor)); // 获取单位移动的最短路径
        }
        else
        {
            // 如果在控制区内移动的话则路径就只是一格，不需要寻路了
            path = new List<Vector2I>() { coor };
        }

        return path;
    }


    /// <summary>
    /// 根据参数传入的阵营枚举返回相应的敌方ZOC
    /// </summary>
    /// <param name="team"></param>
    /// <returns></returns>
    public List<AxialCoor> GetCorrZocs(TeamEnum team)
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
        TeamEnum enemyTeam = (unitInfo.Team == TeamEnum.Friend) ? TeamEnum.Enemy : TeamEnum.Friend;   // 获取敌方阵营的枚举值（条件操作符）

        List<AxialCoor> zocs = GetCorrZocs(unitInfo.Team);
        /* 调用Dijkstra算法返回一个元组，元组的第一个元素就是所有地格距离当前地格的最小距离（移动力成本）
		此处语法是元组的解构 */

        // 判断单位所处的地格是否在敌方ZOC上
        if (!zocs.Contains(unitInfo.CoorOfAxial))
        {
            var (weights, prev) = Dijkstra.FindShortestPaths(_hexGraph, start, this, zocs, enemyTeam);
            _prev = prev;   // 每次调用最短路径算法方法时都更新一下前驱节点序列

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
        // 若在则将移动范围强制变为相邻地格（无视地形）
        else
        {
            List<AxialCoor> tempCoorsWithAxial = unitInfo.CoorOfAxial.GetNeighborCoor(HexAxialCoors);
            foreach (var coor in tempCoorsWithAxial)
            {
                // 不能移动到有敌方单位的地格
                if (CoorWithUnit.Contains((coor, enemyTeam)))
                {
                    continue;
                }

                // 判断地格是否越界（虽然GetNeighborCoor方法已经确保了返回的相邻地格合法）
                if (HexAxialCoors.Contains(coor))
                {
                    tempCoors.Add(coor.AxialToOffset());
                }
            }
        }

        tempCoors.Remove(unitInfo.Coor);    // 移除单位所在的地格（不能原地移动）

        // 判断是否单选单位，否则不显示移动范围和无法移动
        if (MM.Inst.SelectedUnits.Count == 1)
        {
            _mapInteraction2.GreenHighlight(tempCoors);  // 调用第二个地图交互节点的绿色高亮方法
            _canMoveCoors = tempCoors;  // 更新可移动地格序列
        }



    }


    /// <summary>
    /// 移除可移动地格上的绿色高光
    /// </summary>
    public void RemoveGreen()
    {
        _mapInteraction2.RemoveGreenHighlight(_canMoveCoors);
    }

    /// <summary>
    /// 事件处理器，响应Counter的SelectUnit事件。根据单位阵营显示对应的敌方ZOC
    /// </summary>
    /// <param name="unit"></param>
    public void ShowZoc(UnitInfo unit)
    {
        List<AxialCoor> zoc = GetCorrZocs(unit.Team);
        _mapInteraction3.ShowZoc(zoc);
    }

    /// <summary>
    /// 事件处理器，响应Counter的DeselectUnit事件。取消显示地图上所有被显示的ZOC
    /// </summary>
    public void RemoveZoc()
    {
        _mapInteraction3.RemoveZoc(HexAxialCoors);
    }


    [Signal] public delegate void SelectCoorEventHandler(Godot.Collections.Array<Vector2I> coors, UnitStack stack);

    /// <summary>
    /// 当传入的地格坐标在_canMoveCoors里就触发SelectCoor事件，同时移除显示移动范围的高光（因为此时算子绑定该事件的方法已将算子移动）
    /// </summary>
    /// <param name="cell"></param>
    protected virtual void OnSelectCoor(Vector2I coor)
    {
        AxialCoor axialCoor = AxialCoor.OffsetToAxial(coor);

        if (!_canMoveCoors.Contains(coor)) return;
        if (CoorWithUnit.Contains((axialCoor, TeamEnum.Friend)) || CoorWithUnit.Contains((axialCoor, TeamEnum.Enemy))) return;
        if (MM.Inst.SelectedUnits.Count == 0 || MM.Inst.SelectedUnits.Count > 1) return;

        UnitStack stack = FormAStack(coor);

        List<Vector2I> path = BulidValidPath(coor);

        EmitSignal(SignalName.SelectCoor, new Godot.Collections.Array<Vector2I>(path), stack);

    }



    [Signal] public delegate void ClickCoorEventHandler(Vector2I coor);

    protected virtual void OnClickCoor(Vector2I coor)
    {
        EmitSignal(SignalName.ClickCoor, coor);
    }


    protected virtual void OnRightBotton()
    {
        EmitSignal(SignalName.RightBotton);
    }

    [Signal] public delegate void RightBottonEventHandler();

    protected virtual void OnEnterStack(Vector2I coor, UnitStack stack)
    {

        if (MM.Inst.SelectedUnits.Count == 0 || MM.Inst.SelectedUnits.Count > 1) return;
        GD.Print("调用");

        List<Vector2I> path = BulidValidPath(coor);

        EmitSignal(SignalName.EnterStack, coor, stack, new Godot.Collections.Array<Vector2I>(path));
    }

    [Signal] public delegate void EnterStackEventHandler(Vector2I coor, UnitStack stack, Godot.Collections.Array<Vector2I> path);


    protected virtual void OnStackReady(Dictionary<Vector2I, UnitStack> dict)
    {
        EmitSignal(SignalName.StackReady, new Godot.Collections.Dictionary<Vector2I, UnitStack>(dict));
    }

    [Signal] public delegate void StackReadyEventHandler(Godot.Collections.Dictionary<Vector2I, UnitStack> dict);

    protected virtual void OnQuitStack()
    {
        EmitSignal(SignalName.QuitStack);
    }

    [Signal] public delegate void QuitStackEventHandler();
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
    public static (float[] weights, int[] prev) FindShortestPaths(List<(int to, float weight)>[] graph, int start, Map map, List<AxialCoor> zocs, TeamEnum enemyTeam)
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

            // 如果优先队列出队的是敌方控制区，那么将跳过，不会处理其邻接点（单位在进入控制区后无法继续移动）
            if (zocs.Contains(map.HexAxialCoors[v]))
            {
                continue;
            }

            // 遍历当前所在顶点的所有邻接点，t是邻接点的索引，w是其权重
            foreach (var (t, w) in graph[v])
            {
                AxialCoor coor = map.HexAxialCoors[t];
                // 如果这个邻接点有敌方单位的话，跳过（即永远不会入队，不可到达点）
                if (map.CoorWithUnit.Contains((coor, enemyTeam)))
                {
                    continue;
                }

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

    /// <summary>
    /// 这个重载仅用来计算两个点在权重均为1的图中的距离
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="start"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 根据前驱节点序列建立单位沿着最短路径移动时的顺序
    /// </summary>
    /// <param name="prev">前驱节点序列，其结构是索引代表一个地格，而索引的位置储存着到达这个地格所经过的前一个地格的索引</param>
    /// <param name="targetIndex">要到达的目标节点的索引</param>
    /// <param name="coors">用来通过索引获得地格坐标实例的序列</param>
    /// <param name="start">起点的索引</param>
    /// <returns></returns>
    public static List<Vector2I> GetPath(int[] prev, int targetIndex, Godot.Collections.Array<Vector2I> coors, int start)
    {
        // 先建立序列，并将目的地节点先插入序列
        List<Vector2I> path = new List<Vector2I>();
        path.Add(coors[targetIndex]);

        int index = targetIndex;

        // 判断路径是否已经回到了起点
        while (prev[index] != start)
        {
            index = prev[index];
            path.Add(coors[index]);

        }

        path.Reverse(); // 将序列反转，因为是从终点开始插入的

        return path;
    }

    /// <summary>
    /// 自动计算单位撤退路径，路径共三格，算法是计算三次邻接点，在六个邻接点中选择“离最近的敌人最远的”
    /// </summary>
    /// <param name="start"></param>
    /// <param name="map"></param>
    /// <param name="team"></param>
    /// <returns></returns>
    public static Vector2I[] GetRetreatPath(AxialCoor start, Map map, TeamEnum team)
    {
        Vector2I[] rPath = new Vector2I[3]; // 撤退路径
        AxialCoor nowCoor = start;
        List<AxialCoor> enemyCoors = new List<AxialCoor>();
        List<AxialCoor> zocs = map.GetCorrZocs(team);


        // 先得到所有敌方单位所在的地格
        foreach (var (coor, enemyTeam) in map.CoorWithUnit)
        {
            if (team != enemyTeam) enemyCoors.Add(coor);
        }

        // 进行三次循环，先从起点开始计算邻接点，在邻接点里选择后以下一个选择的点为起点继续计算邻接点
        for (int i = 0; i < 3; i++)
        {
            List<AxialCoor> neighbors = nowCoor.GetNeighborCoor(map.HexAxialCoors);
            neighbors.Shuffle();    // 打乱邻接点列表的一个扩展方法，为了让撤退路径多样化
            float bestMinDist = -1.0f;

            foreach (var neighbor in neighbors)
            {
                float minDist = float.MaxValue;
                foreach (var enemy in enemyCoors)
                {
                    float dist = GetTwoCoorDist(map.HexAxialCoors.IndexOf(neighbor), map.HexAxialCoors.IndexOf(enemy), map.GraphForRPath);

                    if (dist < minDist) minDist = dist; // 这里确定距离遍历到的邻接点最近的敌方单位（直接确定这个最短距离就行）

                }

                // 每次有邻接点离上面确定好的minDist更远的就将这个邻接点加入撤退路径（有更远的会更新，因为是通过索引赋值的而不是Add方法）
                // 如果这个邻接点是敌方控制区或敌方单位所在地格就不加入撤退路径
                if (minDist > bestMinDist && !zocs.Contains(neighbor) && !enemyCoors.Contains(neighbor))
                {
                    rPath[i] = neighbor.AxialToOffset();
                    //GD.Print(neighbor.AxialToOffset());
                    // 更新状态
                    nowCoor = neighbor;
                    bestMinDist = minDist;
                }
            }
        }

        return rPath;
    }

    /// <summary>
    /// 获得两个顶点之间的最短路径
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="graph"></param>
    /// <returns></returns>
    public static float GetTwoCoorDist(int start, int end, List<(int to, float weight)>[] graph)
    {
        var (weights, _) = FindShortestPaths(graph, start);

        return weights[end];
    }

}
