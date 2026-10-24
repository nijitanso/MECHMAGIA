using Godot;
using System;

/// <summary>
/// 是游戏内大部分UI按钮空间的基类
/// </summary>
public partial class ButtonBase : TextureRect
{
    public bool IsHovering { get; set; } = false;
    protected TextureRect _buttonHL;

    public void Highlight()
    {
        _buttonHL.Visible = true;
    }

    /// <summary>
    /// Hides the attack button's highlight.
    /// </summary>
	/// <remarks>Has no effect if the highlight is already hidden.</remarks>
    public void Nothighlight()
    {
        _buttonHL.Visible = false;
    }

    /// <summary>
    /// 鼠标左键按下时触发，不会有实际效果，只是交互显示
    /// </summary>
    public void Click()
    {
        if (IsHovering)
        {
            Nothighlight(); // 点击时隐藏高光，更自然
            Modulate = new Color(1, 1, 1, 0.5f); // 点击时变为半透明
        }
    }

    /// <summary>
    /// 鼠标左键释放时触发，实际是触发效果的方法
    /// </summary>
    public void Release()
    {
        GD.Print("AttackButton Released");
        Modulate = new Color(1, 1, 1, 1); // 松开鼠标时恢复为不透明

        // 因为按下时将高光隐藏了，所以松开时要重新显示高光，如果鼠标仍然悬停在按钮上
        if (IsHovering)
        {
            Highlight();
        }
    }

    /// <summary>
    /// 事件处理器，当鼠标进入这个节点时调用。设置_isHovering为true，表示鼠标正在悬停在这个节点上
    /// </summary>
    protected void Hovering()
    {
        IsHovering = true;
    }

    /// <summary>
    /// 事件处理器，当鼠标离开这个节点时调用。设置_isHovering为false，表示鼠标已经离开这个节点
    /// </summary>
    protected void NotHovering()
    {
        IsHovering = false;
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
}
