using Godot;
using Godot.Collections;
using HexGrid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public partial class Map : TileMapLayer
{

    private Array<Vector2I> _hexOffsetCoors;
    private List<AxialCoordinates> _hexAxialCoors = new List<AxialCoordinates>();
    private TileData[] _hexDates;
    private List<(int to, float weight)>[] _hexGraph;


    private TileMapLayer _mapInteraction;




    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _mapInteraction = GetNode<TileMapLayer>("MapInteraction");




        _hexOffsetCoors = GetUsedCells();

        int length = _hexOffsetCoors.Count;

        _hexDates = new TileData[length];

        _hexGraph = new List<(int to, float weight)>[length];

        for (int i = 0; i < length; i++)
        {
            var coor = _hexOffsetCoors[i];
            TileData hex = GetCellTileData(coor);
            _hexDates[i] = hex;

            var axialCoor = AxialCoordinates.OffsetToAxial(coor);
            _hexAxialCoors.Add(axialCoor);

            _hexGraph[i] = new List<(int to, float weight)>();

        }

        for (int i = 0; i < length; i++)
        {
            AxialCoordinates axialCoor = _hexAxialCoors[i];
            AxialCoordinates n1 = new AxialCoordinates(axialCoor.Q + 1, axialCoor.R);
            AxialCoordinates n2 = new AxialCoordinates(axialCoor.Q + 1, axialCoor.R - 1);
            AxialCoordinates n3 = new AxialCoordinates(axialCoor.Q, axialCoor.R - 1);
            AxialCoordinates n4 = new AxialCoordinates(axialCoor.Q - 1, axialCoor.R);
            AxialCoordinates n5 = new AxialCoordinates(axialCoor.Q - 1, axialCoor.R + 1);
            AxialCoordinates n6 = new AxialCoordinates(axialCoor.Q, axialCoor.R + 1);

            AddGraph(n1, i);
            AddGraph(n2, i);
            AddGraph(n3, i);
            AddGraph(n4, i);
            AddGraph(n5, i);
            AddGraph(n6, i);


        }

        //GD.Print($"偏移坐标：{_hexOffsetCoors[39]}，轴向坐标：{_hexAxialCoors[39]}，移动力成本：{_hexDates[39].GetCustomDataByLayerId(0)}");

    }

    public void AddGraph(AxialCoordinates n, int i)
    {

        int indexN = _hexAxialCoors.IndexOf(n);
        if (indexN >= 0)
        {
            float weightN = (float)_hexDates[indexN].GetCustomDataByLayerId(0);
            _hexGraph[i].Add((indexN, weightN));
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    
}

public class Dijkstra
{





    public static (float[] distance, int[] prev) FindShortestPaths(List<(int to, float weight)>[] graph, int start)
    {
        int n = graph.Length;
        float[] weights = new float[n];
        int[] prev = new int[n];

        System.Array.Fill(weights, int.MaxValue);
        System.Array.Fill(prev, -1);

        weights[start] = 0;

        var pq = new PriorityQueue<int, float>();
        pq.Enqueue(start, 0);

        while (pq.Count > 0)
        {
            pq.TryDequeue(out int g, out float p);

            if (p != weights[g])
            {
                continue;
            }

            foreach (var (t, w) in graph[g])
            {
                float newWeight = w + p;
                if (newWeight < weights[g])
                {
                    weights[t] = newWeight;
                    prev[t] = g;
                    pq.Enqueue(t, newWeight);
                }
            }

        }

        return (weights, prev);

    }
}
