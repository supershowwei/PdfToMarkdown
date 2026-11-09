using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 視覺上的一行文字，片段已依 X 座標由左至右排序。
    /// </summary>
    internal sealed class TextLine
    {
        /// <summary>片段間距超過字級的此倍數，視為欄位分隔（表格列的特徵）。</summary>
        private const double ColumnGapRatio = 2.0;

        public TextLine(IReadOnlyList<TextFragment> fragments)
        {
            Fragments = fragments;
            Bounds = fragments.Skip(1).Aggregate(fragments[0].Bounds, (box, f) => box.Union(f.Bounds));
            Text = TextJoiner.Join(fragments);
            FontSize = Median(fragments.Select(f => f.FontSize));
            HasColumnGap = fragments.Zip(fragments.Skip(1), (left, right) => right.Bounds.Left - left.Bounds.Right)
                .Any(gap => gap > FontSize * ColumnGapRatio);
        }

        public IReadOnlyList<TextFragment> Fragments { get; }

        public Box Bounds { get; }

        public string Text { get; }

        /// <summary>行內片段字級的中位數。</summary>
        public double FontSize { get; }

        /// <summary>行內是否有明顯的欄位間距（可能是表格列）。</summary>
        public bool HasColumnGap { get; }

        public override string ToString()
        {
            return Text;
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted[sorted.Count / 2];
        }
    }
}
