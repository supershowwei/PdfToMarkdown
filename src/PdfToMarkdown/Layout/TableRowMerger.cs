using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 合併表格中因儲存格折行而拆成多列的資料。
    /// </summary>
    /// <remarks>
    /// 第一欄（項目名稱）的折行會凸排約 1 個字寬，下層項目則縮排約 2 個字寬，
    /// 以此區分「同一項目的折行」與「新的子項目」。
    /// </remarks>
    internal static class TableRowMerger
    {
        /// <summary>第一欄折行相對上一列的最小縮排（字寬倍數）。</summary>
        private const double MinWrapIndentRatio = 0.4;

        /// <summary>第一欄折行相對上一列的最大縮排（字寬倍數）；超過視為子項目。</summary>
        private const double MaxWrapIndentRatio = 1.6;

        /// <summary>上一列以編號開頭時（例如目錄的「(五)」），折行對齊編號後文字，允許較大縮排。</summary>
        private const double MaxHangingIndentRatio = 4.0;

        private static readonly Regex _numbered = new Regex(
            @"^([（(][一二三四五六七八九十百\d]+[)）]|[一二三四五六七八九十百]+、|\d+[.、])",
            RegexOptions.Compiled);

        public static List<TableRow> Merge(IReadOnlyList<TableRow> rows)
        {
            var result = new List<TableRow>();

            foreach (var row in rows)
            {
                var previous = result.LastOrDefault();

                if (previous != null && IsLabelOnly(previous) && IsWrappedLabel(previous, row))
                {
                    // 項目名稱折行，數值在第二行：把第一行的名稱併入第二行
                    result[result.Count - 1] = Combine(previous, row);
                }
                else if (previous != null && IsContinuation(previous, row))
                {
                    // 第二行只是各格文字的延續
                    result[result.Count - 1] = Combine(previous, row);
                }
                else
                {
                    result.Add(row);
                }
            }

            return result;
        }

        private static bool IsLabelOnly(TableRow row)
        {
            return !row.IsEmpty(0) && row.NonEmptyCount == 1;
        }

        /// <summary>
        /// 下一列每個有文字的格子，上一列同一格也有文字，且下一列沒有數值。
        /// 第一欄另需符合折行縮排。
        /// </summary>
        private static bool IsContinuation(TableRow previous, TableRow row)
        {
            // 分散對齊的跨欄表頭要保留給表頭處理，不與上下列合併
            if (row.HasNumericCell || row.NonEmptyCount == 0 || previous.NonEmptyCount == 0
                || row.IsLetterSpaced || previous.IsLetterSpaced)
            {
                return false;
            }

            for (var column = 0; column < row.Cells.Length; column++)
            {
                if (row.IsEmpty(column))
                {
                    continue;
                }

                if (previous.IsEmpty(column))
                {
                    return false;
                }

                if (column == 0 && !IsWrappedLabel(previous, row))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWrappedLabel(TableRow previous, TableRow row)
        {
            if (previous.IsEmpty(0) || row.IsEmpty(0))
            {
                return false;
            }

            var indent = row.CellLefts[0].Value - previous.CellLefts[0].Value;
            var em = previous.FontSize;
            var maxIndent = _numbered.IsMatch(previous.Cells[0]) && !_numbered.IsMatch(row.Cells[0])
                ? MaxHangingIndentRatio
                : MaxWrapIndentRatio;

            return indent >= em * MinWrapIndentRatio && indent <= em * maxIndent;
        }

        private static TableRow Combine(TableRow upper, TableRow lower)
        {
            var cells = upper.Cells
                .Select((cell, column) => JoinCellText(cell, lower.Cells[column]))
                .ToArray();
            var lefts = upper.CellLefts
                .Select((left, column) => left ?? lower.CellLefts[column])
                .ToArray();
            var rights = upper.CellRights
                .Select((right, column) => lower.CellRights[column] ?? right)
                .ToArray();

            return new TableRow(
                cells,
                lefts,
                rights,
                upper.HasColumnGap || lower.HasColumnGap,
                upper.IsLetterSpaced && lower.IsLetterSpaced,
                upper.FontSize);
        }

        /// <summary>儲存格文字上下接續：中文不補空白，英數字補一個空白。</summary>
        public static string JoinCellText(string upper, string lower)
        {
            if (upper.Length == 0)
            {
                return lower;
            }

            if (lower.Length == 0)
            {
                return upper;
            }

            var needsSpace = !(CjkText.IsCjk(upper[upper.Length - 1]) || CjkText.IsCjk(lower[0]));
            return needsSpace ? upper + " " + lower : upper + lower;
        }
    }
}
