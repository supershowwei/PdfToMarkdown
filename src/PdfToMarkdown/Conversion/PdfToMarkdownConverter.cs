using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>診斷輸出每頁最多列出的格線數量。</summary>
        private const int MaxDumpedRulings = 20;

        public string Convert(string pdfPath)
        {
            var contents = new List<PageContent>();

            using (var document = PdfDocument.Open(pdfPath))
            {
                foreach (var pdfPage in document.GetPages())
                {
                    contents.Add(PdfPageReader.Read(pdfPage));
                }
            }

            var pages = Analyze(contents);

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
                    dump.AppendLine(Invariant($"=== page {page.Number} ({page.Width:0}x{page.Height:0}){(page.IsScanned ? " scanned" : string.Empty)} rulings={page.Rulings.Count}"));
                    foreach (var ruling in page.Rulings.Take(MaxDumpedRulings))
                    {
                        dump.AppendLine(Invariant($"  ruling {ruling}"));
                    }

                    foreach (var line in LineBuilder.Build(page.Fragments))
                    {
                        var b = line.Bounds;
                        dump.AppendLine(Invariant($"T{b.Top,6:0.0} L{b.Left,6:0.0} R{b.Right,6:0.0} F{line.FontSize,4:0.0} {(line.HasColumnGap ? "G" : " ")} | {line.Text}"));
                    }
                }
            }

            return dump.ToString();
        }

        private static List<PageLayout> Analyze(IReadOnlyList<PageContent> contents)
        {
            var lines = contents
                .Select(page => page.IsScanned
                    ? new List<TextLine>()
                    : PageNumberFilter.Apply(LineBuilder.Build(page.Fragments), page.Height))
                .ToList();

            // 內文右界以相同頁寬的所有頁面一起計算，避免表格多、內文少的頁面判斷失準
            var contentRightByWidth = contents
                .Select((page, index) => (Width: Math.Round(page.Width), Lines: lines[index]))
                .GroupBy(page => page.Width)
                .ToDictionary(
                    group => group.Key,
                    group => ParagraphBuilder.FindContentRight(group.SelectMany(page => ProseCandidates(page.Lines))));

            return contents
                .Select((page, index) =>
                {
                    if (page.IsScanned)
                    {
                        return new PageLayout(page.Number, true, new List<Block>());
                    }

                    var contentRight = contentRightByWidth[Math.Round(page.Width)];
                    return new PageLayout(page.Number, false, BlockBuilder.Build(lines[index], contentRight, page.Rulings), contentRight);
                })
                .ToList();
        }

        /// <summary>表格以外的行。</summary>
        private static IEnumerable<TextLine> ProseCandidates(IReadOnlyList<TextLine> lines)
        {
            var tableLines = new HashSet<int>(TableRegionDetector.Find(lines)
                .SelectMany(region => Enumerable.Range(region.Start, region.End - region.Start + 1)));

            return lines.Where((line, index) => !tableLines.Contains(index));
        }
    }
}
