using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 依編號規則把段落轉為標題或清單項目。
    /// </summary>
    internal static class StructureDetector
    {
        /// <summary>標題文字的最大長度；超過視為以編號開頭的一般段落。</summary>
        private const int MaxHeadingLength = 50;

        /// <summary>字級達內文的此倍數以上為一級標題（封面標題）。</summary>
        private const double TitleFontRatio = 1.5;

        /// <summary>字級達內文的此倍數以上為二級標題（副標題、目錄標題）。</summary>
        private const double SubtitleFontRatio = 1.2;

        /// <summary>目錄頁標題：「目錄」「§目　錄§」。</summary>
        private static readonly Regex _tableOfContentsTitle = new Regex(@"^\W*目\s*錄\W*$", RegexOptions.Compiled);

        /// <summary>「一、公司沿革」、「二一、收入」。</summary>
        private static readonly Regex _chapter = new Regex(@"^[一二三四五六七八九十百]+、", RegexOptions.Compiled);

        /// <summary>「(一) 遵循聲明」、「（十二）其他」。</summary>
        private static readonly Regex _section = new Regex(@"^[（(][一二三四五六七八九十百]+[)）]", RegexOptions.Compiled);

        /// <summary>條列符號與其後的空白。</summary>
        private static readonly Regex _bullet = new Regex(@"^[•●■◆▪˙・]\s*", RegexOptions.Compiled);

        /// <summary>只含中文、全形標點、括號與空白。</summary>
        private static readonly Regex _cjkOnly = new Regex(
            @"^[一-鿿　-〿＀-￯()\s]+$",
            RegexOptions.Compiled);

        /// <summary>中文字之間的空白（標題常以分散對齊排版，例如「商 譽」）。</summary>
        private static readonly Regex _spaceBetweenCjk = new Regex(
            @"(?<=[一-鿿　-〿＀-￯])\s+(?=[一-鿿　-〿＀-￯])",
            RegexOptions.Compiled);

        /// <param name="page">要轉換的頁面。</param>
        /// <param name="bodyFontSize">內文字級，見 <see cref="FindBodyFontSize"/>。</param>
        public static void Apply(PageLayout page, double bodyFontSize)
        {
            // 目錄頁的「一、」「(一)」是目錄項目而非標題
            var isTableOfContents = page.Blocks.FirstOrDefault() is ParagraphBlock first
                && _tableOfContentsTitle.IsMatch(first.Text);

            for (var i = 0; i < page.Blocks.Count; i++)
            {
                if (page.Blocks[i] is ParagraphBlock paragraph)
                {
                    page.Blocks[i] = ClassifyByFontSize(paragraph, bodyFontSize)
                        ?? (isTableOfContents ? paragraph : Classify(paragraph));
                }
            }

            MergeConsecutiveTitles(page.Blocks);
        }

        /// <summary>
        /// 內文字級：全文件各行字級的眾數（以字數加權）。
        /// </summary>
        public static double FindBodyFontSize(IEnumerable<PageLayout> pages)
        {
            return pages
                .SelectMany(page => page.Blocks.OfType<ParagraphBlock>())
                .SelectMany(paragraph => paragraph.Lines)
                .GroupBy(line => System.Math.Round(line.FontSize * 2) / 2)
                .OrderByDescending(group => group.Sum(line => line.Fragments.Count))
                .Select(group => group.Key)
                .FirstOrDefault();
        }

        /// <summary>
        /// 單行且字級明顯大於內文者轉為標題；不符合時回傳 null。
        /// </summary>
        private static Block ClassifyByFontSize(ParagraphBlock paragraph, double bodyFontSize)
        {
            if (paragraph.Lines.Count != 1 || bodyFontSize <= 0)
            {
                return null;
            }

            var ratio = paragraph.Lines[0].FontSize / bodyFontSize;
            var text = NormalizeHeading(paragraph.Text.Trim());

            if (ratio >= TitleFontRatio)
            {
                return new HeadingBlock(1, text);
            }

            if (ratio >= SubtitleFontRatio)
            {
                return new HeadingBlock(2, text);
            }

            return null;
        }

        /// <summary>
        /// 封面標題常分成多行，連續的一級標題合併為一個。
        /// </summary>
        private static void MergeConsecutiveTitles(List<Block> blocks)
        {
            for (var i = blocks.Count - 1; i > 0; i--)
            {
                if (blocks[i] is HeadingBlock current && current.Level == 1
                    && blocks[i - 1] is HeadingBlock previous && previous.Level == 1)
                {
                    blocks[i - 1] = new HeadingBlock(1, previous.Text + " " + current.Text);
                    blocks.RemoveAt(i);
                }
            }
        }

        public static Block Classify(ParagraphBlock paragraph)
        {
            var text = paragraph.Text.Trim();

            // 有欄位間距的行（例如目錄、表格）不轉換；
            // 但純中文的短行可能只是分散對齊的標題（例如「十六、商　　譽」）
            if (paragraph.Lines.Any(line => line.HasColumnGap) && !_cjkOnly.IsMatch(text))
            {
                return paragraph;
            }

            var bullet = _bullet.Match(text);
            if (bullet.Success)
            {
                return new ListItemBlock(text.Substring(bullet.Length));
            }

            if (IsHeadingText(text))
            {
                if (_chapter.IsMatch(text))
                {
                    return new HeadingBlock(2, NormalizeHeading(text));
                }

                if (_section.IsMatch(text))
                {
                    return new HeadingBlock(3, NormalizeHeading(text));
                }
            }

            return paragraph;
        }

        private static bool IsHeadingText(string text)
        {
            return text.Length <= MaxHeadingLength && !text.Contains("。");
        }

        private static string NormalizeHeading(string text)
        {
            return _spaceBetweenCjk.Replace(text, string.Empty).Trim('§', ' ');
        }
    }
}
