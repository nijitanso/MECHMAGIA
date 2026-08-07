using Godot;
using System;

public partial class MouseManager : Node
{

    public static MouseManager Instance { get; private set; }

    // 为了确保 Instance 在场景树中唯一，使用 _EnterTree 方法来设置 Instance（单例）
    public override void _EnterTree()
    {
        Instance = this;
    }



    public bool IsMouseLeftHolding { get; set; } = false;
    public double MouseLeftHoldingTime { get; set; }
    public long ClickedMouseButton { get; set; }

    private Label mouseClickTimeShower = null;
    // Called when the node enters the scene tree for the first time.


    public override void _Ready()
    {
        CallDeferred(nameof(InitializeNode));
    }

    private void InitializeNode()
    {
        mouseClickTimeShower = GetNode<Label>("/root/Game/MouseClickTimeShower");
        if (mouseClickTimeShower == null)
        {
            GD.PrintErr("没有找到 MouseClickTimeShower 节点！");
        }
        else
        {
            mouseClickTimeShower.Text = "找到MouseClickTimeShower 节点！";
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {

       

        ClickedMouseButton = (long)(Input.GetMouseButtonMask());
        if (ClickedMouseButton == 1)
        {
            if (!IsMouseLeftHolding)
            {
                IsMouseLeftHolding = true;
                MouseLeftHoldingTime = 0;
            }
            else
            {
                MouseLeftHoldingTime += delta;
            }
        }
        else
        {
            if (IsMouseLeftHolding)
            {
                IsMouseLeftHolding = false;
                MouseLeftHoldingTime = 0;
            }
        }

        if (mouseClickTimeShower != null)
            mouseClickTimeShower.Text = $"鼠标左键按下时长：{MouseLeftHoldingTime:F2}";
    }
}
