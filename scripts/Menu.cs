using Godot;
using System;

public partial class Menu : CanvasLayer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        // 让这个节点在游戏暂停时也能处理输入事件
        ProcessMode = ProcessModeEnum.Always;
		Visible = false;

        _pauseIcon = GetNode<PauseIcon>("/root/Main/GUI/VBoxContainer2/MarginContainer/PauseIcon");

        _pauseIcon.PauseIconClicked += TogglePause;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    private PauseIcon _pauseIcon;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        bool paused = !GetTree().Paused;
        GetTree().Paused = paused;
        Visible = paused;

        _pauseIcon.SwitchAtlas();
    }
}
