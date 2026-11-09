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

        /// <summary>
        /// 回傳每列的儲存格文字；每列欄位數相同。
        /// </summary>
        public static List<string[]> Build(IReadOnlyList<TextLine> lines)
        {
            var separators = TableColumnDetector.FindSeparators(lines);
            var columnCount = separators.Count + 1;

            var rows = lines
                .Select(line => SplitCells(line, separators, columnCount))
                .ToList();

            return RemoveEmptyColumns(rows, columnCount);
        }

        /// <summary>
        /// 依片段中心點落在哪個欄位，把片段分到儲存格。
        /// 與前一個片段緊鄰（間距小於走廊最小寬度）時沿用前一個欄位，
        /// 避免越過分界的折行項目名稱被切開（例如「原始到期日超過 3個月之」）。
        /// </summary>
        private static string[] SplitCells(TextLine line, IReadOnlyList<double> separators, int columnCount)
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
                        || TableColumnDetector.IsAttachedToNext(previous, fragment));

                if (!isContinuation)
                {
                    var center = (fragment.Bounds.Left + fragment.Bounds.Right) / 2;
                    column = separators.Count(separator => separator < center);
                }

                cells[column].Add(fragment);
                previous = fragment;
            }

            return cells.Select(cell => cell.Count == 0 ? string.Empty : TextJoiner.Join(cell)).ToArray();
        }

        private static List<string[]> RemoveEmptyColumns(List<string[]> rows, int columnCount)
        {
            var keep = Enumerable.Range(0, columnCount)
                .Where(column => rows.Any(row => row[column].Length > 0))
                .ToList();

            return rows.Select(row => keep.Select(column => row[column]).ToArray()).ToList();
        }
    }
}
