using System.Collections.Generic;
using System.Text;
using PdfToMarkdown.Layout;
using PdfToMarkdown.Pdf;
using UglyToad.PdfPig;
using static System.FormattableString;

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
            var bodyFontSize = StructureDetector.FindBodyFontSize(pages);
            foreach (var page in pages)
            {
                StructureDetector.Apply(page, bodyFontSize);
            }

            return MarkdownWriter.Write(pages);
        }

        /// <summary>
        /// 診斷用：輸出每頁重建後的行與座標，用來調整版面分析的門檻。
        /// </summary>
        public string DumpLines(string pdfPath)
        {
            var dump = new StringBuilder();

            using (var document = PdfDocument.Open(pdfPath))
            {
                foreach (var pdfPage in document.GetPages())
                {
                    var page = PdfPageReader.Read(pdfPage);
                    dump.AppendLine(Invariant($"=== page {page.Number} ({page.Width:0}x{page.Height:0}){(page.IsScanned ? " scanned" : string.Empty)}"));

                    foreach (var line in LineBuilder.Build(page.Fragments))
                    {
                        var b = line.Bounds;
                        dump.AppendLine(Invariant($"T{b.Top,6:0.0} L{b.Left,6:0.0} R{b.Right,6:0.0} F{line.FontSize,4:0.0} {(line.HasColumnGap ? "G" : " ")} | {line.Text}"));
                    }
                }
            }

            return dump.ToString();
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
