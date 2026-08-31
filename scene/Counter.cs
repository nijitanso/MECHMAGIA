using Data;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using APC = ActionProcessor.AttackProcessor;

public partial class Counter : Area2D
{
    //	UnitInfo类是每一个算子用来储存单位数据的数据类
    public UnitInfo UnitInfo { get; set; } = new UnitInfo();


    // 对节点的引用
    private Label _attackPointLabel;
    private Label _defendPointLabel;
    private Label _movePointLabel;
    private CollisionShape2D _collisionShape2D;
    private Node2D _upperLayer;
    private Map _map;
    private Main _main;
    private Sprite2D _bodySprite2D;

    // 算子对自身大小信息的储存，用于绘制鼠标悬停和选中边框
    private Vector2 _topLeftPosition;
    private Vector2 _topRightPosition;
    private Vector2 _downLeftPosition;
    private Vector2 _downRightPosition;
    private Vector2 _size;

    // 状态属性
    public bool IsHovering { get; private set; } = false;
    public bool IsSelected { get; private set; } = false;
    public bool IsMultiSelect { get; set; } = false;

    public Vector2I[] RetreatPath { get; set; }


    private Tween _tween;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // 获取引用
        _map = GetNode<Map>("/root/Main/Map");
        _attackPointLabel = GetNode<Label>("AttackPointLabel");
        _defendPointLabel = GetNode<Label>("DefendPointLabel");
        _movePointLabel = GetNode<Label>("MovePointLabel");
        _collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        _upperLayer = GetNode<Node2D>("UpperLayer");
        _bodySprite2D = GetNode<Sprite2D>("BodySprite2D");
        _main = GetParent<Main>();

        // 算子的状态初始化
        Init();


        // 计算算子大小
        _size = _collisionShape2D.Shape.GetRect().Size;
        _topLeftPosition = new Vector2(-_size.X / 2.0f, -_size.Y / 2.0f);
        _topRightPosition = new Vector2(_size.X / 2.0f, -_size.Y / 2.0f);
        _downLeftPosition = new Vector2(-_size.X / 2.0f, _size.Y / 2.0f);
        _downRightPosition = new Vector2(_size.X / 2.0f, _size.Y / 2.0f);


        // 绑定事件（同一个事件绑定不同事件处理器时，绑定的顺序将影响事件处理器被调用的顺序，不能随意调换）
        // TODO：这里各种事件的订阅的范式不标准（应该在订阅者那边订阅，而不是发布者），考虑重构成本（或许可以实现一个事件总线）并且注意以后的事件订阅


        _main.UnitsUpdate += SetRetreatPath;    // 地图上的算子状态有变时更新自己的撤退路线，以免过期导致撤退有问题

        // 当鼠标进入算子时触发此事件
        MouseEntered += HoveringHighlight;  // 调用自己的边框高亮方法
        MouseEntered += MouseManager.Inst.HoverSwitchToCounter; // 调用MouseManager的方法，将悬停状态改为算子MouseEntered
        MouseEntered += SetHoveringUnit;

        // 当鼠标离开算子时触发此事件，具体的事件处理器逻辑相同，不再赘述
        MouseExited += NotHoveringHighlight;
        MouseExited += MouseManager.Inst.HoverSwitchToMap;
        MouseExited += SetHoveringUnitToNull;

        // 当有算子被选择时触发此事件，没有被选中的算子将会调用自身的Deselect方法（在SwitchDeselect方法内做判断并调用）
        MouseManager.Inst.SwitchCounter += DeselectForMultiSelect;

        _map.SelectCoor += Move;    // 当地格被选中时触发此事件
        _map.ClickCoor += Deselect;

        // 当算子被选中时触发事件，这里的绑定顺序一定不能调换，因为是先清除上一次选中时显示的绿色高亮再显示新的
        SelectUnit += _map.DisclickCellForUnit;
        SelectUnit += MouseManager.Inst.SelectSwitchToCounter;
        SelectUnit += MouseManager.Inst.SetSelectedUnits;
        SelectUnit += _map.GetHexMpList;    // 调用计算最小路径的方法，获取算子移动范围，并显示绿色高光
        SelectUnit += _map.ShowZoc;

        DeselectUnit += _map.RemoveGreen;
        DeselectUnit += _map.RemoveZoc;
        DeselectUnit += MouseManager.Inst.ClearSelectedUnits;

