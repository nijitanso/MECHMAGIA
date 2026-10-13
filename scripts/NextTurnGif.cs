using Godot;
using System;
using System.Threading.Tasks;
using MM = Managers.MouseManager;
using TM = Managers.TurnManager;

public partial class NextTurnGif : TextureRect
{

    public int Frame { get; set; } = 0;
    // 用两个布尔值来控制动画的竞态
    public bool CanPlayingForward { get; set; } = false;
    public bool CanPlayingBackward { get; set; } = false;
    public AtlasTexture Atlas { get; set; }
    public int SpriteSize { get; set; } = 32;   // 每帧的大小，单位为像素
    public int TotalFrame { get; set; } = 5; // 总帧数
    [Export] public int FrameSpeed { get; set; } = 100;     // 每帧的播放速度，单位为毫秒

    private bool _isHovering = false;

    public override void _Ready()
    {
        Atlas = Texture as AtlasTexture;

        MouseEntered += PlayForward;
        MouseEntered += Hovering;

        MouseExited += PlayBackward;
        MouseExited += NotHovering;



        MouseFilter = MouseFilterEnum.Stop;


    }

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

        TM.Inst.MoveToNextPhase(); // 调用TurnManager的NextPhase方法，切换到下一阶段
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



    /// <summary>
    /// 事件处理器，当鼠标进入这个节点时调用。按帧顺序播放动画，采用每帧固定FrameSpeed的速度播放
    /// </summary>
    public async void PlayForward()
    {
        CanPlayingBackward = false;
        CanPlayingForward = true;

        while (Frame < TotalFrame - 1 && CanPlayingForward)
        {
            Frame++;	// 通过记录当前帧的索引来控制动画的播放顺序

            // 根据帧索引来剪切图集中的对应帧区域，并将其设置为AtlasTexture的Region属性，从而实现动画的播放
            float x = Frame * SpriteSize;
            Rect2 rect = new Rect2(x, 0.0f, 32.0f, 32.0f);

            Atlas.Region = rect;

            await Task.Delay(FrameSpeed);

        }


    }


    /// <summary>
    /// 事件处理器，当鼠标离开这个节点时调用。按帧顺序的倒序播放动画，采用每帧固定FrameSpeed的速度播放
    /// </summary>
    public async void PlayBackward()
    {
        CanPlayingForward = false;
        CanPlayingBackward = true;

        while (Frame != 0 && CanPlayingBackward)
        {
            Frame--;    // 通过记录当前帧的索引来控制动画的播放顺序

            // 根据帧索引来剪切图集中的对应帧区域，并将其设置为AtlasTexture的Region属性，从而实现动画的播放
            float x = Frame * SpriteSize;
            Rect2 rect = new Rect2(x, 0.0f, 32.0f, 32.0f);

            Atlas.Region = rect;

            await Task.Delay(FrameSpeed);

        }
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
