using ActionProcessor;
using Godot;
using System;
using static System.Net.Mime.MediaTypeNames;

public partial class PanelContainer1 : PanelContainer
{
    private AttackMessage _attackMessage;

    private Tween _tween;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        _attackMessage = FindChild("AttackMessage") as AttackMessage;
        Modulate = new Color(0, 0, 0, 0);

        AttackProcessor.Inst.Attack += Display;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public string GetText()
    {
        int ratio = AttackProcessor.Inst.Ratio;
        int dice = AttackProcessor.Inst.Dice;
        string ratioText;

        if (ratio == 0)
        {
            ratioText = "1/2";
        }
        else
        {
            ratioText = $"{ratio}/1";
        }


        return $"进攻：点数比为{ratioText}，骰子点数为{dice}，战斗结果为{AttackProcessor.Inst.CR}";


    }

    public void Display()
    {
        // 每当新显示使，杀掉原来的补间实例，重置淡出效果
        if (_tween != null)
        {
            _tween.Kill();
        }

        _attackMessage.Text = GetText();
        Modulate = new Color(1, 1, 1, 1);

        // 创建补间实例，实现动画效果
        _tween = GetTree().CreateTween();
        _tween.TweenInterval(2.0);  // 让信息显示两秒再淡出
        // 设置淡出效果（本质上是在规定的时间段内将modulate属性动态调整到透明度为0）
        _tween.TweenProperty(this, "modulate", new Color(0, 0, 0, 0), 0.5);
    }
}
