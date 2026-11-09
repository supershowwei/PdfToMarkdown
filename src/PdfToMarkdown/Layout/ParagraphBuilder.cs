using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 將一頁的行合併成段落，並處理跨頁接續。
    /// </summary>
    internal static class ParagraphBuilder
    {
        /// <summary>行尾距離內文右界在此字寬倍數內，視為「排滿」的行（段落尚未結束）。</summary>
        private const double FullLineToleranceRatio = 2.0;

        /// <summary>下一行比上一行多縮排超過此字寬倍數，視為新段落。</summary>
        private const double IndentToleranceRatio = 0.5;

        /// <summary>
        /// 以編號開頭的段落，第二行常採凸排（對齊編號後的文字），允許多縮排至此字寬倍數。
        /// </summary>
        private const double HangingIndentRatio = 4.0;

        /// <summary>兩行間距（上一行底到下一行頂）超過此字寬倍數，視為新段落。樣本行距約 1 個字寬。</summary>
        private const double ParagraphGapRatio = 1.2;

        /// <summary>計算內文右界時，只採計至少這麼多字的行。</summary>
        private const int MinCharsForRightEdge = 10;

        /// <summary>以這些開頭的行一定是新段落（編號、註解、條列符號）。</summary>
        private static readonly Regex _blockStart = new Regex(
            @"^(第?[一二三四五六七八九十百]+、" +
            @"|[（(][一二三四五六七八九十百]+[)）]" +
            @"|[（(]?\d{1,2}[)）.、]" +
            @"|註\s*[一二三四五六七八九十\d]*\s*[：:]" +
            @"|[•●■◆▪˙・])",
            RegexOptions.Compiled);

        /// <param name="lines">一頁的行。</param>
        /// <param name="contentRight">內文右界，見 <see cref="FindContentRight"/>；沒有時不合併。</param>
        public static List<Block> Build(IReadOnlyList<TextLine> lines, double? contentRight)
        {
            var blocks = new List<Block>();
            ParagraphBlock current = null;

            foreach (var line in lines)
            {
                if (current != null && contentRight.HasValue && Continues(current, line, contentRight.Value))
                {
                    current.Lines.Add(line);
                    continue;
                }

                current = new ParagraphBlock(new[] { line });
                blocks.Add(current);
            }

            return blocks;
        }

        /// <summary>
        /// 前一頁最後一段若尚未結束，把下一頁第一段接上去。
        /// </summary>
        public static void MergeAcrossPages(IReadOnlyList<PageLayout> pages)
        {
            for (var i = 1; i < pages.Count; i++)
            {
                var previousPage = pages[i - 1];
                var page = pages[i];

                if (!(previousPage.Blocks.LastOrDefault() is ParagraphBlock tail)
                    || !(page.Blocks.FirstOrDefault() is ParagraphBlock head))
                {
                    continue;
                }

                var lastLine = tail.Lines.Last();
                var firstLine = head.Lines.First();

                if (previousPage.ContentRight.HasValue
                    && IsFull(lastLine, previousPage.ContentRight.Value)
                    && IsProse(firstLine)
                    && !_blockStart.IsMatch(firstLine.Text.Trim())
                    && Math.Abs(firstLine.FontSize - lastLine.FontSize) < 0.5)
                {
                    tail.Lines.AddRange(head.Lines);
                    page.Blocks.RemoveAt(0);
                }
            }
        }

        private static bool Continues(ParagraphBlock paragraph, TextLine line, double contentRight)
        {
            var previous = paragraph.Lines.Last();
            var em = previous.FontSize;

            var isHangingIndent = paragraph.Lines.Count == 1 && _blockStart.IsMatch(previous.Text.Trim());
            var indentTolerance = em * (isHangingIndent ? HangingIndentRatio : IndentToleranceRatio);

            return IsProse(previous)
                && IsProse(line)
                && IsFull(previous, contentRight)
                && !_blockStart.IsMatch(line.Text.Trim())
                && Math.Abs(line.FontSize - previous.FontSize) < 0.5
                && line.Bounds.Left <= previous.Bounds.Left + indentTolerance
                && line.Bounds.Top - previous.Bounds.Bottom <= em * ParagraphGapRatio;
        }

        private static bool IsProse(TextLine line)
        {
            return !line.HasColumnGap;
        }

        private static bool IsFull(TextLine line, double contentRight)
        {
            return line.Bounds.Right >= contentRight - line.FontSize * FullLineToleranceRatio;
        }

        /// <summary>
        /// 內文右界：較長內文行右緣的最常見位置（以 2 點分組），不受表格或短行影響。
        /// </summary>
        public static double? FindContentRight(IEnumerable<TextLine> lines)
        {
            var bucket = lines
                .Where(line => IsProse(line) && line.Fragments.Count >= MinCharsForRightEdge)
                .GroupBy(line => Math.Round(line.Bounds.Right / 2))
                .OrderByDescending(group => group.Count())
                .ThenByDescending(group => group.Key)
                .FirstOrDefault();

            return bucket?.Max(line => line.Bounds.Right);
        }
    }
}
