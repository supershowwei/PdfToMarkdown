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

                case HeadingBlock heading:
                    markdown.Append('#', heading.Level).Append(' ').Append(heading.Text).Append("\n\n");
                    break;

                case TableBlock table:
                    WriteTable(markdown, table.Rows);
                    break;

                case ListItemBlock item:
                    markdown.Append("- ").Append(item.Text).Append("\n\n");
                    break;

                default:
                    throw new NotSupportedException($"未支援的區塊類型：{block.GetType().Name}");
            }
        }

        /// <summary>
        /// 輸出 Markdown 表格，第一列作為表頭。
        /// </summary>
        private static void WriteTable(StringBuilder markdown, IReadOnlyList<string[]> rows)
        {
            if (rows.Count == 0 || rows[0].Length == 0)
            {
                return;
            }

            WriteRow(markdown, rows[0]);
            markdown.Append('|');
            for (var i = 0; i < rows[0].Length; i++)
            {
                markdown.Append(" --- |");
            }

            markdown.Append('\n');

            for (var i = 1; i < rows.Count; i++)
            {
                WriteRow(markdown, rows[i]);
            }

            markdown.Append('\n');
        }

        private static void WriteRow(StringBuilder markdown, string[] cells)
        {
            markdown.Append('|');
            foreach (var cell in cells)
            {
                markdown.Append(' ').Append(cell.Replace("|", "\\|")).Append(" |");
            }

            markdown.Append('\n');
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
