using System.Collections.Generic;
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
            var pages = new List<PageLayout>();

            using (var document = PdfDocument.Open(pdfPath))
            {
                foreach (var pdfPage in document.GetPages())
                {
                    pages.Add(Analyze(PdfPageReader.Read(pdfPage)));
                }
            }

            ParagraphBuilder.MergeAcrossPages(pages);
            foreach (var page in pages)
            {
                StructureDetector.Apply(page);
            }

            return MarkdownWriter.Write(pages);
        }

        private static PageLayout Analyze(PageContent page)
        {
            if (page.IsScanned)
            {
                return new PageLayout(page.Number, true, new List<Block>());
            }

            var lines = PageNumberFilter.Apply(LineBuilder.Build(page.Fragments), page.Height);
            var contentRight = ParagraphBuilder.FindContentRight(lines);
            return new PageLayout(page.Number, false, ParagraphBuilder.Build(lines, contentRight), contentRight);
        }
    }
}
