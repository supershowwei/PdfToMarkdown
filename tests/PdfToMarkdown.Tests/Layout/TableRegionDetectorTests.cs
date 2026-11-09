using System.Collections.Generic;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class TableRegionDetectorTests
    {
        private const double FontSize = 10;

        /// <summary>表格列距（1.3 倍字級）。</summary>
        private const double TableStep = 13;

        /// <summary>內文行距（2 倍字級）。</summary>
        private const double ProseStep = 20;

        /// <summary>
        /// 建立一行；文字中的「|」代表 5 個字寬的欄位間距。
        /// </summary>
        private static TextLine Line(double top, string text)
        {
            var fragments = new List<TextFragment>();
            double x = 0;
            foreach (var c in text)
            {
                if (c == '|')
                {
                    x += FontSize * 5;
                    continue;
                }

                fragments.Add(new TextFragment(c.ToString(), new Box(x, top, x + FontSize, top + FontSize), FontSize));
                x += FontSize;
            }

            return new TextLine(fragments);
        }

        [Fact]
        public void 連續具欄位間距的行為表格()
        {
            var lines = new[]
            {
                Line(0, "說明文字。"),
                Line(ProseStep, "存款|100|200"),
                Line(ProseStep + TableStep, "現金|300|400"),
                Line(ProseStep * 2 + TableStep, "後續說明。"),
            };

            Assert.Equal(new[] { (1, 2) }, TableRegionDetector.Find(lines));
        }

        [Fact]
        public void 向上納入表頭與向下納入折行的項目名稱()
        {
            var lines = new[]
            {
                Line(0, "115年3月31日"),
                Line(TableStep, "原始到期日超過三個月之"),
                Line(TableStep * 2, "定期存款|100|200"),
                Line(TableStep * 3, "合計|100|200"),
            };

            Assert.Equal(new[] { (0, 3) }, TableRegionDetector.Find(lines));
        }

        [Fact]
        public void 行距為內文行距時不納入表格()
        {
            var lines = new[]
            {
                Line(0, "八、按攤銷後成本衡量之金融資產"),
                Line(ProseStep, "定期存款|100|200"),
                Line(ProseStep + TableStep, "合計|100|200"),
            };

            Assert.Equal(new[] { (1, 2) }, TableRegionDetector.Find(lines));
        }

        [Fact]
        public void 含句子標點的長行視為內文()
        {
            var lines = new[]
            {
                Line(0, "定期存款|100|200"),
                Line(TableStep, "合計|100|200"),
                Line(TableStep * 2, "註：合併公司與銀行簽訂結構式存款合約，該存款包括衍生工具。"),
            };

            Assert.Equal(new[] { (0, 1) }, TableRegionDetector.Find(lines));
        }

        [Fact]
        public void 單獨一行欄位間距不構成表格()
        {
            var lines = new[]
            {
                Line(0, "說明文字。"),
                Line(ProseStep, "十六、商|譽"),
                Line(ProseStep * 2, "說明文字。"),
            };

            Assert.Empty(TableRegionDetector.Find(lines));
        }

        [Fact]
        public void 單一欄位間距但連續三行表格列距仍為表格()
        {
            var lines = new[]
            {
                Line(0, "115年3月31日"),
                Line(TableStep, "非流動"),
                Line(TableStep * 2, "質押定期存款|100|200"),
            };

            Assert.Equal(new[] { (0, 2) }, TableRegionDetector.Find(lines));
        }
    }
}
