using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 頁面上的內容區塊（段落、標題、表格等）。
    /// </summary>
    internal abstract class Block
    {
    }

    /// <summary>
    /// 段落：由一或多行組成，輸出時串成一行。
    /// </summary>
    internal sealed class ParagraphBlock : Block
    {
        public ParagraphBlock(IEnumerable<TextLine> lines)
        {
            Lines = new List<TextLine>(lines);
        }

        public List<TextLine> Lines { get; }

        public string Text => LineJoiner.Join(Lines);
    }

    /// <summary>
    /// 表格：由連續的行組成。
    /// </summary>
    internal sealed class TableBlock : Block
    {
        public TableBlock(IEnumerable<TextLine> lines)
        {
            Lines = new List<TextLine>(lines);

            var rows = TableRowMerger.Merge(TableBuilder.Build(Lines));
            var (header, body) = TableHeaderBuilder.Split(rows);
            Header = header;
            Rows = body.Select(row => row.Cells).ToList();
        }

        public List<TextLine> Lines { get; }

        /// <summary>表頭；沒有表頭時為 null。</summary>
        public string[] Header { get; }

        /// <summary>資料列的儲存格文字，每列欄位數相同。</summary>
        public List<string[]> Rows { get; }
    }

    /// <summary>
    /// 標題。
    /// </summary>
    internal sealed class HeadingBlock : Block
    {
        public HeadingBlock(int level, string text)
        {
            Level = level;
            Text = text;
        }

        /// <summary>Markdown 標題層級（1～6）。</summary>
        public int Level { get; }

        public string Text { get; }
    }

    /// <summary>
    /// 項目清單的一個項目（不含條列符號）。
    /// </summary>
    internal sealed class ListItemBlock : Block
    {
        public ListItemBlock(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    /// <summary>
    /// 一頁的版面分析結果。
    /// </summary>
    internal sealed class PageLayout
    {
        public PageLayout(int number, bool isScanned, List<Block> blocks, double? contentRight = null)
        {
            Number = number;
            IsScanned = isScanned;
            Blocks = blocks;
            ContentRight = contentRight;
        }

        public int Number { get; }

        public bool IsScanned { get; }

        public List<Block> Blocks { get; }

        /// <summary>內文右界（判斷段落是否跨頁接續用）；無法判斷時為 null。</summary>
        public double? ContentRight { get; }
    }
}
