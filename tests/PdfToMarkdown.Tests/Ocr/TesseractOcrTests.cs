using System.IO;
using System.Linq;
using PdfToMarkdown.Ocr;
using PdfToMarkdown.Pdf;
using UglyToad.PdfPig;
using Xunit;

namespace PdfToMarkdown.Tests.Ocr
{
    /// <summary>
    /// OCR 冒煙測試：需要 tessdata/ 下的語言資料（tools/get-tessdata.ps1），沒有時直接略過。
    /// </summary>
    public class TesseractOcrTests
    {
        [Fact]
        public void 辨識資產負債表掃描頁()
        {
            var tessdata = Path.Combine(RepoPaths.Root, "tessdata");
            if (TesseractOcr.FindTessdata(tessdata) == null)
            {
                return;
            }

            string text;
            using (var document = PdfDocument.Open(Path.Combine(RepoPaths.Root, "doc", "202601_1582.pdf")))
            using (var bitmap = ScannedPageRenderer.Render(document.GetPage(5)))
            using (var ocr = new TesseractOcr(tessdata))
            {
                text = string.Join(" ", ocr.Recognize(bitmap, ScannedPageRenderer.Dpi).Select(f => f.Text));
            }

            Assert.Contains("流動", text);
            Assert.Contains("2,782,346", text);
        }

        [Fact]
        public void 指定的資料夾沒有語言資料時不採用()
        {
            var missing = Path.Combine(Path.GetTempPath(), "no-such-tessdata");

            Assert.NotEqual(missing, TesseractOcr.FindTessdata(missing));
        }
    }
}
