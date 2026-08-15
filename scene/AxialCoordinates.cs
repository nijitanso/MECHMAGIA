using System;
using Godot;

namespace HexGrid
{
    /// <summary>
    /// 用于表示轴向坐标的结构体，适用于六边形网格。轴向坐标使用两个整数 Q 和 R 来表示一个六边形的位置，其中 Q 表示水平轴上的位置，R 表示斜轴上的位置。
    /// </summary>
    public struct AxialCoordinates
    {
        public int Q { get; }
        public int R { get; }
        public AxialCoordinates(int q, int r)
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
        public static AxialCoordinates OffsetToAxial(Vector2I Offset)
        {
            int parity = Offset.Y & 1; // 用按位与1来判断该行是奇数行还是偶数行
            int Q = Offset.X - (Offset.Y - parity) / 2;
            int R = Offset.Y;
            return new AxialCoordinates(Q, R);
        }

        public  Vector2I AxialToOffset()
        {
            int parity = this.R & 1; // 用按位与1来判断该行是奇数行还是偶数行
            int X = this.Q + (this.R - (this.R & 1)) / 2;
            int Y = this.R;
            return new Vector2I(X, Y);
        }

        public Vector2 ToWorldPosition(float hexSize)
        {
            float x = hexSize * (3.0f / 2.0f * Q);
            float y = hexSize * (Mathf.Sqrt(3) / 2.0f * Q + Mathf.Sqrt(3) * R);
            return new Vector2(x, y);
        }
        public static AxialCoordinates FromWorldPosition(Vector2 position, float hexSize)
        {
            float q = (2.0f / 3.0f * position.X) / hexSize;
            float r = (-1.0f / 3.0f * position.X + Mathf.Sqrt(3) / 3.0f * position.Y) / hexSize;
            return RoundToAxial(q, r);
        }
        private static AxialCoordinates RoundToAxial(float q, float r)
        {
            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);
            return new AxialCoordinates(roundedQ, roundedR);
        }
    }
}