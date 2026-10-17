using Godot;
using System;

public partial class AttackButton : ButtonBase
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

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

	
}
