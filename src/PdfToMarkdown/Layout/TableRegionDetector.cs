using System;
using System.Collections.Generic;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 找出頁面中的表格範圍（連續的行）。
    /// </summary>
    /// <remarks>
    /// 樣本中的表格沒有直線框線，因此以文字排列判斷：
    /// 表格列有明顯的欄位間距，且列距比內文行距密（約 1.3 倍字級，內文約 2 倍）。
    /// </remarks>
    internal static class TableRegionDetector
    {
        /// <summary>相鄰兩行的上緣距離不超過字級的此倍數，才視為同一個表格。</summary>
        private const double MaxRowStepRatio = 1.5;

        /// <summary>表格至少要有這麼多行具有欄位間距。</summary>
        private const int MinColumnGapLines = 2;

        /// <summary>只有一行具有欄位間距時，表格至少要有這麼多行（例如續頁只剩一列數字的表格）。</summary>
        private const int MinLinesWithSingleColumnGap = 3;

        /// <summary>沒有欄位間距、至少這麼多字且含句子標點的行視為內文，不納入表格。</summary>
        private const int MinProseFragments = 20;

        /// <summary>表格內的行與相鄰表格列字級差異超過此值，視為不同區塊。</summary>
        private const double MaxFontSizeDifference = 1.0;

        /// <summary>
        /// 回傳每個表格的起訖行索引（含）。
        /// </summary>
        public static IReadOnlyList<(int Start, int End)> Find(IReadOnlyList<TextLine> lines)
        {
            var regions = new List<(int Start, int End)>();
            var i = 0;

            while (i < lines.Count)
            {
                if (!lines[i].HasColumnGap)
                {
                    i++;
                    continue;
                }

                var start = i;
                var end = i;
                var columnGapLines = 1;

                // 向下延伸
                while (end + 1 < lines.Count && BelongsTogether(lines[end], lines[end + 1]))
                {
                    end++;
                    if (lines[end].HasColumnGap)
                    {
                        columnGapLines++;
                    }
                }

                // 向上延伸（表頭常沒有明顯的欄位間距）
                var floor = regions.Count > 0 ? regions[regions.Count - 1].End + 1 : 0;
                while (start - 1 >= floor && BelongsTogether(lines[start - 1], lines[start]))
                {
                    start--;
                }

                if (columnGapLines >= MinColumnGapLines || end - start + 1 >= MinLinesWithSingleColumnGap)
                {
                    regions.Add((start, end));
                }

                i = end + 1;
            }

            return regions;
        }

        /// <summary>
        /// 判斷相鄰的上下兩行是否屬於同一個表格。
        /// </summary>
        private static bool BelongsTogether(TextLine upper, TextLine lower)
        {
            var fontSize = Math.Max(upper.FontSize, lower.FontSize);
            var step = lower.Bounds.Top - upper.Bounds.Top;

            return step <= fontSize * MaxRowStepRatio
                && Math.Abs(upper.FontSize - lower.FontSize) <= MaxFontSizeDifference
                && !IsProse(upper)
                && !IsProse(lower);
        }

        /// <summary>
        /// 內文行：沒有欄位間距、夠長，且含有句子標點。
        /// 表頭（例如「115年3月31日 114年12月31日」）與項目名稱通常沒有句子標點。
        /// </summary>
        private static bool IsProse(TextLine line)
        {
            return !line.HasColumnGap
                && line.Fragments.Count >= MinProseFragments
                && line.Text.IndexOfAny(SentencePunctuation) >= 0;
        }

        /// <summary>只用全形標點：半形逗號會出現在金額的千分位。</summary>
        private static readonly char[] SentencePunctuation = { '。', '，', '；' };
    }
}
