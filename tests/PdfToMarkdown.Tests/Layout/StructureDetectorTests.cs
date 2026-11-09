using System.Linq;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class StructureDetectorTests
    {
        private const double FontSize = 10;

        /// <summary>
        /// 建立單行段落；文字中的「|」代表 3 個字寬的間距（會被視為欄位間距）。
        /// </summary>
        private static ParagraphBlock Paragraph(string text)
        {
            var fragments = new System.Collections.Generic.List<TextFragment>();
            double x = 0;
            foreach (var c in text)
            {
                if (c == '|')
                {
                    x += FontSize * 3;
                    continue;
                }

                fragments.Add(new TextFragment(c.ToString(), new Box(x, 0, x + FontSize, FontSize), FontSize));
                x += FontSize;
            }

            return new ParagraphBlock(new[] { new TextLine(fragments) });
        }

        [Theory]
        [InlineData("一、公司沿革", "一、公司沿革")]
        [InlineData("二一、收入", "二一、收入")]
        [InlineData("十六、商|譽", "十六、商譽")]
        public void 中文數字加頓號為二級標題(string text, string expected)
        {
            var heading = Assert.IsType<HeadingBlock>(StructureDetector.Classify(Paragraph(text)));

            Assert.Equal(2, heading.Level);
            Assert.Equal(expected, heading.Text);
        }

        [Theory]
        [InlineData("(一)遵循聲明")]
        [InlineData("（十二）其他")]
        public void 括號中文數字為三級標題(string text)
        {
            var heading = Assert.IsType<HeadingBlock>(StructureDetector.Classify(Paragraph(text)));

            Assert.Equal(3, heading.Level);
        }

        [Fact]
        public void 過長或含句號的編號段落維持段落()
        {
            Assert.IsType<ParagraphBlock>(StructureDetector.Classify(Paragraph("(一)" + new string('字', 60))));
            Assert.IsType<ParagraphBlock>(StructureDetector.Classify(Paragraph("一、本公司設立於68年。")));
        }

        [Fact]
        public void 含數字的欄位間距行維持段落()
        {
            // 目錄行：「三、會計師核閱報告 3～4」
            Assert.IsType<ParagraphBlock>(StructureDetector.Classify(Paragraph("三、會計師核閱報告|3～4")));
        }

        [Theory]
        [InlineData("˙損益表應列報營業損益。", "損益表應列報營業損益。")]
        [InlineData("•項目", "項目")]
        public void 條列符號轉為清單項目(string text, string expected)
        {
            var item = Assert.IsType<ListItemBlock>(StructureDetector.Classify(Paragraph(text)));

            Assert.Equal(expected, item.Text);
        }

        [Fact]
        public void 一般段落維持段落()
        {
            Assert.IsType<ParagraphBlock>(StructureDetector.Classify(Paragraph("本合併財務報告於董事會通過。")));
        }

        [Fact]
        public void Apply會替換頁面中的區塊()
        {
            var page = new PageLayout(1, false, new[] { (Block)Paragraph("一、公司沿革"), Paragraph("內文") }.ToList());

            StructureDetector.Apply(page);

            Assert.IsType<HeadingBlock>(page.Blocks[0]);
            Assert.IsType<ParagraphBlock>(page.Blocks[1]);
        }
    }
}
