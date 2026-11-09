using System.Collections.Generic;
using System.Linq;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class LatticeTableTests
    {
        private const double FontSize = 10;

        private static Box VLine(double x, double top, double bottom) => new Box(x, top, x + 0.5, bottom);

        private static Box HLine(double y, double left, double right) => new Box(left, y, right, y + 0.5);

        /// <summary>依「文字@X」建立一行，每字寬一個字級。</summary>
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

        /// <summary>
        /// 三欄格線表格：表頭兩列（「擔保品」跨第二、三欄），資料區沒有水平線。
        /// 欄界 x = 0, 100, 200, 300；表頭上緣 y=0、跨欄表頭下緣 y=15（只在第二、三欄）、表頭下緣 y=30。
        /// </summary>
        private static Box[] Rulings()
        {
            return new[]
            {
                VLine(0, 0, 100), VLine(100, 0, 100), VLine(200, 15, 100), VLine(300, 0, 100),
                HLine(0, 0, 300), HLine(15, 100, 300), HLine(30, 0, 300), HLine(100, 0, 300),
            };
        }

        [Fact]
        public void 由連接的格線找出表格範圍()
        {
            var grid = Assert.Single(LatticeGrid.Find(Rulings()));

            Assert.Equal(new[] { 0.25, 100.25, 200.25, 300.25 }, grid.ColumnLines);
        }

        [Fact]
        public void 只有水平底線時不是格線表格()
        {
            Assert.Empty(LatticeGrid.Find(new[] { HLine(10, 0, 50), HLine(10, 60, 100), HLine(30, 0, 100) }));
        }

        [Fact]
        public void 跨欄表頭加到每個涵蓋的欄位()
        {
            var grid = LatticeGrid.Find(Rulings()).Single();
            var lines = new List<TextLine>
            {
                Line(3, "擔保品@130"),
                Line(18, "編號@20", "名稱@120", "價值@220"),
                Line(40, "0@20", "土地@120", "100@220"),
                Line(53, "1@20", "房屋@120", "200@220"),
            };

            var block = TableBlock.FromGrid(lines, grid);

            Assert.Equal(new[] { "編號", "擔保品 名稱", "擔保品 價值" }, block.Header);
            Assert.Equal(new[] { "0", "土地", "100" }, block.Rows[0]);
            Assert.Equal(2, block.Rows.Count);
        }

        [Fact]
        public void 格內折行的文字併入同一列()
        {
            var grid = LatticeGrid.Find(Rulings()).Single();
            var lines = new List<TextLine>
            {
                Line(18, "編號@20", "名稱@120", "價值@220"),
                Line(40, "0@20", "信錦企業@110", "100@220"),
                Line(53, "股份有限公司@110"),
            };

            var block = TableBlock.FromGrid(lines, grid);

            Assert.Equal(new[] { "0", "信錦企業股份有限公司", "100" }, Assert.Single(block.Rows));
        }
    }
}
