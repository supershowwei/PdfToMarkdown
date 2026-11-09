using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

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
                foreach (var page in document.GetPages())
                {
                    // 每頁以 HTML 註解標記頁碼，方便對照原始 PDF
                    markdown.Append("<!-- page ").Append(page.Number).Append(" -->\n\n");
                    markdown.Append(ContentOrderTextExtractor.GetText(page).Replace("\r\n", "\n").TrimEnd());
                    markdown.Append("\n\n");
                }
            }

            return markdown.ToString();
        }
    }
}
