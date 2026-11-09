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
            // 先處理有完整格線的表格，其餘的行再依文字排列判斷表格與段落
            var grids = LatticeGrid.Find(rulings);
            var blocks = new List<Block>();
            var pending = new List<TextLine>();
            var emitted = new HashSet<LatticeGrid>();

            foreach (var line in lines)
            {
                var grid = grids.FirstOrDefault(g => g.Contains(line));
                if (grid == null)
                {
                    pending.Add(line);
                    continue;
                }

                if (emitted.Add(grid))
                {
                    blocks.AddRange(BuildWithoutGrid(pending, contentRight, rulings));
                    pending.Clear();
                    blocks.Add(TableBlock.FromGrid(lines.Where(grid.Contains).ToList(), grid));
                }
            }

            blocks.AddRange(BuildWithoutGrid(pending, contentRight, rulings));
            return blocks;
        }

        private static List<Block> BuildWithoutGrid(IReadOnlyList<TextLine> lines, double? contentRight, IReadOnlyList<Box> rulings)
        {
            var blocks = new List<Block>();
            var next = 0;

            foreach (var (start, end) in TableRegionDetector.Find(lines))
            {
                blocks.AddRange(ParagraphBuilder.Build(Slice(lines, next, start - 1), contentRight));
                var tableLines = Slice(lines, start, end);
                blocks.Add(TableBlock.FromLines(tableLines, RulingsNear(tableLines, rulings)));
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
