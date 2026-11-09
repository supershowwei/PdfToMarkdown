using System.Collections.Generic;
using System.Text;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 將段落中被折行的多行接回一行。
    /// </summary>
    internal static class LineJoiner
    {
        public static string Join(IEnumerable<TextLine> lines)
        {
            var text = new StringBuilder();

            foreach (var line in lines)
            {
                var next = line.Text.Trim();
                if (next.Length == 0)
                {
                    continue;
                }

                // 中文折行不補空白；英文、數字折行補一個空白
                if (text.Length > 0 && !(CjkText.IsCjk(text[text.Length - 1]) || CjkText.IsCjk(next[0])))
                {
                    text.Append(' ');
                }

                text.Append(next);
            }

            return text.ToString();
        }
    }
}
