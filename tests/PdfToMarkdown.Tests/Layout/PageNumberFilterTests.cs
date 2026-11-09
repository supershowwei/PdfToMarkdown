using System.Linq;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class PageNumberFilterTests
    {
        private const double PageHeight = 1000;

        private static TextLine Line(string text, double top)
        {
            return new TextLine(new[] { new TextFragment(text, new Box(0, top, 50, top + 10), 10) });
        }

        private static string[] Filter(params TextLine[] lines)
        {
            return PageNumberFilter.Apply(lines, PageHeight).Select(l => l.Text).ToArray();
        }

        [Theory]
        [InlineData("- 17 -")]
        [InlineData("-17-")]
        [InlineData("－ 3 －")]
        [InlineData("第 5 頁")]
        public void 移除頁尾頁碼(string pageNumber)
        {
            Assert.Equal(new[] { "內文" }, Filter(Line("內文", 500), Line(pageNumber, 950)));
        }

        [Fact]
        public void 移除頁首頁碼()
        {
            Assert.Equal(new[] { "內文" }, Filter(Line("- 2 -", 20), Line("內文", 500)));
        }

        [Fact]
        public void 頁面中間的相同格式不移除()
        {
            Assert.Equal(new[] { "- 17 -" }, Filter(Line("- 17 -", 500)));
        }

        [Fact]
        public void 邊緣的一般文字不移除()
        {
            Assert.Equal(new[] { "- 17 - 附註" }, Filter(Line("- 17 - 附註", 950)));
        }
    }
}
