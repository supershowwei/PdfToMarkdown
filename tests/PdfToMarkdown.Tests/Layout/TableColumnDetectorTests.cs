using System.Collections.Generic;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class TableColumnDetectorTests
    {
        private const double FontSize = 10;

        /// <summary>依「文字@X」建立一行，每字寬一個字級，同一格的字緊鄰。</summary>
        private static TextLine Line(double top, params string[] cellsAtX)
        {
            var fragments = new List<TextFragment>();
            foreach (var cell in cellsAtX)
            {
                var parts = cell.Split('@');
                var x = double.Parse(parts[1]);
                foreach (var c in parts[0])
                {
                    fragments.Add(new TextFragment(c.ToString(), new Box(x, top, x + FontSize, top + FontSize), FontSize));
                    x += FontSize;
                }
            }

            return new TextLine(fragments);
        }

        private static Box Underline(double left, double right)
        {
            return new Box(left, 50, right, 50.5);
        }

        [Fact]
        public void 底線空隙補上過窄的欄位分界()
        {
            // 第一欄「信錦企業」與第二欄「廣進」只隔 3 點，走廊太窄；欄位底線在 43～47 之間有空隙
            var lines = new[]
            {
                Line(0, "信錦企業@0", "廣進@43", "100@200"),
                Line(13, "股份有限公司@0", "富慶@73", "200@200"),
            };

            var separators = TableColumnDetector.Find(lines, new[] { Underline(0, 43), Underline(47, 150), Underline(190, 230) });

            Assert.Contains(45.0, separators.Hard);
        }

        [Fact]
        public void 底線空隙落在分散對齊的字之間時不採用()
        {
            // 「新 台 幣」每字間隔 5 點，底線空隙剛好在「新」與「台」之間
            var fragments = new List<TextFragment>();
            foreach (var (c, x) in new[] { ("新", 0.0), ("台", 15.0), ("幣", 30.0), ("1", 100.0) })
            {
                fragments.Add(new TextFragment(c, new Box(x, 0, x + FontSize, FontSize), FontSize));
            }

            var lines = new[] { new TextLine(fragments), Line(13, "其他@0", "2@100") };

            var separators = TableColumnDetector.Find(lines, new[] { Underline(0, 11), Underline(14, 60) });

            Assert.Empty(separators.Hard);
        }

        [Fact]
        public void 間隔其他欄的合計底線不視為分界()
        {
            var lines = new[]
            {
                Line(0, "現金@0", "100@100", "200@200", "300@300"),
                Line(13, "存款@0", "100@100", "200@200", "300@300"),
            };

            var separators = TableColumnDetector.Find(lines, new[] { Underline(100, 130), Underline(300, 330) });

            Assert.Empty(separators.Hard);
        }
    }
}
