using System.Text;
using PdfToMarkdown.Layout;
using PdfToMarkdown.Pdf;
using UglyToad.PdfPig;

namespace PdfToMarkdown.Conversion
{
    /// <summary>
    /// 將 PDF 轉換為 Markdown 文字。
    /// </summary>
    internal sealed class PdfToMarkdownConverter
    {
        public string Convert(string pdfPath)
        {
            var markdown = new StringBuilder();

            using (var document = PdfDocument.Open(pdfPath))
            {
                foreach (var pdfPage in document.GetPages())
                {
                    var page = PdfPageReader.Read(pdfPage);

                    // 每頁以 HTML 註解標記頁碼，方便對照原始 PDF
                    markdown.Append("<!-- page ").Append(page.Number).Append(" -->\n\n");

                    if (page.IsScanned)
                    {
                        markdown.Append("> 本頁為掃描影像，尚未進行文字辨識。\n\n");
                        continue;
                    }

                    var lines = PageNumberFilter.Apply(LineBuilder.Build(page.Fragments), page.Height);
                    foreach (var line in lines)
                    {
                        markdown.Append(line.Text).Append('\n');
                    }

                    markdown.Append('\n');
                }
            }

            return markdown.ToString();
        }
    }
}
