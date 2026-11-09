using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 依完整格線切分表格：垂直線切欄；表頭依水平線分列，同一格內的多行文字合併。
    /// </summary>
    /// <remarks>
    /// 樣本的附表只有表頭與外框有水平線，資料列之間沒有，
    /// 因此資料區逐行切欄後，再以 <see cref="TableRowMerger"/> 合併折行。
    /// </remarks>
    internal static class LatticeTableBuilder
    {
        public static List<TableRow> Build(IReadOnlyList<TextLine> lines, LatticeGrid grid)
        {
            var columnCount = grid.ColumnLines.Count + 1;
            var rows = new List<TableRow>();
            var bodyStarted = false;

            foreach (var band in lines.GroupBy(line => grid.RowLines.Count(y => y < line.Bounds.CenterY)).OrderBy(g => g.Key))
            {
                var bandLines = band.ToList();
                var cells = CellsOf(bandLines, grid, columnCount);

                bodyStarted |= !TableHeaderBuilder.IsHeaderRow(ToRow(cells, keepSpanCopies: false));
                if (!bodyStarted)
                {
                    // 表頭：整個列帶為一列，跨欄格子保留在每個涵蓋的欄位（之後合併成「擔保品 名稱」）
                    rows.Add(ToRow(cells, keepSpanCopies: true));
                    continue;
                }

                var lineRows = bandLines
                    .Select(line => ToRow(CellsOf(new[] { line }, grid, columnCount), keepSpanCopies: false))
                    .Where(row => row.NonEmptyCount > 0);
                rows.AddRange(TableRowMerger.Merge(lineRows.ToList()));
            }

            return RemoveEmptyColumns(rows, columnCount);
        }

        /// <summary>
        /// 把多行文字依格線分到各欄；跨欄的格子會出現在每個涵蓋的欄位，並標記為複製。
        /// </summary>
        private static Cell[] CellsOf(IEnumerable<TextLine> lines, LatticeGrid grid, int columnCount)
        {
            var cells = Enumerable.Range(0, columnCount).Select(_ => new Cell()).ToArray();

            foreach (var line in lines)
            {
                foreach (var (fragments, firstColumn, lastColumn) in SplitByLocalLines(line, grid))
                {
                    for (var c = firstColumn; c <= lastColumn; c++)
                    {
                        cells[c].Fragments.AddRange(fragments);
                        cells[c].IsSpanCopy |= c > firstColumn;
                    }
                }
            }

            return cells;
        }

        /// <summary>
        /// 依穿過這一行的垂直線切成數段；每段對應到它涵蓋的全域欄位範圍。
        /// 表頭的跨欄格子沒有內部垂直線，因此會涵蓋多個全域欄位。
        /// </summary>
        private static IEnumerable<(List<TextFragment> Fragments, int FirstColumn, int LastColumn)> SplitByLocalLines(
            TextLine line, LatticeGrid grid)
        {
            var columns = grid.ColumnLines;
            var local = grid.ColumnLinesAt(line.Bounds.CenterY);

            return line.Fragments
                .GroupBy(f => local.Count(x => x < (f.Bounds.Left + f.Bounds.Right) / 2))
                .Select(group =>
                {
                    var index = group.Key;
                    var left = index == 0 ? double.MinValue : local[index - 1];
                    var right = index == local.Count ? double.MaxValue : local[index];

                    // 全域欄位 c 介於 columns[c-1] 與 columns[c] 之間；取完全落在這段內的欄位
                    var covered = Enumerable.Range(0, columns.Count + 1)
                        .Where(c => (c == 0 ? double.MinValue : columns[c - 1]) >= left - 1
                                 && (c == columns.Count ? double.MaxValue : columns[c]) <= right + 1)
                        .ToList();

                    if (covered.Count == 0)
                    {
                        var center = group.Average(f => (f.Bounds.Left + f.Bounds.Right) / 2);
                        covered.Add(columns.Count(x => x < center));
                    }

                    return (group.ToList(), covered.First(), covered.Last());
                });
        }

        private static TableRow ToRow(Cell[] cells, bool keepSpanCopies)
        {
            var texts = new string[cells.Length];
            var lefts = new double?[cells.Length];
            var rights = new double?[cells.Length];
            var fontSizes = new List<double>();

            for (var c = 0; c < cells.Length; c++)
            {
                var fragments = cells[c].Fragments;
                if (fragments.Count == 0 || (cells[c].IsSpanCopy && !keepSpanCopies))
                {
                    texts[c] = string.Empty;
                    continue;
                }

                texts[c] = JoinLines(fragments);
                lefts[c] = fragments.Min(f => f.Bounds.Left);
                rights[c] = fragments.Max(f => f.Bounds.Right);
                fontSizes.AddRange(fragments.Select(f => f.FontSize));
            }

            var fontSize = fontSizes.Count == 0 ? 0 : fontSizes.OrderBy(s => s).ElementAt(fontSizes.Count / 2);
            return new TableRow(texts, lefts, rights, hasColumnGap: true, isLetterSpaced: false, fontSize);
        }

        /// <summary>格內片段依視覺行重建後，上下接續成一段文字。</summary>
        private static string JoinLines(IEnumerable<TextFragment> fragments)
        {
            return LineBuilder.Build(fragments)
                .Select(line => line.Text.Trim())
                .Aggregate(string.Empty, TableRowMerger.JoinCellText);
        }

        private static List<TableRow> RemoveEmptyColumns(List<TableRow> rows, int columnCount)
        {
            var keep = Enumerable.Range(0, columnCount)
                .Where(column => rows.Any(row => !row.IsEmpty(column)))
                .ToArray();

            return rows.Select(row => row.Select(keep)).ToList();
        }

        private sealed class Cell
        {
            public List<TextFragment> Fragments { get; } = new List<TextFragment>();

            /// <summary>跨欄格子在第二個以後欄位的複製。</summary>
            public bool IsSpanCopy { get; set; }
        }
    }
}
