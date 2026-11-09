using System.Collections.Generic;
using System.Text;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 將同一行的片段串成文字，依片段間距決定是否補空白。
    /// </summary>
    internal static class TextJoiner
    {
        /// <summary>間距超過字級的此比例即視為空白（英文單字間距）。</summary>
        private const double SpaceGapRatio = 0.25;

        /// <summary>
        /// 中文字之間、分散對齊的數字之間，間距要超過字級的此比例才視為空白。
        /// 樣本中分散對齊與左右對齊造成的字距都小於 1 個字寬，表格欄距則大於 2 個字寬。
        /// </summary>
        private const double WideGapRatio = 1.0;

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

            if (IsLetterSpaced(left, right))
            {
                return gap > fontSize * WideGapRatio;
            }

            // 小字級英文的字距可能小於門檻，因此也參考原本的空白字元
            return gap > fontSize * SpaceGapRatio || right.SpaceBefore;
        }

        /// <summary>
        /// 兩側是否可能只是字距被拉開（而非真正的分隔）：
        /// 中文字相鄰（例如「合 併 公 司」），或單一數字字元相鄰（例如表頭的「3 1 ～ 9 0 天」）。
        /// </summary>
        private static bool IsLetterSpaced(TextFragment left, TextFragment right)
        {
            var leftChar = left.Text[left.Text.Length - 1];
            var rightChar = right.Text[0];

            if (CjkText.IsCjk(leftChar) && CjkText.IsCjk(rightChar))
            {
                return true;
            }

            return left.Text.Length == 1 && right.Text.Length == 1
                && char.IsDigit(leftChar) && char.IsDigit(rightChar);
        }
    }
}
