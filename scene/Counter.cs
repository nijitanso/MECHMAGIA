using Godot;
using Godot.Collections;
using System;
using Data;

public partial class Counter : Area2D
{
	//	这些数值将从数值类中获取
	public int ID { get; set; }
	public TeamEnum Team { get; set; }
	public int AttackPoint { get; set; }
	public int DefendkPoint { get; set; }
	public int MovePoint { get; set; } = 2;
	public Vector2I CoorOnHex { get; set; } = new Vector2I(0, 0);


	private Array<Vector2I> _hexOffsetCoors;    // 没看出来有什么用

	// 对节点的引用
	private Label _attackPointLabel;
	private Label _defendPointLabel;
	private Label _movePointLabel;
	private CollisionShape2D _collisionShape2D;
	private Node2D _upperLayer;
	private Map _map;
	private MapInteraction _mapInteraction;
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
	private Vector2 _newPosition;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// 获取引用
		_map = GetNode<Map>("/root/Main/Map");
		_mapInteraction = GetNode<MapInteraction>("/root/Main/Map/MapInteraction");
		_attackPointLabel = GetNode<Label>("AttackPointLabel");
		_defendPointLabel = GetNode<Label>("DefendPointLabel");
		_movePointLabel = GetNode<Label>("MovePointLabel");
		_collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
		_upperLayer = GetNode<Node2D>("UpperLayer");
        _bodySprite2D = GetNode<Sprite2D>("BodySprite2D");


		// 算子的状态初始化
		Init();


        // 计算算子大小
        _size = _collisionShape2D.Shape.GetRect().Size;
		_topLeftPosition = new Vector2(-_size.X / 2.0f, -_size.Y / 2.0f);
		_topRightPosition = new Vector2(_size.X / 2.0f, -_size.Y / 2.0f);
		_downLeftPosition = new Vector2(-_size.X / 2.0f, _size.Y / 2.0f);
		_downRightPosition = new Vector2(_size.X / 2.0f, _size.Y / 2.0f);


		// 绑定事件（同一个事件绑定不同事件处理器时，绑定的顺序将影响事件处理器被调用的顺序，不能随意调换）
		// TODO：这里各种事件的使用逻辑有些混乱，想办法好好注释，解释整个流程
		
		// 当鼠标进入算子时触发此事件
		MouseEntered += HoveringHighlight;  // 调用自己的边框高亮方法
		MouseEntered += MouseManager.Inst.HoverSwitchToCounter; // 调用MouseManager的方法，将悬停状态改为算子


		// 当鼠标离开算子时触发此事件，具体的事件处理器逻辑相同，不再赘述
		MouseExited += NotHoveringHighlight;
		MouseExited += MouseManager.Inst.HoverSwitchToMap;

		// 当有算子被选择时触发此事件，没有被选中的算子将会调用自身的Deselect方法（在SwitchDeselect方法内做判断并调用）
		MouseManager.Inst.SwitchCounter += SwitchDeselect;

		_map.SelectCoor += Move;    // 当地格被选中时触发此事件
		_map.ClickCoor += Deselect;

		// 当算子被选中时触发事件，这里的绑定顺序一定不能调换，因为是先清除上一次选中时显示的绿色高亮再显示新的
		SelectUnit += _map.DisclickCellForUnit;
        SelectUnit += MouseManager.Inst.SelectSwitchToCounter;
		SelectUnit += _map.GetHexMpList;	// 调用计算最小路径的方法，获取算子移动范围，并显示绿色高光

		DeselectUnit += _map.RemoveGreen;

		QueueRedraw();  // 这个重绘可能时实现阴影时留下的，现在有了更好的实现方法，但还是不敢动这行




	}

	public void Init()
	{
        Position = _map.MapToLocal(CoorOnHex);
        _newPosition = Position;

        _movePointLabel.Text = MovePoint.ToString();

        _hexOffsetCoors = _map.GetUsedCells();


		switch (Team)
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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Position = Position.Lerp(_newPosition, 10.0f * (float)delta);   // 只是一行平滑移动
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
	/// 重写的_Draw方法，每次调用QueueRedraw时都会重新绘制游戏画面。注意更改状态不需要对画布进行清除，只需要“不绘制“即可，因为_Draw方法本来就是每帧都被调用的
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

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && IsHovering)
			{
				if (!IsSelected)
				{
					// 和HoveringHighlight方法差不多的处理，更新状态和重绘，但多了触发事件的一行
					Select();
				}
				else
				{
					Deselect();
				}


			}
		}
	}

	public void Select()
	{
		IsSelected = true;
		QueueRedraw();

		OnSelectUnit();
	}

	public void Deselect()
	{

		IsSelected = false;
		QueueRedraw();

		OnDeselectUnit();
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

    public void SwitchDeselect()
	{
		if (MouseManager.Inst.SelectedUnitID != ID)
		{
			Deselect();
		}
	}



    /// <summary>
    /// 如果算子处于被选中状态，将算子移动到传入的地格坐标处，同时取消选中（调用Deselect方法）
    /// </summary>
    /// <param name="coor"></param>
    public void Move(Vector2I coor)
	{
		if (IsSelected)
		{
			CoorOnHex = coor;
			_newPosition = _map.MapToLocal(coor);

			Deselect();

        }

	}

	protected virtual void OnSelectUnit()
	{
		EmitSignal(SignalName.SelectUnit, CoorOnHex, MovePoint, ID);
	}

	protected virtual void OnDeselectUnit()
	{
		EmitSignal(SignalName.DeselectUnit);
	}


	[Signal] public delegate void SelectUnitEventHandler(Vector2I CoorOnHex, int MP, int ID);
	[Signal] public delegate void DeselectUnitEventHandler();
}
