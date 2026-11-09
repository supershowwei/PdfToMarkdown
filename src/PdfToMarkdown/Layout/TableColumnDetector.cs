using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 以「垂直空白走廊」切分表格欄位：所有資料列在某段 X 範圍內都沒有文字，該處即為欄位分隔。
    /// </summary>
    internal static class TableColumnDetector
    {
        /// <summary>X 軸取樣解析度（點）。</summary>
        private const double Resolution = 0.5;

        /// <summary>
        /// 走廊寬度至少為字級的此倍數：中文字距約 0～0.2 個字寬，
        /// 太窄的空白可能只是兩列剛好對齊的字距。
        /// </summary>
        private const double MinCorridorRatio = 0.5;

        /// <summary>
        /// 回傳各欄位的分界 X 座標（不含最左與最右）。欄位數為分界數 + 1。
        /// </summary>
        /// <remarks>
        /// 只用有欄位間距的行計算：跨欄的表頭與折行的項目名稱通常沒有欄位間距，
        /// 若一併計算會把走廊填滿。
        /// </remarks>
        public static IReadOnlyList<double> FindSeparators(IReadOnlyList<TextLine> lines)
        {
            var bodyLines = lines.Where(line => line.HasColumnGap).ToList();
            if (bodyLines.Count == 0)
            {
                bodyLines = lines.ToList();
            }

            var fragments = bodyLines.SelectMany(line => line.Fragments).ToList();
            var left = fragments.Min(f => f.Bounds.Left);
            var right = fragments.Max(f => f.Bounds.Right);
            var fontSize = Median(fragments.Select(f => f.FontSize));

            var slots = (int)Math.Ceiling((right - left) / Resolution) + 1;
            var covered = new bool[slots];

            void Cover(double from, double to)
            {
                var start = (int)Math.Floor((from - left) / Resolution);
                var end = (int)Math.Ceiling((to - left) / Resolution);
                for (var i = Math.Max(0, start); i < Math.Min(slots, end); i++)
                {
                    covered[i] = true;
                }
            }

            foreach (var line in bodyLines)
            {
                for (var i = 0; i < line.Fragments.Count; i++)
                {
                    var fragment = line.Fragments[i];
                    Cover(fragment.Bounds.Left, fragment.Bounds.Right);

                    // 會計格式的「$」「(」靠左、數字靠右，中間的空白屬於同一格
                    if (i + 1 < line.Fragments.Count && IsAttachedToNext(fragment, line.Fragments[i + 1]))
                    {
                        Cover(fragment.Bounds.Right, line.Fragments[i + 1].Bounds.Left);
                    }
                }
            }

            var separators = new List<double>();
            var minSlots = fontSize * MinCorridorRatio / Resolution;
            var runStart = -1;

            for (var i = 0; i <= slots; i++)
            {
                var isGap = i < slots && !covered[i];
                if (isGap && runStart < 0)
                {
                    runStart = i;
                }
                else if (!isGap && runStart >= 0)
                {
                    if (i - runStart >= minSlots)
                    {
                        separators.Add(left + (runStart + i) / 2.0 * Resolution);
                    }

                    runStart = -1;
                }
            }

            return separators;
        }

        /// <summary>
        /// 左側片段是貨幣符號或左括號、或右側片段是右括號時，兩者之間的空白屬於同一格。
        /// </summary>
        public static bool IsAttachedToNext(TextFragment fragment, TextFragment next)
        {
            return fragment.Text == "$" || fragment.Text == "(" || fragment.Text == "（"
                || next.Text == ")" || next.Text == "）";
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            return sorted[sorted.Count / 2];
        }
    }
}
