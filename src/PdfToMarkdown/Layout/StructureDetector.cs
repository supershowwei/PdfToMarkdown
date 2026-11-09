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

        public static void Apply(PageLayout page)
        {
            for (var i = 0; i < page.Blocks.Count; i++)
            {
                if (page.Blocks[i] is ParagraphBlock paragraph)
                {
                    page.Blocks[i] = Classify(paragraph);
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
            return _spaceBetweenCjk.Replace(text, string.Empty);
        }
    }
}
