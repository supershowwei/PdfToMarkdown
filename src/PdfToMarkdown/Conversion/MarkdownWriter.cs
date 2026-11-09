using System;
using System.Collections.Generic;
using System.Text;
using PdfToMarkdown.Layout;

namespace PdfToMarkdown.Conversion
{
    /// <summary>
    /// 將版面分析結果輸出為 Markdown。
    /// </summary>
    internal static class MarkdownWriter
    {
        public static string Write(IEnumerable<PageLayout> pages)
        {
            var markdown = new StringBuilder();

            foreach (var page in pages)
            {
                // 每頁以 HTML 註解標記頁碼，方便對照原始 PDF
                markdown.Append("<!-- page ").Append(page.Number).Append(" -->\n\n");

                if (page.IsScanned)
                {
                    markdown.Append("> 本頁為掃描影像，尚未進行文字辨識。\n\n");
                    continue;
                }

                foreach (var block in page.Blocks)
                {
                    WriteBlock(markdown, block);
                }
            }

            return markdown.ToString();
        }

        private static void WriteBlock(StringBuilder markdown, Block block)
        {
            switch (block)
            {
                case ParagraphBlock paragraph:
                    markdown.Append(Escape(paragraph.Text)).Append("\n\n");
                    break;

                default:
                    throw new NotSupportedException($"未支援的區塊類型：{block.GetType().Name}");
            }
        }

        /// <summary>
        /// 跳脫行首會被 Markdown 誤判為語法的字元（標題、引言、項目清單）。
        /// 「1. 」開頭的編號保留給 Markdown 呈現為編號清單。
        /// </summary>
        private static string Escape(string text)
        {
            if (text.Length == 0)
            {
                return text;
            }

            switch (text[0])
            {
                case '#':
                case '>':
                case '-':
                case '+':
                case '*':
                    return "\\" + text;
            }

            return text;
        }
    }
}
