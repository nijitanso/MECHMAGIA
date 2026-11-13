using Godot;
using System;

public partial class PauseIcon : TextureRect
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        ProcessMode = ProcessModeEnum.Always;

        Atlas = Texture as AtlasTexture;

        MouseEntered += Hovering;

        MouseExited += NotHovering;

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public AtlasTexture Atlas { get; set; }
    private float X { get; set; } = 0.0f;  // 图集的x坐标，用于切换不同的图片

    private bool _isHovering = false;


    private void Click()
    {
        if (_isHovering)
        {
            Modulate = new Color(1, 1, 1, 0.5f); // 点击时变为半透明
        }
    }

    private void Release()
    {
        Modulate = new Color(1, 1, 1, 1); // 松开鼠标时恢复为不透明
        OnPauseIconClicked(); // 触发点击事件，通知Menu暂停游戏
    }

    public void SwitchAtlas()
    {
        X = (X == 0.0f) ? 32.0f : 0.0f;
        Rect2 rect = new(X, 0.0f, 32.0f, 32.0f);

        Atlas.Region = rect;
    }

    /// <summary>
    /// 事件处理器，当鼠标进入这个节点时调用。设置_isHovering为true，表示鼠标正在悬停在这个节点上
    /// </summary>
    private void Hovering()
    {
        _isHovering = true;
    }

    /// <summary>
    /// 事件处理器，当鼠标离开这个节点时调用。设置_isHovering为false，表示鼠标已经离开这个节点
    /// </summary>
    private void NotHovering()
    {
        _isHovering = false;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                Click();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
            {
                Release();
            }
        }
    }

    [Signal] public delegate void PauseIconClickedEventHandler();

    public void OnPauseIconClicked()
    {
        EmitSignal(SignalName.PauseIconClicked);
    }
}
