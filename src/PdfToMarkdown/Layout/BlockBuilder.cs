using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 將一頁的行切分為表格與段落區塊。
    /// </summary>
    internal static class BlockBuilder
    {
        public static List<Block> Build(IReadOnlyList<TextLine> lines, double? contentRight)
        {
            var blocks = new List<Block>();
            var next = 0;

            foreach (var (start, end) in TableRegionDetector.Find(lines))
            {
                blocks.AddRange(ParagraphBuilder.Build(Slice(lines, next, start - 1), contentRight));
                blocks.Add(new TableBlock(Slice(lines, start, end)));
                next = end + 1;
            }

            blocks.AddRange(ParagraphBuilder.Build(Slice(lines, next, lines.Count - 1), contentRight));
            return blocks;
        }

        private static List<TextLine> Slice(IReadOnlyList<TextLine> lines, int start, int end)
        {
            return lines.Skip(start).Take(end - start + 1).ToList();
        }
    }
}
