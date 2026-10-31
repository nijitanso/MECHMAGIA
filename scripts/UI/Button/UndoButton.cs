using Godot;
using System;


public partial class UndoButton : ButtonBase
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_buttonHL = GetChild<TextureRect>(0);

		MouseEntered += Highlight;
		MouseEntered += Hovering;

		MouseExited += Nothighlight;
		MouseExited += NotHovering;

		MouseFilter = MouseFilterEnum.Stop;

		GD.Print(_buttonHL.GetHashCode());

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public bool IsUndone { get; set; } = false;


    public override void Release()
	{
		base.Release();

		OnTryUndoState();   // 触发TryUndoState信号，通知Main节点尝试撤回状态（之所以不直接调用SM单例的方法是因为需要保存在Main的Counters序列）
		if (IsUndone)
		{
            FlipH = false;  
        }
		else
		{
            FlipH = true;   // 如果已经撤回了，将按钮反转，表示此时再按下就是恢复状态
        }

        IsUndone = !IsUndone;   // 切换状态

    }

	public void OnTryUndoState()
	{
		EmitSignal(SignalName.TryUndoState);
	}

	[Signal] public delegate void TryUndoStateEventHandler();
}
