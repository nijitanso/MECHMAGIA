using Godot;
using System;

public partial class Camera2d : Camera2D
{
    [Export] public float PanSpeed { get; set; } = 1.0f;
    [Export] public float ZoomSpeed { get; set; } = 0.25f;
    [Export] public Vector2 MinZoom { get; set; } = new Vector2(0.6f, 0.6f);
    [Export] public Vector2 MaxZoom { get; set; } = new Vector2(4f, 4f);
    [Export] public float EdgePanSpeed { get; set; } = 4.0f;

    private Vector2 preciousMousePosition;
    private Vector2 newZoom;    //  这两个“new”字段用于存储缩放和平移的目标值，来实现平滑过渡效果（用Lerp方法）
    private Vector2 newPosition;
    private Viewport viwport;
    private Vector2 pViewportSize;
    private float cameraSizeX;
    private float cameraSizeY;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        preciousMousePosition = GetGlobalMousePosition();
        newZoom = Zoom;
        newPosition = Position;

        //  获取视口的大小，用于计算摄像机的大小并实现边缘移动功能
        viwport = GetViewport();
        pViewportSize = viwport.GetVisibleRect().Size;

        float viewportSizeX = pViewportSize.X;
        float viewportSizeY = pViewportSize.Y;
        cameraSizeX = viewportSizeX / 2.0f / Zoom.X;
        cameraSizeY = viewportSizeY / 2.0f / Zoom.Y;

        GD.Print($"摄像机大小：{cameraSizeX}, {cameraSizeY}");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
    {

        Vector2 viewportSize = viwport.GetVisibleRect().Size;

        //  如果视口大小发生变化或者缩放发生变化，则重新计算摄像机的大小（计算要用的理论值）
        if (viewportSize != pViewportSize || Zoom != newZoom)
        {
            float viewportSizeX = viewportSize.X;
            float viewportSizeY = viewportSize.Y;

            cameraSizeX = viewportSizeX / 2.0f / Zoom.X;
            cameraSizeY = viewportSizeY / 2.0f / Zoom.Y;

            pViewportSize = viewportSize;
        }


        //  这段代码实现了中建平移摄像机的功能，原理是获取鼠标当前位置和上一次鼠标位置的差值，然后根据这个差值来调整摄像机的位置，从而实现平移效果
        Vector2 currentMousePosition = GetGlobalMousePosition();
        float deltaX = currentMousePosition.X - preciousMousePosition.X;
        float deltaY = currentMousePosition.Y - preciousMousePosition.Y;

        long clickedMouseButton = (long)(Input.GetMouseButtonMask());

        if (clickedMouseButton == 4)
        {
            //  这里先用临时的position变量来存储摄像机的位置，然后根据鼠标的移动量来调整摄像机的位置，最后再把这个临时变量赋值给摄像机的Position属性，这样可以避免直接修改Position属性导致的性能问题
            Vector2 position = Position;
            position.X -= PanSpeed * deltaX;
            position.Y -= PanSpeed * deltaY;



            Position = position;
            newPosition = position;
        }

        //  如果中建松开则更新preciousMousePosition为当前鼠标位置，这样下次按下中建时就可以正确计算鼠标的移动量
        if (clickedMouseButton == 0)
        {
            preciousMousePosition = currentMousePosition;
        }

        EdgeMove(currentMousePosition);

        // 让缩放和平移平滑过渡
        Zoom = Zoom.Lerp(newZoom, 8.0f * (float)delta); 
        Position = Position.Lerp(newPosition, 8.0f * (float)delta);

        //  限制缩放范围，必须在Process方法中钳制范围是因为平滑移动
        Zoom = new Vector2(Math.Clamp(Zoom.X, MinZoom.X, MaxZoom.X), Math.Clamp(Zoom.Y, MinZoom.Y, MaxZoom.Y)); 

    }

    /// <summary>
    /// 这个方法是Godot专门用来处理键盘、鼠标、手柄等输入的，在接收到操作系统的控件输入后会被调用，所有专门的硬件输入处理应当放在这个方法中实现以提高性能
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
                Vector2 oldZoom = newZoom;
                //  此处并不获取滚轮的滚动量，而是用一个固定的缩放速度来调整每次鼠标滚轮时zoom的变化量，做一个累乘会使每次缩放的值均匀一些（因为_Input方法是每滚一次就调用一次）
                newZoom = Zoom * (1.0f + ZoomSpeed); // 放大

                Vector2 pMousePosition = GetLocalMousePosition();
                //  计算缩放时鼠标位置的变化量，来调整摄像机的位置，使得缩放时鼠标位置不发生偏移
                Vector2 cMousePosition = pMousePosition * (oldZoom / newZoom);

                //  对摄像机位置进行补偿
                Vector2 deltaMousePosition = cMousePosition - pMousePosition;
                newPosition -= deltaMousePosition;


            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
            {
                //  缩小时不用跟踪鼠标，模仿欧陆风云4的手感
                newZoom = Zoom * (1.0f - ZoomSpeed); // 缩小
            }
        }




    }

    /// <summary>
    /// 实现边缘移动摄像机的功能，当鼠标靠近屏幕边缘时，摄像机会自动向该方向移动，同时也支持使用键盘的WASD或方向键来控制摄像机移动
    /// </summary>
    /// <param name="mousePositon"></param>
    private void EdgeMove(Vector2 mousePositon)
    {
        //  记住相对位置是上负下正
        float cameraLeftEdge = Position.X - cameraSizeX;
        float cameraRightEdge = Position.X + cameraSizeX;
        float cameraTopEdge = Position.Y - cameraSizeY;
        float cameraDownEdge = Position.Y + cameraSizeY;

        //  这里直接用按键的来判断的方法可能会在以后界面多起来后有冲突，判断起来可能会很麻烦，后续也许需要重构一下
        if (mousePositon.X <= cameraLeftEdge + 5 || Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            newPosition -= new Vector2(EdgePanSpeed / Zoom.X, 0);
        }

        if (mousePositon.X >= cameraRightEdge - 5 || Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            newPosition += new Vector2(EdgePanSpeed / Zoom.X, 0);
        }

        if (mousePositon.Y <= cameraTopEdge + 5 || Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            newPosition -= new Vector2(0, EdgePanSpeed / Zoom.X);
        }

        if (mousePositon.Y >= cameraDownEdge - 5 || Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            newPosition += new Vector2(0, EdgePanSpeed / Zoom.X);
        }

    }

}
