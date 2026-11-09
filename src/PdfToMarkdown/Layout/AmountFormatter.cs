using System.Text.RegularExpressions;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 整理金額文字的排版空白。只移除空白，不改變數值與符號（保留原文）。
    /// </summary>
    internal static class AmountFormatter
    {
        /// <summary>左括號後的空白：「( 1,760 )」→「(1,760 )」。</summary>
        private static readonly Regex _afterOpenParenthesis = new Regex(@"([(（])\s+", RegexOptions.Compiled);

        /// <summary>右括號前的空白：「(1,760 )」→「(1,760)」。</summary>
        private static readonly Regex _beforeCloseParenthesis = new Regex(@"\s+([)）])", RegexOptions.Compiled);

        public static string Normalize(string text)
        {
            if (!TableRow.IsNumeric(text))
            {
                return text;
            }

            text = _afterOpenParenthesis.Replace(text, "$1");
            return _beforeCloseParenthesis.Replace(text, "$1");
        }
    }
}
