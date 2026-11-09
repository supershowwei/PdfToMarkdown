using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 從表格開頭的列找出表頭，多列表頭合併為一列。
    /// </summary>
    internal static class TableHeaderBuilder
    {
        private static readonly Regex _spaceBetweenCjk = new Regex(
            @"(?<=[一-鿿　-〿＀-￯])\s+(?=[一-鿿　-〿＀-￯])",
            RegexOptions.Compiled);

        /// <summary>
        /// 分出表頭與資料列；沒有表頭時 header 為 null。
        /// </summary>
        public static (string[] Header, List<TableRow> Body) Split(IReadOnlyList<TableRow> rows)
        {
            var headerCount = 0;
            while (headerCount < rows.Count - 1 && IsHeaderRow(rows[headerCount]))
            {
                headerCount++;
            }

            if (headerCount == 0)
            {
                return (null, rows.ToList());
            }

            var headerRows = rows.Take(headerCount).ToList();
            var body = rows.Skip(headerCount).ToList();
            var header = MergeHeaderRows(headerRows);

            return MergeHeaderOnlyColumns(header, headerRows, body);
        }

        /// <summary>
        /// 表頭列：沒有數值，且第一欄空白或有多個欄位有文字。
        /// 只有第一欄有文字的列是分類標題（例如「金融資產－流動」），屬於資料列。
        /// </summary>
        public static bool IsHeaderRow(TableRow row)
        {
            return !row.HasNumericCell && (row.IsEmpty(0) || row.NonEmptyCount >= 2);
        }

        private static string[] MergeHeaderRows(IReadOnlyList<TableRow> headerRows)
        {
            var columnCount = headerRows[0].Cells.Length;
            var header = Enumerable.Repeat(string.Empty, columnCount).ToArray();

            foreach (var row in headerRows)
            {
                // 跨欄表頭（例如「所 持 股 權 百 分 比」分散在三欄）：整段文字加到每個涵蓋的欄位
                var spanText = row.IsLetterSpaced && row.NonEmptyCount >= 2
                    ? Normalize(string.Join(" ", row.Cells.Where(c => c.Length > 0)))
                    : null;

                for (var column = 0; column < columnCount; column++)
                {
                    if (row.IsEmpty(column))
                    {
                        continue;
                    }

                    var text = spanText ?? Normalize(row.Cells[column]);
                    header[column] = header[column].Length == 0 ? text : header[column] + " " + text;
                }
            }

            return header;
        }

        /// <summary>
        /// 只有表頭有文字的欄位，通常是分散對齊的表頭（例如「頁　　次」）被走廊切開，
        /// 併入水平距離最近、且有資料的相鄰欄位。
        /// </summary>
        private static (string[] Header, List<TableRow> Body) MergeHeaderOnlyColumns(
            string[] header, IReadOnlyList<TableRow> headerRows, List<TableRow> body)
        {
            bool HasBody(int column) => body.Any(row => !row.IsEmpty(column));

            for (var column = 0; column < header.Length; column++)
            {
                if (HasBody(column) || header[column].Length == 0)
                {
                    continue;
                }

                var left = Enumerable.Range(0, column).Reverse().Where(HasBody).Cast<int?>().FirstOrDefault();
                var right = Enumerable.Range(column + 1, header.Length - column - 1).Where(HasBody).Cast<int?>().FirstOrDefault();
                if (left == null && right == null)
                {
                    continue;
                }

                // 以中心點距離判斷：靠左對齊的長項目名稱可能延伸到表頭下方，用邊緣距離會誤判
                var headerCenter = Center(headerRows, column);
                var leftDistance = left == null ? double.MaxValue : headerCenter - Center(body, left.Value);
                var rightDistance = right == null ? double.MaxValue : Center(body, right.Value) - headerCenter;

                if (leftDistance <= rightDistance)
                {
                    header[left.Value] = TableRowMerger.JoinCellText(header[left.Value], header[column]);
                }
                else
                {
                    header[right.Value] = TableRowMerger.JoinCellText(header[column], header[right.Value]);
                }

                var keep = Enumerable.Range(0, header.Length).Where(c => c != column).ToArray();
                header = keep.Select(c => header[c]).ToArray();
                body = body.Select(row => row.Select(keep)).ToList();
                headerRows = headerRows.Select(row => row.Select(keep)).ToList();
                column--;
            }

            return (header, body);
        }

        /// <summary>某欄有文字的儲存格中心點的中位數。</summary>
        private static double Center(IEnumerable<TableRow> rows, int column)
        {
            var centers = rows
                .Where(row => !row.IsEmpty(column))
                .Select(row => (row.CellLefts[column].Value + row.CellRights[column].Value) / 2)
                .OrderBy(x => x)
                .ToList();
            return centers[centers.Count / 2];
        }

        /// <summary>表頭常以分散對齊排版，移除中文字之間的空白。</summary>
        private static string Normalize(string text)
        {
            return _spaceBetweenCjk.Replace(text.Trim(), string.Empty);
        }
    }
}
