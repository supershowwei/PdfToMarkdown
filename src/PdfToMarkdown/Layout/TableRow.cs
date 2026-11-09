using System.Linq;
using System.Text.RegularExpressions;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 表格的一列：各欄文字與左右緣位置。
    /// </summary>
    internal sealed class TableRow
    {
        /// <summary>單一數值：金額、百分比，或「-」（代表零）。</summary>
        private static readonly Regex _number = new Regex(@"^(-|-?\d[\d,]*(\.\d+)?%?)$", RegexOptions.Compiled);

        private static readonly char[] _currencyAndParentheses = { '$', '＄', '(', ')', '（', '）' };

        private static readonly char[] _whitespace = { ' ', '\t' };

        public TableRow(string[] cells, double?[] cellLefts, double?[] cellRights, bool hasColumnGap, bool isLetterSpaced, double fontSize)
        {
            Cells = cells;
            CellLefts = cellLefts;
            CellRights = cellRights;
            HasColumnGap = hasColumnGap;
            IsLetterSpaced = isLetterSpaced;
            FontSize = fontSize;
        }

        public string[] Cells { get; }

        /// <summary>各格第一個字的左緣；空白格為 null。</summary>
        public double?[] CellLefts { get; }

        /// <summary>各格最後一個字的右緣；空白格為 null。</summary>
        public double?[] CellRights { get; }

        /// <summary>原始行是否有明顯的欄位間距。</summary>
        public bool HasColumnGap { get; }

        /// <summary>原始行是否為一段分散對齊的文字（字距大而平均，例如跨欄表頭「所 持 股 權 百 分 比」）。</summary>
        public bool IsLetterSpaced { get; }

        public double FontSize { get; }

        public bool IsEmpty(int column) => Cells[column].Length == 0;

        public int NonEmptyCount => Cells.Count(cell => cell.Length > 0);

        public bool HasNumericCell => Cells.Skip(1).Any(IsNumeric);

        /// <summary>
        /// 數值儲存格：一或多個數值，可含 $ 與括號。
        /// 欄位切分失敗時一格可能含多個數字（例如「$ 19,609 $ 6,121」），仍視為數值。
        /// </summary>
        public static bool IsNumeric(string text)
        {
            // 括號不成對（例如折行的「公司淨值 20%）」末段「20%）」）不是數值
            var opens = text.Count(c => c == '(' || c == '（');
            var closes = text.Count(c => c == ')' || c == '）');
            if (opens != closes)
            {
                return false;
            }

            var tokens = new string(text.Select(c => _currencyAndParentheses.Contains(c) ? ' ' : c).ToArray())
                .Split(_whitespace, System.StringSplitOptions.RemoveEmptyEntries);

            return tokens.Length > 0 && tokens.All(token => _number.IsMatch(token));
        }

        /// <summary>只保留指定的欄位。</summary>
        public TableRow Select(int[] columns)
        {
            return new TableRow(
                columns.Select(c => Cells[c]).ToArray(),
                columns.Select(c => CellLefts[c]).ToArray(),
                columns.Select(c => CellRights[c]).ToArray(),
                HasColumnGap,
                IsLetterSpaced,
                FontSize);
        }
    }
}
