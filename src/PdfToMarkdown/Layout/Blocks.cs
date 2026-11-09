using System.Collections.Generic;

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
