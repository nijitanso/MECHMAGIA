using Data;
using Godot;
using Managers;
using Godot.Collections;
using System;
using System.Collections.Generic;
using APC = ActionProcessor.AttackProcessor;
using TM = Managers.TurnManager;
using HexGrid;

/// <summary>
/// 地图上的算子节点，负责显示、选中、移动、堆叠与战斗结果处理
/// </summary>
public partial class Counter : Area2D
{
    //	UnitInfo类是每一个算子用来储存单位数据的数据类
    public UnitInfo UnitInfo { get; set; } = new UnitInfo();


    // 对节点的引用
    /// <summary>
    /// 攻击点数标签
    /// </summary>
    private Label _attackPointLabel;
    /// <summary>
    /// 防御点数标签
    /// </summary>
    private Label _defendPointLabel;
    /// <summary>
    /// 移动点数标签
    /// </summary>
    private Label _movePointLabel;
    /// <summary>
    /// 算子碰撞体
    /// </summary>
    public CollisionShape2D CollisionShape2D { get; set; }
    /// <summary>
    /// 上层节点引用
    /// </summary>
    private Node2D _upperLayer;
    /// <summary>
    /// 地图节点引用
    /// </summary>
    private Map _map;
    /// <summary>
    /// 主场景引用
    /// </summary>
    private Main _main;
    /// <summary>
    /// 算子本体精灵
    /// </summary>
    private Sprite2D _bodySprite2D;
    /// <summary>
    /// 可行动图标场景资源
    /// </summary>
    private PackedScene _canMoveIcon;
    /// <summary>
    /// 可行动图标实例
    /// </summary>
    public CanActionIcon CanActionIcon { get; set; }

    // 算子对自身大小信息的储存，用于绘制鼠标悬停和选中边框
    /// <summary>
    /// 算子左上角相对坐标
    /// </summary>
    private Vector2 _topLeftPosition;
    /// <summary>
    /// 算子右上角相对坐标
    /// </summary>
    private Vector2 _topRightPosition;
    /// <summary>
    /// 算子左下角相对坐标
    /// </summary>
    private Vector2 _downLeftPosition;
    /// <summary>
    /// 算子右下角相对坐标
    /// </summary>
    private Vector2 _downRightPosition;
    /// <summary>
    /// 算子碰撞体尺寸
    /// </summary>
    private Vector2 _size;
    /// <summary>
    /// 所属算子堆叠
    /// </summary>
    public UnitStack ParentStack { get; set; }
    /// <summary>
    /// 本算子在所属堆叠中的索引
    /// </summary>
    public int StackIndex { get; set; } = 0;


    // 状态属性
    public bool IsHovering { get; private set; } = false;
    public bool IsSelected { get; private set; } = false;
    public bool IsMultiSelect { get; set; } = false;

    /// <summary>
    /// 撤退路径的地格坐标序列
    /// </summary>
    public Vector2I[] RetreatPath { get; set; }




    /// <summary>
    /// 用于平滑移动的补间动画
    /// </summary>
    private Tween _tween;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // 获取引用
        _map = GetNode<Map>("/root/Main/Map");
        _attackPointLabel = GetNode<Label>("AttackPointLabel");
        _defendPointLabel = GetNode<Label>("DefendPointLabel");
        _movePointLabel = GetNode<Label>("MovePointLabel");
        CollisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        _upperLayer = GetNode<Node2D>("UpperLayer");
        _bodySprite2D = GetNode<Sprite2D>("BodySprite2D");
        _main = GetParent<Main>();
        _canMoveIcon = GD.Load<PackedScene>("res://scene/CanActionIcon.tscn");

        // 算子的状态初始化
        Init();


        // 计算算子大小
        _size = CollisionShape2D.Shape.GetRect().Size;
        _topLeftPosition = new Vector2(-_size.X / 2.0f, -_size.Y / 2.0f);
        _topRightPosition = new Vector2(_size.X / 2.0f, -_size.Y / 2.0f);
        _downRightPosition = new Vector2(_size.X / 2.0f, _size.Y / 2.0f);
        _downLeftPosition = new Vector2(-_size.X / 2.0f, _size.Y / 2.0f);

        GetRectPointOnMap();



        // 绑定事件（同一个事件绑定不同事件处理器时，绑定的顺序将影响事件处理器被调用的顺序，不能随意调换）
        // TODO：这里各种事件的订阅的范式不标准（应该在订阅者那边订阅，而不是发布者），考虑重构成本（或许可以实现一个事件总线）并且注意以后的事件订阅