        MultiSelect += _map.RemoveGreen;    // 当此时算子是被多选选中时（即Ctrl被按下时）移除绿色高光（因为算子在多选状态不能移动）

        APC.Inst.Attack += Deselect;
        APC.Inst.Attack += ProcessCR;


        QueueRedraw();  // 这个重绘可能时实现阴影时留下的，现在有了更好的实现方法，但还是不敢动这行





    }

    /// <summary>
    /// 对单位状态进行初始化
    /// </summary>
    public void Init()
    {
        // 设置位置
        Position = _map.MapToLocal(UnitInfo.Coor);

        // 设置算子纹理上的数值数字
        _attackPointLabel.Text = UnitInfo.AP.ToString();
        _defendPointLabel.Text = UnitInfo.DP.ToString();
        _movePointLabel.Text = UnitInfo.MP.ToString();

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
            IsMultiSelect= false;
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
        IsHovering = true;
        Scale = new Vector2(1.07f, 1.07f);
        QueueRedraw();
    }

    /// <summary>
    /// HoveringHighlight方法的反向操作
    /// </summary>
    public void NotHoveringHighlight()
    {
        IsHovering = false;
        Scale = new Vector2(1.0f, 1.0f);
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
                if (!IsSelected)
                {
                    Select();
                }
                else
                {
                    Deselect();
                }
            }
        }
    }

    /// <summary>
    /// 将本单位选中，触发事件SelectUnit并绘制选中边框。SelectUnit事件绑定了很多事件处理器
    /// </summary>
    public void Select()
    {
        OnSelectUnit();

        if (IsMultiSelect)
        {
            OnMultiSelect();
        }

        IsSelected = true;
        QueueRedraw();
    }

    /// <summary>
    /// 将本单位取消选中，和Select方法差不多，但是格式不是很统一，能跑就行了
    /// </summary>
    public void Deselect()
    {
        MouseManager.Inst.SelectState = MouseManager.SelectStateEnum.Null;
        IsSelected = false;
        QueueRedraw();

        OnDeselectUnit();
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
    /// </summary>
    /// <param name="coor"></param>
    public void Move(Array<Vector2I> path)
    {
        if (!IsSelected) return;

        _tween = GetTree().CreateTween();   // 创建一个补间实例，实现算子的平滑移动

        float time = 0.2f / path.Count; // 移动总用时为0.2秒

        // 遍历移动路径，将每一次补间都加入Tween实例的队列中（会自动按顺序执行）
        foreach (var coor in path)
        {
            UnitInfo.Coor = coor;
            _tween.TweenProperty(this, "position", _map.MapToLocal(coor), time);
        }

        Deselect();

        OnMoveUnit();
    }

    /// <summary>
    /// 根据本单位的撤退路线进行撤退的移动，和Move方法逻辑基本类似，但是有一个参数调整撤退的格数
    /// </summary>
    /// <param name="path"></param>
    /// <param name="num">需要撤退的格数</param>
    public void Retreat(Vector2I[] path, int num)
    {
        if (!IsInsideTree()) return;

        _tween = GetTree().CreateTween();
        float time = 0.2f / num;

        for (int i = 0; i < num; i++)
        {
            UnitInfo.Coor = path[i];
            _tween.TweenProperty(this, "position", _map.MapToLocal(path[i]), time);

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

    protected virtual void OnSelectUnit()
    {
        EmitSignal(SignalName.SelectUnit, UnitInfo);
    }

    [Signal] public delegate void SelectUnitEventHandler(UnitInfo unitInfo);

    protected virtual void OnDeselectUnit()
    {
        EmitSignal(SignalName.DeselectUnit);
    }

    [Signal] public delegate void DeselectUnitEventHandler();

    protected virtual void OnMoveUnit()
    {
        EmitSignal(SignalName.MoveUnit);
    }

    [Signal] public delegate void MoveUnitEventHandler();

    protected virtual void OnRemoveCounter()
    {
        EmitSignal(SignalName.RemoveCounter, this);
    }

    [Signal] public delegate void RemoveCounterEventHandler(Counter counter);


    protected virtual void OnMultiSelect()
    {
        EmitSignal(SignalName.MultiSelect);
    }

    [Signal] public delegate void MultiSelectEventHandler();
}
