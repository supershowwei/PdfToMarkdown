using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 將一頁的行切分為表格與段落區塊。
    /// </summary>
    internal static class BlockBuilder
    {
        public static List<Block> Build(IReadOnlyList<TextLine> lines, double? contentRight, IReadOnlyList<Box> rulings)
        {
            var blocks = new List<Block>();
            var next = 0;

            foreach (var (start, end) in TableRegionDetector.Find(lines))
            {
                blocks.AddRange(ParagraphBuilder.Build(Slice(lines, next, start - 1), contentRight));
                var tableLines = Slice(lines, start, end);
                blocks.Add(new TableBlock(tableLines, RulingsNear(tableLines, rulings)));
                next = end + 1;
            }

            blocks.AddRange(ParagraphBuilder.Build(Slice(lines, next, lines.Count - 1), contentRight));
            return blocks;
        }

        /// <summary>
        /// 位於表格範圍內（上下各放寬一個字級）的格線。
        /// </summary>
        private static List<Box> RulingsNear(IReadOnlyList<TextLine> lines, IReadOnlyList<Box> rulings)
        {
            var margin = lines.Max(line => line.FontSize);
            var top = lines.Min(line => line.Bounds.Top) - margin;
            var bottom = lines.Max(line => line.Bounds.Bottom) + margin;

            return rulings.Where(r => r.Bottom >= top && r.Top <= bottom).ToList();
        }

        private static List<TextLine> Slice(IReadOnlyList<TextLine> lines, int start, int end)
        {
            return lines.Skip(start).Take(end - start + 1).ToList();
        }
    }
}
