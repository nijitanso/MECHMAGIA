using Godot;
using System;
using MM = Managers.MouseManager;
using TTG = Generator.ToolTipGenerator;

public partial class ToolTip : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _panelContainer = GetNode<PanelContainer>("PanelContainer");
        _label = GetNode<Label>("PanelContainer/MarginContainer/Label");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdatePos();
        ShouldShow(delta);

    }

    private Vector2 _lastMousePos = new Vector2();
    [Export] public float SpeedThreshold = 100f; // 速度阈值，可根据需要调整
    private float _timerForShow = 0f; // 更新间隔，单位为秒
    private const float TipMargin = 15f;
    [Export] public float UpdateInterval = 0.5f; // 更新间隔，单位为秒
    private PanelContainer _panelContainer;
    private Label _label;





    /// <summary>
    /// 更新FloatingDescription的位置，使其跟随鼠标移动。根据鼠标具体的位置决定显示位置，尽量避免提示框被遮住。预期显示位置为鼠标右下方，若鼠标在屏幕右侧，则显示在鼠标左下方；若鼠标在屏幕下方，则显示在鼠标右上方；若鼠标在屏幕右下角，则显示在鼠标左上方。
    /// 总而言之，显示位置的优先级是右>左，下>上。
    /// </summary>
    public void UpdatePos()
    {
        var mousePos = GetGlobalMousePosition();

        float xOffset = TipMargin;
        float yOffset = TipMargin;

        Rect2 screenRect = GetViewport().GetVisibleRect();

        float screenWidth = screenRect.End.X;
        float screenHeight = screenRect.End.Y;


        if ((mousePos.X + xOffset + _panelContainer.Size.X) > screenWidth)
        {
            xOffset = -(_panelContainer.Size.X + TipMargin);
        }

        if ((mousePos.Y + yOffset + _panelContainer.Size.Y) > screenHeight)
        {
            yOffset = -(_panelContainer.Size.Y + TipMargin);
        }

        Position = mousePos + new Vector2(xOffset, yOffset);

        _label.Text = TTG.TooltipText;


    }
/*
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            // 判断左键时是否悬停在地图上
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                GD.Print($"鼠标位置：{GetGlobalMousePosition()}");
            }
        }
    }
*/
    /// <summary>
    /// 根据鼠标当前悬浮的对象，显示对应的描述信息
    /// </summary>
    public new void Show()
	{
        if (Visible) return;

        

		Visible = true;
    }



    /// <summary>
    /// 判断鼠标状态，决定是否显示FloatingDescription。判断逻辑采用计算鼠标某段时刻内的平均滑动速度，若平均速度小于某个阈值，则显示FloatingDescription，否则隐藏。
    /// </summary>
    public void ShouldShow(double delta)
    {
        

        Vector2 mousePos = GetGlobalMousePosition();
        float dist = (mousePos - _lastMousePos).Length();
        float speed = dist / (float)delta;


        if (speed < SpeedThreshold * 5) // 速度阈值，可根据需要调整
        {
            if (!Visible)
            {
                _timerForShow += (float)delta;
            }

            if (_timerForShow > UpdateInterval)
            {
                Show();
                _timerForShow = 0f; // 重置计时器
            }
            
        }
        else
        {
            Visible = false;
        }


        _lastMousePos = mousePos;

    }


}
