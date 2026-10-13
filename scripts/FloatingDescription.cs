using Godot;
using System;
using MM = Managers.MouseManager;

public partial class FloatingDescription : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdatePos();
        ShouldShow(delta);

    }

    private Vector2 _lastMousePos = new Vector2();
    [Export] public float SpeedThreshold = 100f; // 速度阈值，可根据需要调整
    [Export] public float TimerForShow = 0f; // 更新间隔，单位为秒]




    /// <summary>
    /// 更新FloatingDescription的位置，使其跟随鼠标移动
    /// </summary>
    public void UpdatePos()
    {
        var mousePos = GetGlobalMousePosition();
        Position = mousePos + new Vector2(15, 15);
    }

    /// <summary>
    /// 根据鼠标当前悬浮的对象，显示对应的描述信息
    /// </summary>
    public new void Show()
	{
        if (Visible) return;

        ProcessText();
		Visible = true;
    }

	public void ProcessText()
    {

    }

    /// <summary>
    /// 判断鼠标状态，决定是否显示FloatingDescription。判断逻辑采用计算鼠标某段时刻内的平均滑动速度，若平均速度小于某个阈值，则显示FloatingDescription，否则隐藏。
    /// </summary>
    public void ShouldShow(double delta)
    {
        

        Vector2 mousePos = GetGlobalMousePosition();
        float dist = (mousePos - _lastMousePos).Length();
        float speed = dist / (float)delta;
        GD.Print($"Mouse speed: {speed}, {SpeedThreshold * 5}"); // 打印鼠标速度，便于调试


        if (speed < SpeedThreshold * 5) // 速度阈值，可根据需要调整
        {
            if (!Visible)
            {
                TimerForShow += (float)delta;
            }

            if (TimerForShow > 0.5f)
            {
                Show();
                TimerForShow = 0f; // 重置计时器
            }
            
        }
        else
        {
            Visible = false;
        }


        _lastMousePos = mousePos;

    }


}
