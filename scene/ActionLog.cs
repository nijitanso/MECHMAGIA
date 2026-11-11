using Godot;
using System;
using LG = Generator.LogGenerator;

public partial class ActionLog : RichTextLabel
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        LG.ActionLog = this;    // 向 LogGenerator 注册 ActionLog 实例，方便在其他地方调用 AddLog 方法
		BbcodeEnabled = true;


    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	public void AddLog(string log, LG.LogType logType)
    {
        AppendText(log + "\n");
        ScrollToLine(this.GetLineCount() - 1);
    }

}
