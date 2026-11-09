using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 移除頁面上下邊緣的頁碼行（例如「- 17 -」）。
    /// </summary>
    internal static class PageNumberFilter
    {
        /// <summary>頁碼只在頁面上下此比例的範圍內才移除，避免誤刪內文。</summary>
        private const double MarginRatio = 0.15;

        private static readonly Regex _pageNumber = new Regex(
            @"^\s*[-－–—]\s*\d{1,4}\s*[-－–—]\s*$|^\s*第\s*\d{1,4}\s*頁\s*$",
            RegexOptions.Compiled);

        public static IReadOnlyList<TextLine> Apply(IReadOnlyList<TextLine> lines, double pageHeight)
        {
            var top = pageHeight * MarginRatio;
            var bottom = pageHeight * (1 - MarginRatio);

            return lines
                .Where(line => !(IsInMargin(line, top, bottom) && _pageNumber.IsMatch(line.Text)))
                .ToList();
        }

        private static bool IsInMargin(TextLine line, double top, double bottom)
        {
            return line.Bounds.Bottom <= top || line.Bounds.Top >= bottom;
        }
    }
}
