using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 切分表格欄位。
    /// </summary>
    /// <remarks>
    /// 主要依「垂直空白走廊」：所有資料列在某段 X 範圍內都沒有文字，該處即為欄位分隔。
    /// 會計格式的欄位底線可補強走廊太窄（相鄰兩格幾乎相連）而找不到的分界。
    /// </remarks>
    internal static class TableColumnDetector
    {
        /// <summary>X 軸取樣解析度（點）。</summary>
        private const double Resolution = 0.5;

        /// <summary>
        /// 走廊寬度至少為字級的此倍數：中文字距約 0～0.2 個字寬，
        /// 太窄的空白可能只是兩列剛好對齊的字距。
        /// </summary>
        private const double MinCorridorRatio = 0.5;

        /// <summary>相鄰底線之間的空隙至少這麼寬（點）才視為欄位分界；完整格線表格的線段接縫約 0.5 點。</summary>
        private const double MinUnderlineGap = 1.0;

        /// <summary>
        /// 相鄰底線之間的空隙最多為字級的此倍數；更寬的空隙（例如兩欄合計線之間隔著其他欄）不是欄位分界。
        /// </summary>
        private const double MaxUnderlineGapRatio = 2.0;

        /// <summary>
        /// 分界處的字距與左右兩側字距相差在字級的此倍數內，視為切進同一個分散對齊的詞。
        /// </summary>
        private const double LetterSpacingTolerance = 0.2;

        /// <summary>
        /// 回傳欄位分界 X 座標（由小到大，不含最左與最右）；欄位數為分界數 + 1。
        /// <see cref="ColumnSeparators.Hard"/> 是由底線得到、走廊找不到的分界，切分時不可跨越。
        /// </summary>
        /// <remarks>
        /// 只用有欄位間距的行計算：跨欄的表頭與折行的項目名稱通常沒有欄位間距，
        /// 若一併計算會把走廊填滿。
        /// </remarks>
        public static ColumnSeparators Find(IReadOnlyList<TextLine> lines, IEnumerable<Box> rulings)
        {
            var bodyLines = lines.Where(line => line.HasColumnGap).ToList();
            if (bodyLines.Count == 0)
            {
                bodyLines = lines.ToList();
            }

            var coverage = new Coverage(bodyLines);
            var fontSize = Median(bodyLines.SelectMany(line => line.Fragments).Select(f => f.FontSize));
            var corridors = coverage.FindGaps(fontSize * MinCorridorRatio);

            var hard = FindUnderlineSeparators(rulings, fontSize * MaxUnderlineGapRatio)
                .Where(x => !corridors.Any(c => x >= c.Start - fontSize * MinCorridorRatio && x <= c.End + fontSize * MinCorridorRatio))
                .Where(x => !coverage.IsAttachedGap(x))
                .Where(x => !bodyLines.Any(line => CutsLetterSpacedWord(line, x, fontSize)))
                .Distinct()
                .ToList();

            var all = corridors.Select(c => (c.Start + c.End) / 2).Concat(hard).OrderBy(x => x).ToList();
            return new ColumnSeparators(all, hard);
        }

        /// <summary>
        /// 左側片段是貨幣符號或左括號、或右側片段是右括號時，兩者之間的空白屬於同一格。
        /// </summary>
        public static bool IsAttachedToNext(TextFragment fragment, TextFragment next)
        {
            return fragment.Text == "$" || fragment.Text == "(" || fragment.Text == "（"
                || next.Text == ")" || next.Text == "）";
        }

        /// <summary>
        /// 會計格式的欄位底線：同一高度有多段底線時，相鄰兩段之間的空隙中點。
        /// </summary>
        private static IEnumerable<double> FindUnderlineSeparators(IEnumerable<Box> rulings, double maxGap)
        {
            return rulings
                .Where(r => r.Width > r.Height)
                .GroupBy(r => Math.Round(r.CenterY))
                .Where(group => group.Count() >= 2)
                .SelectMany(group =>
                {
                    var segments = group.OrderBy(r => r.Left).ToList();
                    return segments.Zip(segments.Skip(1), (a, b) => (Gap: b.Left - a.Right, Middle: (a.Right + b.Left) / 2))
                        .Where(pair => pair.Gap >= MinUnderlineGap && pair.Gap <= maxGap)
                        .Select(pair => Math.Round(pair.Middle, 1));
                });
        }

        /// <summary>
        /// x 是否切進一個分散對齊的詞：x 兩側的字距與左右各自相鄰的字距幾乎相同，
        /// 例如底線空隙落在「新 台 幣」的「新」與「台」之間。
        /// </summary>
        private static bool CutsLetterSpacedWord(TextLine line, double x, double fontSize)
        {
            var fragments = line.Fragments;
            var right = 0;
            while (right < fragments.Count && fragments[right].Bounds.Left < x)
            {
                right++;
            }

            var left = right - 1;
            if (left < 0 || right >= fragments.Count || fragments[left].Bounds.Right > x)
            {
                return false;
            }

            double Gap(int i) => fragments[i + 1].Bounds.Left - fragments[i].Bounds.Right;
            var tolerance = fontSize * LetterSpacingTolerance;
            var gap = Gap(left);

            // 與兩側（存在者）的字距比較；兩側都沒有字可比時無法判斷
            var hasLeft = left >= 1;
            var hasRight = right + 1 < fragments.Count;
            return (hasLeft || hasRight)
                && (!hasLeft || Math.Abs(gap - Gap(left - 1)) <= tolerance)
                && (!hasRight || Math.Abs(gap - Gap(right)) <= tolerance);
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted[sorted.Count / 2];
        }

        /// <summary>
        /// 各列文字在 X 軸上的覆蓋範圍。
        /// </summary>
        private sealed class Coverage
        {
            private readonly double _left;
            private readonly bool[] _covered;
            private readonly bool[] _attachedGap;

            public Coverage(IReadOnlyList<TextLine> lines)
            {
                var fragments = lines.SelectMany(line => line.Fragments).ToList();
                _left = fragments.Min(f => f.Bounds.Left);
                var right = fragments.Max(f => f.Bounds.Right);
                _covered = new bool[(int)Math.Ceiling((right - _left) / Resolution) + 1];
                _attachedGap = new bool[_covered.Length];

                foreach (var line in lines)
                {
                    for (var i = 0; i < line.Fragments.Count; i++)
                    {
                        var fragment = line.Fragments[i];
                        Mark(_covered, fragment.Bounds.Left, fragment.Bounds.Right);

                        // 會計格式的「$」「(」靠左、數字靠右，中間的空白屬於同一格
                        if (i + 1 < line.Fragments.Count && IsAttachedToNext(fragment, line.Fragments[i + 1]))
                        {
                            Mark(_covered, fragment.Bounds.Right, line.Fragments[i + 1].Bounds.Left);
                            Mark(_attachedGap, fragment.Bounds.Right, line.Fragments[i + 1].Bounds.Left);
                        }
                    }
                }
            }

            /// <summary>x 是否落在「$」「(」與數字之間的空白（屬於同一格）。</summary>
            public bool IsAttachedGap(double x)
            {
                var slot = (int)Math.Floor((x - _left) / Resolution);
                return slot >= 0 && slot < _attachedGap.Length && _attachedGap[slot];
            }

            /// <summary>寬度至少 <paramref name="minWidth"/> 的未覆蓋範圍（不含最左與最右之外）。</summary>
            public List<(double Start, double End)> FindGaps(double minWidth)
            {
                var gaps = new List<(double Start, double End)>();
                var runStart = -1;

                for (var i = 0; i <= _covered.Length; i++)
                {
                    var isGap = i < _covered.Length && !_covered[i];
                    if (isGap && runStart < 0)
                    {
                        runStart = i;
                    }
                    else if (!isGap && runStart >= 0)
                    {
                        if ((i - runStart) * Resolution >= minWidth)
                        {
                            gaps.Add((_left + runStart * Resolution, _left + i * Resolution));
                        }

                        runStart = -1;
                    }
                }

                return gaps;
            }

            private void Mark(bool[] slots, double from, double to)
            {
                var start = (int)Math.Floor((from - _left) / Resolution);
                var end = (int)Math.Ceiling((to - _left) / Resolution);
                for (var i = Math.Max(0, start); i < Math.Min(slots.Length, end); i++)
                {
                    slots[i] = true;
                }
            }
        }
    }

    /// <summary>
    /// 欄位分界。
    /// </summary>
    internal sealed class ColumnSeparators
    {
        public ColumnSeparators(IReadOnlyList<double> all, IReadOnlyList<double> hard)
        {
            All = all;
            Hard = hard;
        }

        /// <summary>所有分界，由小到大。</summary>
        public IReadOnlyList<double> All { get; }

        /// <summary>由格線得到的分界，切分儲存格時不可跨越。</summary>
        public IReadOnlyList<double> Hard { get; }
    }
}
