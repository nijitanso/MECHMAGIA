using Godot;
using System;

public partial class CanActionIcon : Sprite2D
{

    [Export] public float RotationSpeed { get; set; } = 30.0f;

    public override void _Process(double delta)
    {
        if (Visible)    // 只有在图标可见时才旋转
        {
            RotationDegrees += RotationSpeed * (float)delta; // 每秒旋转30度
        }
    }



}
