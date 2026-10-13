using System;
using System.Collections.Generic;
using Godot;

namespace HexGrid
{
    /// <summary>
    /// 用于表示轴向坐标的结构体，适用于六边形网格。轴向坐标使用两个整数 Q 和 R 来表示一个六边形的位置，其中 Q 表示水平轴上的位置，R 表示斜轴上的位置。
    /// </summary>
    public struct AxialCoor
    {
        public int Q { get; }
        public int R { get; }
        public AxialCoor(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>
        /// 返回轴向坐标的字符串表示形式，格式为 "(Q: q, R: r)"，其中 q 和 r 分别是轴向坐标的 Q 和 R 值。这在调试和日志记录中非常有用（打印方法一般都会自动转换成字符串再输出），可以快速查看轴向坐标的值。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"(Q: {Q}, R: {R})";
        }


        /// <summary>
        /// 将偏移坐标（odd-r）转换为轴向坐标
        /// </summary>
        /// <param name="Offset"></param>
        /// <returns></returns>
        public static AxialCoor OffsetToAxial(Vector2I Offset)
        {
            int parity = Offset.Y & 1; // 用按位与1来判断该行是奇数行还是偶数行
            int Q = Offset.X - (Offset.Y - parity) / 2;
            int R = Offset.Y;
            return new AxialCoor(Q, R);
        }

        public Vector2I AxialToOffset()
        {
            int parity = this.R & 1; // 用按位与1来判断该行是奇数行还是偶数行
            int X = this.Q + (this.R - (this.R & 1)) / 2;
            int Y = this.R;
            return new Vector2I(X, Y);
        }

        /// <summary>
        /// 返回一个装有（预期）六个邻接点轴向坐标的列表
        /// </summary>
        /// <returns></returns>
        public List<AxialCoor> GetNeighborCoor(List<AxialCoor> MapScope)
        {
            List<AxialCoor> neighbors = new List<AxialCoor>(6);
            neighbors.Add((new AxialCoor(this.Q + 1, this.R)));
            neighbors.Add((new AxialCoor(this.Q + 1, this.R - 1)));
            neighbors.Add((new AxialCoor(this.Q, this.R - 1)));
            neighbors.Add((new AxialCoor(this.Q - 1, this.R)));
            neighbors.Add((new AxialCoor(this.Q - 1, this.R + 1)));
            neighbors.Add((new AxialCoor(this.Q, this.R + 1)));

            // 这个方法的作用是当筛选器（即那个传进去的委托类型的参数）返回true时，此时处理中的元素被移除序列
            neighbors.RemoveAll(x => !MapScope.Contains(x));    

            return neighbors;
        }

        public Vector2 ToWorldPosition(float hexSize)
        {
            float x = hexSize * (3.0f / 2.0f * Q);
            float y = hexSize * (Mathf.Sqrt(3) / 2.0f * Q + Mathf.Sqrt(3) * R);
            return new Vector2(x, y);
        }
        public static AxialCoor FromWorldPosition(Vector2 position, float hexSize)
        {
            float q = (2.0f / 3.0f * position.X) / hexSize;
            float r = (-1.0f / 3.0f * position.X + Mathf.Sqrt(3) / 3.0f * position.Y) / hexSize;
            return RoundToAxial(q, r);
        }
        private static AxialCoor RoundToAxial(float q, float r)
        {
            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);
            return new AxialCoor(roundedQ, roundedR);
        }
    }
}