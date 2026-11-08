using Godot;
using Managers;
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

		TurnManager.Inst.NextPhase += () =>
		{
            FlipH = false;   // 当进入下一阶段时，将按钮翻转为撤回状态
        };



    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}




    public override void Release()
	{
		base.Release();

		if (!FlipH)
		{
            OnTryUndoState();   // 触发TryUndoState信号，通知Main节点尝试撤回状态（之所以不直接调用SM单例的方法是因为需要保存在Main的Counters序列）
        }
		else
		{
            OnTryRedoState();   // 触发TryRedoState信号，通知Main节点尝试恢复状态（之所以不直接调用SM单例的方法是因为需要保存在Main的Counters序列）
        }


    }

	public void FlipButton()
	{
        FlipH = !FlipH;
    }



    public void OnTryUndoState()
	{
		EmitSignal(SignalName.TryUndoState);
	}

	[Signal] public delegate void TryUndoStateEventHandler();

    public void OnTryRedoState()
    {
        EmitSignal(SignalName.TryRedoState);
    }

    [Signal] public delegate void TryRedoStateEventHandler();
}
