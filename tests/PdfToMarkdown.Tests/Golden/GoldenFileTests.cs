using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfToMarkdown.Conversion;
using Xunit;

namespace PdfToMarkdown.Tests.Golden
{
    /// <summary>
    /// 以 doc/ 下的樣本 PDF 轉換結果，與 tests/golden/ 下的預期 Markdown 比對。
    /// 轉換邏輯有意變更時，設定環境變數 UPDATE_GOLDEN=1 再執行測試即可更新預期檔，
    /// 並在 Review 時一併檢視 golden 檔的差異。
    /// </summary>
    public class GoldenFileTests
    {
        public static IEnumerable<object[]> SamplePdfs()
        {
            return Directory.GetFiles(Path.Combine(RepoPaths.Root, "doc"), "*.pdf")
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => new object[] { Path.GetFileName(path) });
        }

        [Theory]
        [MemberData(nameof(SamplePdfs))]
        public void 轉換結果與golden檔一致(string pdfFileName)
        {
            var pdfPath = Path.Combine(RepoPaths.Root, "doc", pdfFileName);
            var goldenPath = Path.Combine(RepoPaths.Root, "tests", "golden", Path.ChangeExtension(pdfFileName, ".md"));

            // 掃描影像寫到暫存資料夾，golden 檔只比對 Markdown 中的連結
            var imageDirectory = Path.Combine(Path.GetTempPath(), "PdfToMarkdown.Tests", Path.GetFileNameWithoutExtension(pdfFileName));
            var options = new ConversionOptions { ImageDirectory = imageDirectory, ImageLinkPrefix = "images/" };

            var actual = new PdfToMarkdownConverter().Convert(pdfPath, options);
            Assert.NotEmpty(Directory.GetFiles(imageDirectory, "*.jpg"));

            if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(goldenPath));
                File.WriteAllText(goldenPath, actual, new UTF8Encoding(false));
                return;
            }

            Assert.True(File.Exists(goldenPath), $"找不到 golden 檔：{goldenPath}，請以 UPDATE_GOLDEN=1 產生");

            // 換行字元可能因 git 設定而不同，比對前統一為 \n
            var expected = File.ReadAllText(goldenPath, Encoding.UTF8).Replace("\r\n", "\n");
            Assert.Equal(expected, actual);
        }
    }
}
