using System.Collections.Generic;
using System.Linq;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class ParagraphBuilderTests
    {
        private const double FontSize = 10;
        private const double LinePitch = 20;
        private const double ContentRight = 300;

        /// <summary>
        /// 建立第 <paramref name="row"/> 行：從 <paramref name="left"/> 開始逐字排列，每字寬一個字級。
        /// </summary>
        private static TextLine Line(int row, double left, string text)
        {
            var top = row * LinePitch;
            var fragments = text.Select((c, i) => new TextFragment(
                c.ToString(),
                new Box(left + i * FontSize, top, left + (i + 1) * FontSize, top + FontSize),
                FontSize)).ToList();
            return new TextLine(fragments);
        }

        /// <summary>從左界 0 排滿到右界 300 的行（30 字）。</summary>
        private static TextLine FullLine(int row, double left = 0)
        {
            return Line(row, left, new string('字', (int)((ContentRight - left) / FontSize)));
        }

        private static List<string> Texts(IEnumerable<Block> blocks)
        {
            return blocks.Cast<ParagraphBlock>().Select(p => p.Text).ToList();
        }

        [Fact]
        public void 排滿的行與下一行合併()
        {
            var blocks = ParagraphBuilder.Build(new[] { FullLine(0), Line(1, 0, "結尾。") }, ContentRight);

            Assert.Single(blocks);
            Assert.EndsWith("字結尾。", Texts(blocks)[0]);
        }

        [Fact]
        public void 未排滿的行結束段落()
        {
            var blocks = ParagraphBuilder.Build(new[] { Line(0, 0, "短行。"), Line(1, 0, "下一段") }, ContentRight);

            Assert.Equal(new[] { "短行。", "下一段" }, Texts(blocks));
        }

        [Fact]
        public void 首行縮排後的續行合併()
        {
            var blocks = ParagraphBuilder.Build(new[] { FullLine(0, left: 20), Line(1, 0, "續行") }, ContentRight);

            Assert.Single(blocks);
        }

        [Fact]
        public void 比上一行縮排更多視為新段落()
        {
            var blocks = ParagraphBuilder.Build(new[] { FullLine(0), Line(1, 20, "新段落") }, ContentRight);

            Assert.Equal(2, blocks.Count);
        }

        [Fact]
        public void 編號段落的凸排續行合併()
        {
            var first = Line(0, 0, "(一)" + new string('字', 26));
            var blocks = ParagraphBuilder.Build(new[] { first, Line(1, 30, "續行") }, ContentRight);

            Assert.Single(blocks);
        }

        [Theory]
        [InlineData("一、公司沿革")]
        [InlineData("(二)編製基礎")]
        [InlineData("1. 第一項")]
        [InlineData("註 1：說明")]
        [InlineData("˙條列項目")]
        public void 編號或條列開頭視為新段落(string text)
        {
            var blocks = ParagraphBuilder.Build(new[] { FullLine(0), Line(1, 0, text) }, ContentRight);

            Assert.Equal(2, blocks.Count);
        }

        [Fact]
        public void 行距過大視為新段落()
        {
            var blocks = ParagraphBuilder.Build(new[] { FullLine(0), Line(3, 0, "下一段") }, ContentRight);

            Assert.Equal(2, blocks.Count);
        }

        [Fact]
        public void 有欄位間距的行不合併()
        {
            var tableRow = new TextLine(new[]
            {
                new TextFragment("項目", new Box(0, 0, 20, 10), FontSize),
                new TextFragment("100", new Box(270, 0, 300, 10), FontSize),
            });
            var blocks = ParagraphBuilder.Build(new[] { tableRow, Line(1, 0, "下一列") }, ContentRight);

            Assert.Equal(2, blocks.Count);
        }

        [Fact]
        public void 跨頁段落接續()
        {
            var page1 = new PageLayout(1, false, ParagraphBuilder.Build(new[] { FullLine(0) }, ContentRight).ToList(), ContentRight);
            var page2 = new PageLayout(2, false, ParagraphBuilder.Build(new[] { Line(0, 0, "接續。") }, ContentRight).ToList(), ContentRight);

            ParagraphBuilder.MergeAcrossPages(new[] { page1, page2 });

            Assert.EndsWith("字接續。", Texts(page1.Blocks)[0]);
            Assert.Empty(page2.Blocks);
        }

        [Fact]
        public void 前頁最後一行未排滿時不跨頁接續()
        {
            var page1 = new PageLayout(1, false, ParagraphBuilder.Build(new[] { Line(0, 0, "結束。") }, ContentRight).ToList(), ContentRight);
            var page2 = new PageLayout(2, false, ParagraphBuilder.Build(new[] { Line(0, 0, "新段落") }, ContentRight).ToList(), ContentRight);

            ParagraphBuilder.MergeAcrossPages(new[] { page1, page2 });

            Assert.Single(page2.Blocks);
        }
    }
}
