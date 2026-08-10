using Godot;
using System;

public partial class Camera2d : Camera2D
{
    public Vector2 preciousMousePosition { get; set; }
    [Export] public float PanSpeed { get; set; } = 1.0f;
    [Export] public float ZoomSpeed { get; set; } = 0.3f;
    [Export] public Vector2 MinZoom { get; set; } = new Vector2(0.6f, 0.6f);
    [Export] public Vector2 MaxZoom { get; set; } = new Vector2(4f, 4f);


    private Vector2 targetZoom;
    private Vector2 deltaMousePosition;
    private Vector2 pMousePosition;
    private bool isWheeling = false;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        preciousMousePosition = GetGlobalMousePosition();
        targetZoom = Zoom;
        pMousePosition = GetLocalMousePosition();


    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
    {
        Vector2 currentMousePosition = GetGlobalMousePosition();
        float deltaX = currentMousePosition.X - preciousMousePosition.X;
        float deltaY = currentMousePosition.Y - preciousMousePosition.Y;

        long clickedMouseButton = (long)(Input.GetMouseButtonMask());

        if (clickedMouseButton == 4)
        {
            Vector2 position = Position;
            position.X -= PanSpeed * deltaX;
            position.Y -= PanSpeed * deltaY;
            Position = position;
        }

        if (clickedMouseButton == 0)
        {
            preciousMousePosition = currentMousePosition;
        }


        
        Zoom = Zoom.Lerp(targetZoom, 10.0f * (float)delta); // 让缩放平滑过渡到目标缩放值
        if (AreEqualUpToTwoDecimals(Zoom.X, targetZoom.X) && isWheeling)
        {
            Vector2 cMousePosition = GetLocalMousePosition();
            deltaMousePosition = cMousePosition - pMousePosition;
            Position -= deltaMousePosition;
            
        }
        GD.Print($"实际：{Zoom}");
        GD.Print($"目标：{targetZoom}");
        //GD.Print($"绝对位置：{GetGlobalMousePosition()}");
        //GD.Print($"摄像机位置：{Position}");
        //GD.Print($"相对位置：{GetLocalMousePosition()}");
    }

    /// <summary>
    /// 这个方法是Godot专门用来处理键盘、鼠标、手柄输入的，在接收到操作系统的控件输入后会被调用，所有专门的硬件输入处理应当放在这个方法中实现以提高性能
    /// </summary>
    /// <param name="event"></param>
    public override void _Input(InputEvent @event)
    {
        //  这个if语句的条件是C#一个比较新的语法，叫做模式匹配（Pattern Matching），它的意思是如果 @event 是 InputEventMouseButton 类型的，返回true，并把它转换成 mouseEvent 变量，这样就可以直接使用 mouseEvent 的属性和方法了
        if (@event is InputEventMouseButton mouseEvent)
        {
            //  mouseEvent类的 ButtonIndex 属性是一个枚举类型，表示鼠标按键的索引，WheelUp 表示鼠标滚轮向上滚动，WheelDown 表示鼠标滚轮向下滚动，Pressed 属性是一个布尔类型，表示鼠标按键是否被按下
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed)
            {
                isWheeling = true;
                pMousePosition = GetLocalMousePosition();
                //GD.Print($"旧鼠标位置：{pMousePosition}");

                //  此处并不获取滚轮的滚动量，而是用一个固定的缩放速度来调整每次鼠标滚轮时zoom的变化量（因为_Input方法是每滚一次就调用一次）
                //Zoom += new Vector2(ZoomSpeed, ZoomSpeed); // 放大
                targetZoom = Zoom * (1.0f + ZoomSpeed);
                //Position = GetGlobalMousePosition() - (pMousePosition * targetZoom);
                
                
                ////FollowingMouse(); // 让摄像机跟随鼠标位置
                //Vector2 cMousePosition = GetLocalMousePosition();
                ////GD.Print($"新鼠标位置：{cMousePosition}");
                //Vector2 deltaMousePosition = cMousePosition - pMousePosition;
                //Position -= deltaMousePosition; // 让摄像机跟随鼠标位置
                ////GD.Print($"摄像机位置：{Position}");


            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
            {
                targetZoom = Zoom * (1.0f - ZoomSpeed); // 缩小
            }
        }
        else isWheeling = false;

        Zoom = new Vector2(Math.Clamp(Zoom.X, MinZoom.X, MaxZoom.X), Math.Clamp(Zoom.Y, MinZoom.Y, MaxZoom.Y));     //  限制缩放范围



    }

    private void FollowingMouse()
    {
        Vector2 mousePosition = GetGlobalMousePosition();
        Vector2 cameraPosition = Position;
        float VX = mousePosition.X - cameraPosition.X;
        float VY = mousePosition.Y - cameraPosition.Y;
        Vector2 directionVector = new Vector2(VX, VY);
        Vector2 UnitDirectionVector = directionVector.Normalized();
        Position += directionVector * 0.25f;
    }

    private void FollowingMouse2()
    {
        Vector2 mousePosition = GetLocalMousePosition();
        Vector2 cameraPosition = Position;
        float VX = mousePosition.X - cameraPosition.X;
        float VY = mousePosition.Y - cameraPosition.Y;
        Vector2 directionVector = new Vector2(VX, VY);
        Vector2 UnitDirectionVector = directionVector.Normalized();
        Position += directionVector * 0.25f;
    }

    public static bool AreEqualUpToTwoDecimals(float a, float b)
    {
        // 指定 MidpointRounding.AwayFromZero 使 .005 向上舍入，更符合日常习惯
        return Math.Round(a, 2, MidpointRounding.AwayFromZero)
            == Math.Round(b, 2, MidpointRounding.AwayFromZero);
    }
}
