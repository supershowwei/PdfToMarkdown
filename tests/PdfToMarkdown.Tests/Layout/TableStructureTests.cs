using System.Linq;
using PdfToMarkdown.Layout;
using Xunit;

namespace PdfToMarkdown.Tests.Layout
{
    public class TableStructureTests
    {
        private const double FontSize = 10;

        /// <summary>
        /// 建立一列；cells 中的文字以「@X」標示左緣，空字串代表空白格。每字寬一個字級。
        /// </summary>
        private static TableRow Row(params string[] cells)
        {
            return RowWith(hasColumnGap: true, isLetterSpaced: false, cells);
        }

        private static TableRow RowWith(bool hasColumnGap, bool isLetterSpaced, params string[] cells)
        {
            var texts = cells.Select(c => c.Split('@')[0]).ToArray();
            var lefts = cells.Select(c => c.Contains("@") ? double.Parse(c.Split('@')[1]) : (double?)null).ToArray();
            var rights = cells.Select((c, i) => lefts[i] + texts[i].Length * FontSize).ToArray();
            return new TableRow(texts, lefts, rights, hasColumnGap, isLetterSpaced, FontSize);
        }

        [Fact]
        public void 項目名稱折行併入下一列()
        {
            var rows = TableRowMerger.Merge(new[]
            {
                Row("備抵損失（存續期間預期@0", ""),
                Row("信用損失）@10", "( 1,760 )@100"),
            });

            var row = Assert.Single(rows);
            Assert.Equal(new[] { "備抵損失（存續期間預期信用損失）", "( 1,760 )" }, row.Cells);
        }

        [Fact]
        public void 縮排兩個字寬的子項目不合併()
        {
            var rows = TableRowMerger.Merge(new[]
            {
                Row("非衍生金融資產@0", ""),
                Row("－國內上市@20", "100@100"),
            });

            Assert.Equal(2, rows.Count);
        }

        [Fact]
        public void 同縮排的下一個項目不合併()
        {
            var rows = TableRowMerger.Merge(new[]
            {
                Row("－國內興櫃股票@20", "100@100"),
                Row("－國內非上市@20", ""),
            });

            Assert.Equal(2, rows.Count);
        }

        [Fact]
        public void 各欄文字延續併入上一列()
        {
            var rows = TableRowMerger.Merge(new[]
            {
                Row("敬得科技@0", "精密模具之鑄造@100", "75.94@200"),
                Row("", "加工銷售及買賣@100", ""),
            });

            var row = Assert.Single(rows);
            Assert.Equal("精密模具之鑄造加工銷售及買賣", row.Cells[1]);
        }

        [Fact]
        public void 含數值的列不視為延續()
        {
            var rows = TableRowMerger.Merge(new[]
            {
                Row("", "$ 100@100"),
                Row("", "$ 200@100"),
            });

            Assert.Equal(2, rows.Count);
        }

        [Fact]
        public void 編號項目允許凸排折行()
        {
            var rows = TableRowMerger.Merge(new[]
            {
                Row("(五) 重大會計判斷、估計及假設不確@0", "15@300"),
                Row("定性之主要來源@30", ""),
            });

            Assert.Equal("(五) 重大會計判斷、估計及假設不確定性之主要來源", Assert.Single(rows).Cells[0]);
        }

        [Fact]
        public void 多列表頭合併並移除字間空白()
        {
            var (header, body) = TableHeaderBuilder.Split(new[]
            {
                Row("", "逾 期@100"),
                Row("未 逾 期@0", "1 ～ 30 天@100"),
                Row("總帳面金額@0", "$ 41,655@100"),
            });

            Assert.Equal(new[] { "未逾期", "逾期 1 ～ 30 天" }, header);
            Assert.Single(body);
        }

        [Fact]
        public void 只有第一欄的分類列屬於資料列()
        {
            var (header, body) = TableHeaderBuilder.Split(new[]
            {
                Row("", "115年3月31日@100"),
                Row("金融資產－流動@0", ""),
                Row("股票@0", "$ 316,694@100"),
            });

            Assert.Equal(new[] { "", "115年3月31日" }, header);
            Assert.Equal(2, body.Count);
        }

        [Fact]
        public void 沒有表頭時回傳null()
        {
            var (header, body) = TableHeaderBuilder.Split(new[]
            {
                Row("折舊費用@0", "$ 67,888@100"),
                Row("攤銷費用@0", "$ 1,593@100"),
            });

            Assert.Null(header);
            Assert.Equal(2, body.Count);
        }

        [Fact]
        public void 跨欄的分散對齊表頭加到每個涵蓋的欄位()
        {
            var (header, _) = TableHeaderBuilder.Split(new[]
            {
                RowWith(false, true, "", "所 持@100", "股 權@200"),
                Row("", "115年@100", "114年@200"),
                Row("廣進@0", "100@100", "100@200"),
            });

            Assert.Equal(new[] { "", "所持股權 115年", "所持股權 114年" }, header);
        }

        [Fact]
        public void 只有表頭的欄位併入最近的資料欄()
        {
            var (header, body) = TableHeaderBuilder.Split(new[]
            {
                Row("項目@0", "頁@95", "次@125"),
                Row("一、封面@0", "1@110", ""),
                Row("二、目錄@0", "2@110", ""),
            });

            Assert.Equal(new[] { "項目", "頁次" }, header);
            Assert.All(body, row => Assert.Equal(2, row.Cells.Length));
        }

        [Theory]
        [InlineData("$ 2,683,856", true)]
        [InlineData("( 1,760 )", true)]
        [InlineData("4.22%", true)]
        [InlineData("-", true)]
        [InlineData("$ 19,609 $ 6,121", true)]
        [InlineData("115年3月31日", false)]
        [InlineData("註 1", false)]
        [InlineData("", false)]
        public void 數值判斷(string text, bool expected)
        {
            Assert.Equal(expected, TableRow.IsNumeric(text));
        }
    }
}
