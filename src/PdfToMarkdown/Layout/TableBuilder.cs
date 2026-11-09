using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 將表格的行切成儲存格。
    /// </summary>
    internal static class TableBuilder
    {
        /// <summary>片段間距小於字級的此倍數視為同一格（需小於欄位走廊的最小寬度）。</summary>
        private const double ContinuationGapRatio = 0.4;

        /// <summary>分散對齊文字的字距至少為字級的此倍數。</summary>
        private const double LetterSpacedMinGapRatio = 0.5;

        /// <summary>分散對齊文字的最大字距不超過最小字距的此倍數。</summary>
        private const double LetterSpacedMaxGapSpread = 2.0;

        /// <summary>
        /// 回傳每列的儲存格；每列欄位數相同。一行文字對應一列，尚未合併折行。
        /// </summary>
        /// <param name="lines">表格範圍內的行。</param>
        /// <param name="rulings">表格範圍內的格線；會計格式的欄位底線可補強欄位分界。</param>
        public static List<TableRow> Build(IReadOnlyList<TextLine> lines, IReadOnlyList<Box> rulings = null)
        {
            rulings = rulings ?? new Box[0];
            var separators = TableColumnDetector.Find(lines, rulings);
            var rows = Split(lines, separators);

            // 跨欄的表頭（例如「115年7月1日至9月30日」橫跨三個子欄）會擋住子欄之間的走廊：
            // 排除表頭後重算，若能切出更多欄位就採用
            var headerCount = rows.TakeWhile(TableHeaderBuilder.IsHeaderRow).Count();
            if (headerCount > 0 && headerCount < lines.Count)
            {
                var bodySeparators = TableColumnDetector.Find(lines.Skip(headerCount).ToList(), rulings);
                if (bodySeparators.All.Count > separators.All.Count)
                {
                    rows = Split(lines, bodySeparators);
                }
            }

            return rows;
        }

        private static List<TableRow> Split(IReadOnlyList<TextLine> lines, ColumnSeparators separators)
        {
            var columnCount = separators.All.Count + 1;
            var rows = lines
                .Select(line => SplitCells(line, separators.All, separators.Hard, columnCount))
                .ToList();

            return RemoveEmptyColumns(rows, columnCount);
        }

        /// <summary>
        /// 依片段中心點落在哪個欄位，把片段分到儲存格。
        /// 與前一個片段緊鄰（間距小於走廊最小寬度）時沿用前一個欄位，
        /// 避免越過分界的折行項目名稱被切開（例如「原始到期日超過 3個月之」）；
        /// 但不越過由格線得到的分界。
        /// </summary>
        private static TableRow SplitCells(TextLine line, IReadOnlyList<double> separators, IReadOnlyList<double> hardSeparators, int columnCount)
        {
            var cells = new List<TextFragment>[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                cells[i] = new List<TextFragment>();
            }

            TextFragment previous = null;
            var column = 0;

            foreach (var fragment in line.Fragments)
            {
                var isContinuation = previous != null
                    && (fragment.Bounds.Left - previous.Bounds.Right < fragment.FontSize * ContinuationGapRatio
                        || TableColumnDetector.IsAttachedToNext(previous, fragment))
                    && !hardSeparators.Any(x => x > previous.Bounds.Right - 1 && x < fragment.Bounds.Left + 1);

                if (!isContinuation)
                {
                    var center = (fragment.Bounds.Left + fragment.Bounds.Right) / 2;
                    column = separators.Count(separator => separator < center);
                }

                cells[column].Add(fragment);
                previous = fragment;
            }

            return new TableRow(
                cells.Select(cell => cell.Count == 0 ? string.Empty : TextJoiner.Join(cell)).ToArray(),
                cells.Select(cell => cell.Count == 0 ? (double?)null : cell[0].Bounds.Left).ToArray(),
                cells.Select(cell => cell.Count == 0 ? (double?)null : cell[cell.Count - 1].Bounds.Right).ToArray(),
                line.HasColumnGap,
                IsLetterSpaced(line),
                line.FontSize);
        }

        /// <summary>
        /// 整行是一段分散對齊的文字：每個字之間都有明顯且大致相等的間距。
        /// </summary>
        private static bool IsLetterSpaced(TextLine line)
        {
            if (line.Fragments.Count < 3)
            {
                return false;
            }

            var gaps = line.Fragments.Zip(line.Fragments.Skip(1), (a, b) => b.Bounds.Left - a.Bounds.Right).ToList();
            var min = gaps.Min();
            return min >= line.FontSize * LetterSpacedMinGapRatio && gaps.Max() <= min * LetterSpacedMaxGapSpread;
        }

        private static List<TableRow> RemoveEmptyColumns(List<TableRow> rows, int columnCount)
        {
            var keep = Enumerable.Range(0, columnCount)
                .Where(column => rows.Any(row => !row.IsEmpty(column)))
                .ToArray();

            return rows.Select(row => row.Select(keep)).ToList();
        }
    }
}
