using System.Collections.Generic;
using System.Text;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 將同一行的片段串成文字，依片段間距決定是否補空白。
    /// </summary>
    internal static class TextJoiner
    {
        /// <summary>間距超過字級的此比例即視為空白。</summary>
        private const double SpaceGapRatio = 0.25;

        public static string Join(IReadOnlyList<TextFragment> fragments)
        {
            var text = new StringBuilder();
            TextFragment previous = null;

            foreach (var fragment in fragments)
            {
                if (previous != null && NeedsSpace(previous, fragment))
                {
                    text.Append(' ');
                }

                text.Append(fragment.Text);
                previous = fragment;
            }

            return text.ToString();
        }

        private static bool NeedsSpace(TextFragment left, TextFragment right)
        {
            var gap = right.Bounds.Left - left.Bounds.Right;
            var fontSize = (left.FontSize + right.FontSize) / 2;
            return gap > fontSize * SpaceGapRatio;
        }
    }
}
