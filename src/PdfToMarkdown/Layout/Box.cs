using System;
using System.Globalization;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 矩形範圍。座標原點在頁面左上角，Y 軸向下（與影像座標一致，方便 OCR 共用）。
    /// </summary>
    internal readonly struct Box
    {
        public Box(double left, double top, double right, double bottom)
        {
            Left = Math.Min(left, right);
            Right = Math.Max(left, right);
            Top = Math.Min(top, bottom);
            Bottom = Math.Max(top, bottom);
        }

        public double Left { get; }

        public double Top { get; }

        public double Right { get; }

        public double Bottom { get; }

        public double Width => Right - Left;

        public double Height => Bottom - Top;

        public double CenterY => (Top + Bottom) / 2;

        /// <summary>兩個範圍在垂直方向重疊的長度（不重疊為 0）。</summary>
        public double VerticalOverlap(Box other)
        {
            return Math.Max(0, Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top));
        }

        public Box Union(Box other)
        {
            return new Box(
                Math.Min(Left, other.Left),
                Math.Min(Top, other.Top),
                Math.Max(Right, other.Right),
                Math.Max(Bottom, other.Bottom));
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "[{0:0.#},{1:0.#} - {2:0.#},{3:0.#}]", Left, Top, Right, Bottom);
        }
    }
}
