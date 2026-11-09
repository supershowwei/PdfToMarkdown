using System.Collections.Generic;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class TextJoinerTests
    {
        private const double FontSize = 10;

        /// <summary>
        /// 依序排列片段：每個片段寬度為「字數 × 字級」，片段之間的間距以字級倍數指定。
        /// </summary>
        private static string Join(params object[] textsAndGaps)
        {
            var fragments = new List<TextFragment>();
            double x = 0;

            foreach (var item in textsAndGaps)
            {
                if (item is double gap)
                {
                    x += gap * FontSize;
                    continue;
                }

                var text = (string)item;
                var width = text.Length * FontSize;
                fragments.Add(new TextFragment(text, new Box(x, 0, x + width, FontSize), FontSize));
                x += width;
            }

            return TextJoiner.Join(fragments);
        }

        [Fact]
        public void 中文字距拉開時合併()
        {
            Assert.Equal("合併公司", Join("合", 0.5, "併", 0.8, "公", 0.3, "司"));
        }

        [Fact]
        public void 中文字之間間距超過一個字寬視為分隔()
        {
            Assert.Equal("合計 金額", Join("合", 0.2, "計", 1.5, "金", 0.2, "額"));
        }

        [Fact]
        public void 中文與全形標點視為中文()
        {
            Assert.Equal("（註一）", Join("（", 0.5, "註", 0.5, "一", 0.5, "）"));
        }

        [Fact]
        public void 中文與數字之間保留空白()
        {
            Assert.Equal("民國 115 年", Join("民", "國", 0.3, "1", "1", "5", 0.3, "年"));
        }

        [Fact]
        public void 英文單字間保留空白()
        {
            Assert.Equal("Syncmold Enterprise", Join("Syncmold", 0.3, "Enterprise"));
        }

        [Fact]
        public void 分散對齊的單一數字合併()
        {
            Assert.Equal("180", Join("1", 0.4, "8", 0.4, "0"));
        }

        [Fact]
        public void 表格欄位中的數字保留空白()
        {
            Assert.Equal("100 100", Join("1", "0", "0", 2.5, "1", "0", "0"));
        }

        [Fact]
        public void 多字元片段相鄰的數字保留空白()
        {
            // OCR 來源以字詞為片段，字詞邊界即為分隔
            Assert.Equal("1,184 1,511", Join("1,184", 0.4, "1,511"));
        }

        [Fact]
        public void 間距很小時不補空白()
        {
            Assert.Equal("ab", Join("a", 0.1, "b"));
        }
    }
}