        _main.UnitsUpdate += SetRetreatPath;    // 地图上的算子状态有变时更新自己的撤退路线，以免过期导致撤退有问题

        // 当鼠标进入算子时触发此事件
        //MouseEntered += MouseManager.Inst.OnEnterCounter;  // 调用自己的边框高亮方法
        MouseEntered += HoveringHighlight;  // 调用自己的边框高亮方法
        MouseEntered += MouseManager.Inst.HoverSwitchToCounter; // 调用MouseManager的方法，将悬停状态改为算子MouseEntered
        MouseEntered += SetHoveringUnit;
        MouseEntered += HighlightFromStack;

        // 当鼠标离开算子时触发此事件，具体的事件处理器逻辑相同，不再赘述
        MouseExited += NotHoveringHighlight;
        MouseExited += MouseManager.Inst.HoverSwitchToMap;
        MouseExited += SetHoveringUnitToNull;

        // 当有算子被选择时触发此事件，没有被选中的算子将会调用自身的Deselect方法（在SwitchDeselect方法内做判断并调用）
        MouseManager.Inst.SwitchCounter += DeselectForMultiSelect;
        //MouseManager.Inst.EnterCounter += OnMouseExited;

        // Map的事件
        _map.SelectCoor += MoveForMarch;    // 当地格被选中时触发此事件
        _map.SelectCoor += RestoreCollisionShape;    // 当地格被选中时触发此事件，恢复算子碰撞体的形状
        _map.EnterStack += EnterStack;
        _map.ClickCoor += Deselect;
        _map.RightBotton += Deselect;

        // 当算子被选中时触发事件，这里的绑定顺序一定不能调换，因为是先清除上一次选中时显示的绿色高亮再显示新的
        SelectUnit += _map.DisclickCellForUnit;
        SelectUnit += MouseManager.Inst.SelectSwitchToCounter;
        SelectUnit += MouseManager.Inst.SetSelectedUnits;

        if (UnitInfo.MoveLeft != 0)  // 初始化时有移动机会才会绑定这个信号
        {
            SelectUnit += _map.GetHexMpList;    // 调用计算最小路径的方法，获取算子移动范围，并显示绿色高光
        }

        SelectUnit += _map.ShowZoc;



        DeselectUnit += _map.RemoveGreen;
        DeselectUnit += _map.RemoveZoc;
        DeselectUnit += MouseManager.Inst.ClearSelectedUnits;
        MultiDeselectUnit += MouseManager.Inst.RemoveSelectedUnits;

        MultiSelectEvent += _map.RemoveGreen;    // 当此时算子是被多选选中时（即Ctrl被按下时）移除绿色高光（因为算子在多选状态不能移动）

        Retreated += _map.ProcessUnitRetreat;

        APC.Inst.Attack += Deselect;
        APC.Inst.Attack += ProcessCR;

        TM.Inst.NextPhrase += Deselect;
        TM.Inst.NextPhrase += ChangePhrase;
        TM.Inst.SwitchToMovementPhase += RefreshMovement;
        TM.Inst.SwitchToAttackPhase += RefreshAttack;




