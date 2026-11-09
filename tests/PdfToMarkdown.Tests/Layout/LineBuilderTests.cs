using System.Linq;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class LineBuilderTests
    {
        /// <summary>建立字級 10、寬 10 的片段，top 為字元框上緣。</summary>
        private static TextFragment F(string text, double left, double top, double width = 10)
        {
            return new TextFragment(text, new Box(left, top, left + width, top + 10), 10);
        }

        [Fact]
        public void 同一行的片段依X排序()
        {
            var lines = LineBuilder.Build(new[] { F("b", 10, 0), F("a", 0, 0), F("c", 20, 0) });

            Assert.Single(lines);
            Assert.Equal("abc", lines[0].Text);
        }

        [Fact]
        public void 不同行依Y由上而下排序()
        {
            var lines = LineBuilder.Build(new[] { F("下", 0, 30), F("上", 0, 0), F("中", 0, 15) });

            Assert.Equal(new[] { "上", "中", "下" }, lines.Select(l => l.Text));
        }

        [Fact]
        public void 垂直位置略有偏移仍視為同一行()
        {
            var lines = LineBuilder.Build(new[] { F("a", 0, 0), F("b", 10, 3) });

            Assert.Single(lines);
        }

        [Fact]
        public void 垂直重疊不足一半視為不同行()
        {
            var lines = LineBuilder.Build(new[] { F("a", 0, 0), F("b", 10, 6) });

            Assert.Equal(2, lines.Count);
        }

        [Fact]
        public void 間距超過字級四分之一補空白()
        {
            var lines = LineBuilder.Build(new[] { F("a", 0, 0), F("b", 10, 0), F("c", 25, 0) });

            Assert.Equal("ab c", lines[0].Text);
        }

        [Fact]
        public void 沒有片段時回傳空集合()
        {
            Assert.Empty(LineBuilder.Build(new TextFragment[0]));
        }
    }
}
