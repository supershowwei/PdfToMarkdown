using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class AmountFormatterTests
    {
        [Theory]
        [InlineData("( 1,760 )", "(1,760)")]
        [InlineData("( $ 347,048 )", "($ 347,048)")]
        [InlineData("$ 2,683,856", "$ 2,683,856")]
        [InlineData("-", "-")]
        [InlineData("4.22%", "4.22%")]
        public void 只移除括號內側的空白(string text, string expected)
        {
            Assert.Equal(expected, AmountFormatter.Normalize(text));
        }

        [Fact]
        public void 非數值文字不變更()
        {
            Assert.Equal("( 註 1 )", AmountFormatter.Normalize("( 註 1 )"));
        }
    }
}
