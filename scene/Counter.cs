using Godot;
using System;

public partial class Counter : Area2D
{
	//	这些数值将从数值类中获取
	public int AttackPoint { get; set; }
	public int DefendkPoint { get; set; }
	public int MovePoint { get; set; } = 5;
    [Export] public Vector2I CoorOnHex { get; set; } = new Vector2I(0, 0);	//TODO：要写一个setter，防止算子的坐标被设置为地图没有的图格

    private Label _attackPointLabel;
	private Label _defendPointLabel;
	private Label _movePointLabel;
	private CollisionShape2D _collisionShape2D;
	private Node2D _upperLayer;

	private TileMapLayer _map;
    private MapInteraction _mapInteraction;


    private Vector2 _topLeftPosition;
	private Vector2 _topRightPosition;
	private Vector2 _downLeftPosition;
	private Vector2 _downRightPosition;

	private Vector2 _size;

	public bool IsHovering { get; private set; } = false;
	private bool _isSelected = false;

	private Vector2 _newPosition;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		

		_map = GetNode<TileMapLayer>("/root/Game/Map");
        _mapInteraction = GetNode<MapInteraction>("/root/Game/Map/MapInteraction");
        _attackPointLabel = GetNode<Label>("AttackPointLabel");
		_defendPointLabel = GetNode<Label>("DefendPointLabel");
		_movePointLabel = GetNode<Label>("MovePointLabel");
		_collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
		_upperLayer = GetNode<Node2D>("UpperLayer");

        _movePointLabel.Text = MovePoint.ToString();

        Position = _map.MapToLocal(CoorOnHex);
		_newPosition = Position;


        _size = _collisionShape2D.Shape.GetRect().Size;
		_topLeftPosition = new Vector2(-_size.X / 2.0f, -_size.Y / 2.0f);
		_topRightPosition = new Vector2(_size.X / 2.0f, -_size.Y / 2.0f);
		_downLeftPosition = new Vector2(-_size.X / 2.0f, _size.Y / 2.0f);
		_downRightPosition = new Vector2(_size.X / 2.0f, _size.Y / 2.0f);

		GD.Print(_topLeftPosition);

		MouseEntered += HoveringHighlight;
		MouseExited += NotHoveringHighlight;
		
		QueueRedraw();




		_mapInteraction.SelectCoor += Move;





    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        Position = Position.Lerp(_newPosition, 10.0f * (float)delta);
        //GD.Print(GetGlobalMousePosition());
    }

	public void HoveringHighlight()
	{
        IsHovering = true;
		Scale = new Vector2(1.07f, 1.07f);
		QueueRedraw();
	}
	public void NotHoveringHighlight()
	{
        IsHovering = false;
		Scale = new Vector2(1.0f, 1.0f);
		QueueRedraw();
	}

	public override void _Draw()
	{


		if (IsHovering && !_isSelected)
		{
			DrawDashedLine(_topLeftPosition, _topRightPosition, Color.Color8(255, 255, 255), width: 4.0f);
			DrawDashedLine(_topLeftPosition, _downLeftPosition, Color.Color8(255, 255, 255), width: 4.0f);
			DrawDashedLine(_downLeftPosition, _downRightPosition, Color.Color8(255, 255, 255), width: 4.0f);
			DrawDashedLine(_downRightPosition, _topRightPosition, Color.Color8(255, 255, 255), width: 4.0f);

		}

		if (_isSelected)
		{
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
				if (!_isSelected)
				{
                    _isSelected = true;
                    QueueRedraw();
                }
				else
				{
                    _isSelected = false;
                    QueueRedraw();
                }
				

			}
		}
	}

	public void Move(Vector2I coor)
	{
		if (_isSelected)
		{
            CoorOnHex = coor;
            _newPosition = _map.MapToLocal(coor);

			_isSelected = false;
        }
		
	}
}
