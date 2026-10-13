using Godot;
using System.Collections.Generic;

public partial class MapInteraction2 : TileMapLayer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


    /// <summary>
    /// 将传入的地格坐标序列遍历，显示出绿色高光（实现方法是用一层半透明绿色瓦片蒙着）
    /// </summary>
    /// <param name="coors"></param>
	public void GreenHighlight(List<Vector2I> coors)
	{
        foreach (var coor in coors)
        {
            SetCell(coor, 0, new Vector2I(0, 0));
        }
        
    }

    /// <summary>
    /// 将传入的地格坐标序列遍历，移除该坐标上的绿色高亮
    /// </summary>
    /// <param name="coors"></param>
    public void RemoveGreenHighlight(List<Vector2I> coors)
    {
        foreach (var coor in coors)
        {
            EraseCell(coor);
        }
        
    }



}
