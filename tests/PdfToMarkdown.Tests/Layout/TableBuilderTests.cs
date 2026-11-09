using System.Collections.Generic;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class TableBuilderTests
    {
        private const double FontSize = 10;

        /// <summary>
        /// 依「文字@X 座標」建立一行，每個字元寬一個字級；文字中的空白代表下一個字元前有 1 個字寬的空白。
        /// </summary>
        private static TextLine Line(double top, params string[] cellsAtX)
        {
            var fragments = new List<TextFragment>();
            foreach (var cell in cellsAtX)
            {
                var parts = cell.Split('@');
                var x = double.Parse(parts[1]);
                foreach (var c in parts[0])
                {
                    if (c == ' ')
                    {
                        x += FontSize;
                        continue;
                    }

                    fragments.Add(new TextFragment(c.ToString(), new Box(x, top, x + FontSize, top + FontSize), FontSize));
                    x += FontSize;
                }
            }

            return new TextLine(fragments);
        }

        [Fact]
        public void 依空白走廊切分欄位()
        {
            var rows = TableBuilder.Build(new[]
            {
                Line(0, "存款@0", "100@100", "200@200"),
                Line(13, "現金@0", "3000@90", "4@220"),
            });

            Assert.Equal(new[] { "存款", "100", "200" }, rows[0]);
            Assert.Equal(new[] { "現金", "3000", "4" }, rows[1]);
        }

        [Fact]
        public void 貨幣符號與數字之間的空白不視為欄位分隔()
        {
            var rows = TableBuilder.Build(new[]
            {
                Line(0, "股票@0", "$@100", "316,694@140"),
                Line(13, "基金@0", "1,000@170"),
            });

            Assert.Equal(new[] { "股票", "$ 316,694" }, rows[0]);
            Assert.Equal(new[] { "基金", "1,000" }, rows[1]);
        }

        [Fact]
        public void 括號負數維持在同一格()
        {
            var rows = TableBuilder.Build(new[]
            {
                Line(0, "備抵@0", "(@100", "1,760@130", ")@190"),
                Line(13, "合計@0", "2,000@130"),
            });

            Assert.Equal("( 1,760 )", rows[0][1]);
        }

        [Fact]
        public void 表頭依中心點對齊欄位()
        {
            var rows = TableBuilder.Build(new[]
            {
                Line(0, "115年@95", "114年@195"),
                Line(13, "現金@0", "100@100", "200@200"),
                Line(26, "存款@0", "300@100", "400@200"),
            });

            Assert.Equal(new[] { "", "115年", "114年" }, rows[0]);
        }

        [Fact]
        public void 越過分界的折行文字留在同一格()
        {
            // 第一行沒有欄位間距（不參與走廊計算），文字延伸超過第一欄
            var rows = TableBuilder.Build(new[]
            {
                Line(0, "原始到期日超過三個月之@0"),
                Line(13, "定期存款@0", "100@150"),
                Line(26, "質押存款@0", "200@150"),
            });

            Assert.Equal(new[] { "原始到期日超過三個月之", "" }, rows[0]);
        }

        [Fact]
        public void 移除全空的欄位()
        {
            var rows = TableBuilder.Build(new[]
            {
                Line(0, "現金@0", "100@100"),
                Line(13, "存款@0", "200@100"),
            });

            Assert.All(rows, row => Assert.Equal(2, row.Length));
        }
    }
}