        QueueRedraw();  // 这个重绘可能时实现阴影时留下的，现在有了更好的实现方法，但还是不敢动这行





    }

    /// <summary>
    /// 根据算子坐标和尺寸，更新 UnitInfo.Rect 为地图上的矩形区域
    /// </summary>
    public void GetRectPointOnMap()
    {

        Vector2 tL = UnitInfo.Coor + _topLeftPosition;

        UnitInfo.Rect = new Rect2(tL, _size);

    }



    /// <summary>
    /// 对单位状态进行初始化
    /// </summary>
    public void Init()
    {
        // 设置位置
        Position = _map.MapToLocal(UnitInfo.Coor);



        // 根据阵营设置自己的纹理颜色
        switch (UnitInfo.Team)
        {
            case TeamEnum.Friend:
                _bodySprite2D.Texture = GD.Load<Texture2D>("res://resource/Unit-1.png");
                break;
            case TeamEnum.Enemy:
                _bodySprite2D.Texture = GD.Load<Texture2D>("res://resource/Unit-2.png");
                break;
            case TeamEnum.Neutral:
                _bodySprite2D.Texture = GD.Load<Texture2D>("res://resource/Unit-1.png");
                break;
            default:
                _bodySprite2D.Texture = GD.Load<Texture2D>("res://resource/Unit-1.png");
                break;
        }

        UiInitialize();


    }

    /// <summary>
    /// 初始化算子上的攻击、防御、移动点数显示，并初始化可行动图标
    /// </summary>
    public void UiInitialize()
    {
        // 设置算子纹理上的数值数字
        _attackPointLabel.Text = UnitInfo.AP.ToString();
        _defendPointLabel.Text = UnitInfo.DP.ToString();
        _movePointLabel.Text = UnitInfo.MP.ToString();

        CanActionUiInit();
    }

    /// <summary>
    /// 创建并初始化可行动图标，有剩余移动次数时显示
    /// </summary>
    public void CanActionUiInit()
    {
        CanActionIcon = _canMoveIcon.Instantiate<CanActionIcon>();
        AddChild(CanActionIcon);
        CanActionIcon.Scale = new Vector2(0.8f, 0.8f);
        CanActionIcon.Modulate = new Color("#52AB70");

        if (UnitInfo.MoveLeft > 0)
        {
            CanActionIcon.Visible = true;
        }
        else
        {
            CanActionIcon.Visible = false;
        }
    }

    /// <summary>
    /// 设置单位的撤退路线
    /// </summary>
    /// <param name="_"></param>
    private void SetRetreatPath(Array<Counter> _)
    {
        if (!IsInsideTree()) return;    // 当算子已不在场景树上时（一般是被歼灭后移出树）返回

        RetreatPath = Dijkstra.GetRetreatPath(UnitInfo.CoorOfAxial, _map, UnitInfo.Team);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // 根据Ctrl键的按下情况设置单位是否处于多选状态
        if (Input.IsKeyPressed(Key.Ctrl))
        {
            IsMultiSelect = true;
        }
        else
        {
            IsMultiSelect = false;
        }
    }

    /// <summary>
    /// 事件处理器，响应MouseEntered事件，设置MouseManager中的悬停单位
    /// </summary>
    public void SetHoveringUnit()
    {
        MouseManager.Inst.SetHoveringUnit(UnitInfo);
    }


    /// <summary>
    /// 事件处理器，响应MouseExited事件，设置MouseManager中的悬停单位为ID为-1的空单位
    /// </summary>
    public void SetHoveringUnitToNull()
    {
        MouseManager.Inst.SetHoveringUnitToNull();
    }


    /// <summary>
    /// 将算子的悬停状态设置为true（绘制边框的过程将由这个布尔属性来决定，在重写的_Draw方法），然后对算子进行缩放
    /// </summary>
    public void HoveringHighlight()
    {
        //GD.Print("2");
        IsHovering = true;
        //Scale = new Vector2(1.05f, 1.05f);
        QueueRedraw();
    }

    /// <summary>
    /// HoveringHighlight方法的反向操作
    /// </summary>
    public void NotHoveringHighlight()
    {
        //GD.Print("1");

        IsHovering = false;
        //Scale = new Vector2(1.0f, 1.0f);
        QueueRedraw();
    }
    /// <summary>
    /// 重写的_Draw方法，每次调用QueueRedraw时都会重新绘制游戏画面。注意更改状态不需要对画布进行清除，只需要“不绘制”即可，因为_Draw方法本来就是每帧都被调用的
    /// </summary>
    public override void _Draw()
    {
        if (IsHovering && !IsSelected)
        {
            // 在算子的边缘处绘制白色虚线，注意以这种方法绘制的内容会被算子的纹理覆盖
            DrawDashedLine(_topLeftPosition, _topRightPosition, Color.Color8(255, 255, 255), width: 4.0f);
            DrawDashedLine(_topLeftPosition, _downLeftPosition, Color.Color8(255, 255, 255), width: 4.0f);
            DrawDashedLine(_downLeftPosition, _downRightPosition, Color.Color8(255, 255, 255), width: 4.0f);
            DrawDashedLine(_downRightPosition, _topRightPosition, Color.Color8(255, 255, 255), width: 4.0f);
        }

        if (IsSelected)
        {
            // 在算子的边缘处绘制白色虚线，注意以这种方法绘制的内容会被算子的纹理覆盖
            DrawDashedLine(_topLeftPosition, _topRightPosition, Color.Color8(255, 255, 0), width: 5.0f);
            DrawDashedLine(_topLeftPosition, _downLeftPosition, Color.Color8(255, 255, 0), width: 5.0f);
            DrawDashedLine(_downLeftPosition, _downRightPosition, Color.Color8(255, 255, 0), width: 5.0f);
            DrawDashedLine(_downRightPosition, _topRightPosition, Color.Color8(255, 255, 0), width: 5.0f);
        }
    }

    /// <summary>
    /// 重写_Input方法，用来检测鼠标左键对算子的选中
    /// </summary>
    /// <param name="event"></param>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && IsHovering)
            {
                if (IsMultiSelect)
                {
                    MultiSelect();
                }
                else
                {
                    Select();
                    UpToStackTop();
                }

            }
        }
    }

    /// <summary>
    /// 将本单位选中，触发事件SelectUnit并绘制选中边框。SelectUnit事件绑定了很多事件处理器
    /// </summary>
    public void Select()
    {
        // 判断此时有无选中单位，避免下面索引越界
        if (MouseManager.Inst.SelectedUnits.Count != 0)
        {
            // 如果在多选状态时试图选择不同阵营的算子则返回
            if (MouseManager.Inst.SelectedUnits[0].Team != UnitInfo.Team && IsMultiSelect) return;
        }


        OnSelectUnit();

        if (IsMultiSelect)
        {
            OnMultiSelectEvent();
        }

        IsSelected = true;
        QueueRedraw();
    }

    /// <summary>
    /// 多选时切换本算子的选中状态
    /// </summary>
    public void MultiSelect()
    {
        if (IsSelected)
        {
            Deselect();
        }
        else
        {
            Select();
        }
    }

    /// <summary>
    /// 将本单位取消选中，和Select方法差不多，但是格式不是很统一，能跑就行了
    /// </summary>
    public void Deselect()
    {
        MouseManager.Inst.SelectState = MouseManager.SelectStateEnum.Null;
        IsSelected = false;
        QueueRedraw();

        // 在多选和单选的情况下取消选择分别触发不同的事件（单选时会清除整个选中序列，然后重新加入被选中的。而多选时只会将指定的单位从选中序列中移除）
        if (IsMultiSelect)
        {
            OnMultiDeselectUnit();
        }
        else
        {

            OnDeselectUnit();
        }

    }

    /// <summary>
    /// 事件处理器，响应SwitchCounter事件，多选状态时不会将其他算子取消选择
    /// </summary>
    public void DeselectForMultiSelect()
    {
        if (IsMultiSelect) return;

        Deselect();
    }

    /// <summary>
    /// 这个重载是作为Map.ClickCoor事件的处理器而存在的
    /// </summary>
    /// <param name="coor"></param>
    public void Deselect(Vector2I coor)
    {

        IsSelected = false;
        QueueRedraw();

        OnDeselectUnit();
    }



    /// <summary>
    /// 如果算子处于被选中状态，将算子移动到传入的地格坐标处，同时取消选中（调用Deselect方法），并且触发MoveUnit事件通知Main节点更新全局算子状态
    /// 并返回算子移动后的全局坐标
    /// </summary>
    /// <param name="coor"></param>
    public Vector2 Move(Array<Vector2I> path, UnitStack stack)
    {
        if (!IsSelected) return new Vector2(-999, -999);
        if (UnitInfo.MoveLeft == 0) return new Vector2(-999, -999);
        if (UnitInfo.Team != TM.Inst.Team) return new Vector2(-999, -999);

        Vector2 position = new Vector2();


        FormStack(stack);

        _tween = GetTree().CreateTween();   // 创建一个补间实例，实现算子的平滑移动


        float time = 0.2f / path.Count; // 移动总用时为0.2秒

        // 遍历移动路径，将每一次补间都加入Tween实例的队列中（会自动按顺序执行）
        foreach (var coor in path)
        {
            UnitInfo.Coor = coor;
            position = _map.MapToLocal(coor);
            _tween.TweenProperty(this, "position", position, time);
        }


        GetRectPointOnMap();


        Deselect();

        OnMoveUnit();

        UnitInfo.MoveLeft -= 1;
        SelectUnit -= _map.GetHexMpList;    // 没有移动机会后就不需要计算移动范围和显示绿色高光了

        if (UnitInfo.MoveLeft == 0)
        {
            CanActionIcon.Visible = false;
        }


        return position;
    }

    /// <summary>
    /// 在堆叠内按新索引调整位置偏移
    /// </summary>
    /// <param name="newIndex">目标堆叠索引</param>
    public void MoveInStack(int newIndex)
    {
        int deltaIndex = newIndex - StackIndex;

        float offset = 4.0f * deltaIndex;
        //GD.Print("堆叠内移动", StackIndex);
        _tween = GetTree().CreateTween();
        Vector2 pos = new Vector2(Position.X + offset, Position.Y - offset);

        _tween.TweenProperty(this, "position", pos, 0.1);


        StackIndex = newIndex;
    }

    /// <summary>
    /// 事件处理器，响应Map.SelectCoor事件。所以无返回值，将算子移动到传入的地格坐标处
    /// </summary>
    /// <param name="path"></param>
    public void MoveForMarch(Array<Vector2I> path, UnitStack stack)
    {
        Move(path, stack);
    }

    /// <summary>
    /// 堆叠只剩 1 个算子时启用碰撞体
    /// </summary>
    public void RestoreCollisionShape(Array<Vector2I> _, UnitStack _2)
    {
        if (ParentStack.GetCount() == 1)
        {
            CollisionShape2D.Disabled = false;  // 如果算子是堆叠中的唯一一个，就启用它的碰撞体
        }
    }

    /// <summary>
    /// 事件处理器，响应Map.EnterStack事件。将算子移动到传入的地格坐标处，并根据传入的index参数计算偏移量，避免算子重叠（其实主要功能就是移动算子）
    /// </summary>
    /// <param name="coor"></param>
    /// <param name="index">算子在堆叠中的排序索引（就是List中的索引）</param>
    /// <param name="path"></param>
    public void EnterStack(Vector2I coor, UnitStack stack, Array<Vector2I> path)
    {
        if (!IsSelected) return;
        if (UnitInfo.MoveLeft == 0) return;
        if (UnitInfo.Team != TM.Inst.Team) return;

        OnOrderStack(coor);

        Vector2 position = Move(path, stack);

        GetRightStackPosition(position);
    }

    /// <summary>
    /// 按堆叠索引计算偏移，将算子移到堆叠中的正确位置
    /// </summary>
    /// <param name="position">移动后的地格坐标</param>
    public void GetRightStackPosition(Vector2 position)
    {
        float stackOffset = 4.0f * (StackIndex);

        Vector2 pos = new Vector2(position.X + stackOffset, position.Y - stackOffset);

        _tween.TweenProperty(this, "position", pos, 0.05);



    }

    /// <summary>
    /// 堆叠变化时，若本算子在顶部则启用碰撞体
    /// </summary>
    public void StackChanged()
    {

        //GD.Print(ParentStack.UnitIndexOf(UnitInfo) == ParentStack.GetCount() - 1);
        if (ParentStack.UnitIndexOf(UnitInfo) == ParentStack.GetCount() - 1)
        {
            CollisionShape2D.Disabled = false;  // 如果算子在堆叠的顶部，则启用全部碰撞体
        }

    }

    /// <summary>
    /// 将本算子移到所属堆叠的顶部
    /// </summary>
    public void UpToStackTop()
    {
        int index = ParentStack.UnitIndexOf(UnitInfo);
        int maxI = ParentStack.GetCount() - 1;

        OnSelectedInStack(ParentStack.Units[index], ParentStack.Units[maxI], index, maxI);

        (ParentStack.Units[index], ParentStack.Units[maxI]) = (ParentStack.Units[maxI], ParentStack.Units[index]);

        OnOrderStack(UnitInfo.Coor);

    }


    /// <summary>
    /// 将本算子加入指定堆叠，并订阅堆叠变化
    /// </summary>
    /// <param name="stack">目标堆叠</param>
    public void FormStack(UnitStack stack)
    {
        ParentStack.StackChanged -= StackChanged;
        ParentStack = stack;

        int index = stack.UnitIndexOf(UnitInfo);

        StackIndex = index;
        ParentStack.StackChanged += StackChanged;
    }

    /// <summary>
    /// 鼠标悬停时，从所属堆叠高亮对应算子
    /// </summary>
    public void HighlightFromStack()
    {
        _main.HighlightFromStack(ParentStack, UnitInfo);
    }


    /// <summary>
    /// 根据本单位的撤退路线进行撤退的移动，和Move方法逻辑基本类似，但是有一个参数调整撤退的格数
    /// </summary>
    /// <param name="path"></param>
    /// <param name="num">需要撤退的格数</param>
    public void Retreat(Vector2I[] path, int num)
    {
        if (!IsInsideTree()) return;

        Vector2I oldCoor = UnitInfo.Coor;
        Vector2 pos = new Vector2(0, 0);

        _tween = GetTree().CreateTween();
        float time = 0.2f / num;

        for (int i = 0; i < num; i++)
        {
            // 如果撤退路径不支持撤退的格数（即有没被赋值的初始元素），说明撤退路径被控制区或者敌方单位挡住，则将算子歼灭（移出场景树）
            if (path[i] == new Vector2I())
            {
                OnRemoveCounter();
                _tween.Kill();  // 杀死补间实例，避免算子被歼灭后无法加入动画队列导致报错（不可以_tween = null;因为真实的那个实例还在内存中游荡，并试图播放不存在的动画）
                break;
            }

            pos = _map.MapToLocal(path[i]);

            UnitInfo.Coor = path[i];
            _tween.TweenProperty(this, "position", pos, time);

        }

        GetRectPointOnMap();


        OnRetreated(UnitInfo.Coor, oldCoor, this);
        OnOrderStack(UnitInfo.Coor);
        GetRightStackPosition(pos);

        foreach (var i in ParentStack.Units)
        {
            GD.Print(i.ID);
        }

        OnMoveUnit();
    }

    /// <summary>
    /// 降低本单位的AP
    /// </summary>
    /// <param name="level"></param>
    public void LossAP(int level)
    {
        UnitInfo.AP -= level;
        _attackPointLabel.Text = UnitInfo.AP.ToString();
    }

    /// <summary>
    /// 事件处理器，响应事件APC的Attack事件。处理产生的战斗结果
    /// </summary>
    public void ProcessCR()
    {
        // 检测本单位是否在战斗处理器记录的防守单位序列中
        if (APC.Inst.Defenders.Contains(this.UnitInfo))
        {
            // 用战斗结果这个枚举来决定需要执行的操作（撤退或被歼灭）
            switch (APC.Inst.CR)
            {
                case APC.CREnum.DR:
                    Retreat(RetreatPath, 1);
                    break;
                case APC.CREnum.DR2:
                    Retreat(RetreatPath, 2);
                    break;
                case APC.CREnum.DR3:
                    Retreat(RetreatPath, 3);
                    break;
                case APC.CREnum.DE:
                    OnRemoveCounter();
                    break;
                default:
                    break;
            }
        }

        // 同上
        if (APC.Inst.Attackers.Contains(this.UnitInfo))
        {
            UnitInfo.AttackLeft -= 1;
            switch (APC.Inst.CR)
            {
                case APC.CREnum.AR:
                    Retreat(RetreatPath, 1);
                    break;
                case APC.CREnum.AR1:
                    LossAP(1);
                    Retreat(RetreatPath, 1);
                    break;
                case APC.CREnum.AR2:
                    LossAP(2);
                    Retreat(RetreatPath, 1);
                    break;
                case APC.CREnum.AE:
                    OnRemoveCounter();
                    break;
                default:
                    break;
            }
        }

    }

    /// <summary>
    /// 进入移动阶段时，为己方算子恢复移动次数并显示可行动图标
    /// </summary>
    /// <param name="team">当前行动阵营</param>
    public void RefreshMovement(TeamEnum team)
    {
        if (UnitInfo.Team == team)
        {
            UnitInfo.MoveLeft = UnitInfo.MaxMove;
            SelectUnit += _map.GetHexMpList;

            CanActionIcon.Visible = true;
            CanActionIcon.Modulate = new Color("#52AB70");
        }
    }

    /// <summary>
    /// 进入攻击阶段时，为己方算子恢复攻击次数并检测是否可攻击
    /// </summary>
    /// <param name="team">当前行动阵营</param>
    public void RefreshAttack(TeamEnum team)
    {
        if (UnitInfo.Team == team)
        {
            UnitInfo.AttackLeft = UnitInfo.MaxAttack;
            CheckCanAttack();
        }
    }

    /// <summary>
    /// 若本算子处于敌方控制区，将可行动图标改为红色并显示
    /// </summary>
    public void CheckCanAttack()
    {
        List<AxialCoor> zocs = _map.GetCorrZocs(UnitInfo.Team);
        if (zocs.Contains(UnitInfo.CoorOfAxial))
        {
            CanActionIcon.Modulate = new Color("#E84D39");
            CanActionIcon.Visible = true;

        }
    }

    /// <summary>
    /// 切换阶段时取消移动范围计算，并隐藏可行动图标
    /// </summary>
    public void ChangePhrase()
    {
        SelectUnit -= _map.GetHexMpList;
        CanActionIcon.Visible = false;
    }




    /// <summary>
    /// 堆叠内算子被选中时触发
    /// </summary>
    /// <param name="sU">被选中的算子</param>
    /// <param name="tU">在顶上的算子</param>
    /// <param name="i">被选中算子的原索引</param>
    /// <param name="m">最大索引</param>
    protected virtual void OnSelectedInStack(UnitInfo sU, UnitInfo tU, int i, int m)
    {
        EmitSignal(SignalName.SelectedInStack, sU, tU, i, m);
    }

    /// <summary>
    /// 堆叠内算子被选中时发出的信号
    /// </summary>
    [Signal] public delegate void SelectedInStackEventHandler(UnitInfo sU, UnitInfo tU, int i, int m);


    /// <summary>
    /// 触发 SelectUnit 信号
    /// </summary>
    protected virtual void OnSelectUnit()
    {
        EmitSignal(SignalName.SelectUnit, UnitInfo);
    }

    /// <summary>
    /// 算子被选中时发出的信号
    /// </summary>
    [Signal] public delegate void SelectUnitEventHandler(UnitInfo unitInfo);

    /// <summary>
    /// 触发 DeselectUnit 信号
    /// </summary>
    protected virtual void OnDeselectUnit()
    {
        EmitSignal(SignalName.DeselectUnit);
    }

    /// <summary>
    /// 算子取消选中时发出的信号
    /// </summary>
    [Signal] public delegate void DeselectUnitEventHandler();

    /// <summary>
    /// 触发 MultiDeselectUnit 信号
    /// </summary>
    protected virtual void OnMultiDeselectUnit()
    {
        EmitSignal(SignalName.MultiDeselectUnit, this.UnitInfo);
    }

    /// <summary>
    /// 多选状态下取消选中时发出的信号
    /// </summary>
    [Signal] public delegate void MultiDeselectUnitEventHandler(UnitInfo unit);

    /// <summary>
    /// 触发 MoveUnit 信号
    /// </summary>
    protected virtual void OnMoveUnit()
    {
        EmitSignal(SignalName.MoveUnit);
    }

    /// <summary>
    /// 算子移动后发出的信号
    /// </summary>
    [Signal] public delegate void MoveUnitEventHandler();

    /// <summary>
    /// 触发 RemoveCounter 信号
    /// </summary>
    protected virtual void OnRemoveCounter()
    {
        EmitSignal(SignalName.RemoveCounter, this);
    }

    /// <summary>
    /// 算子被歼灭移除时发出的信号
    /// </summary>
    [Signal] public delegate void RemoveCounterEventHandler(Counter counter);


    /// <summary>
    /// 触发 MultiSelectEvent 信号
    /// </summary>
    protected virtual void OnMultiSelectEvent()
    {
        EmitSignal(SignalName.MultiSelectEvent);
    }

    /// <summary>
    /// 算子在多选状态下被选中时发出的信号
    /// </summary>
    [Signal] public delegate void MultiSelectEventEventHandler();

    /// <summary>
    /// 触发 OrderStack 信号，通知按坐标重排/整理堆叠
    /// </summary>
    /// <param name="coor">需要整理的地格坐标</param>
    protected virtual void OnOrderStack(Vector2I coor)
    {
        EmitSignal(SignalName.OrderStack, coor);
    }

    /// <summary>
    /// 需要按坐标重排/整理堆叠时发出的信号
    /// </summary>
    [Signal] public delegate void OrderStackEventHandler(Vector2I coor);




    /// <summary>
    /// 触发 Retreated 信号
    /// </summary>
    /// <param name="newCoor">撤退后的地格坐标</param>
    /// <param name="oldCoor">撤退前的地格坐标</param>
    /// <param name="counter">撤退的算子</param>
    protected virtual void OnRetreated(Vector2I newCoor, Vector2I oldCoor, Counter counter)
    {
        EmitSignal(SignalName.Retreated, newCoor, oldCoor, counter);
    }

    /// <summary>
    /// 算子撤退完成后发出的信号
    /// </summary>
    [Signal] public delegate void RetreatedEventHandler(Vector2I coor, Vector2I oldCoor, Counter counter);

}
