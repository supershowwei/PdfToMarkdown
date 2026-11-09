using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using PdfToMarkdown.Layout;
using PdfToMarkdown.Ocr;
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

        /// <summary>OCR 信心低於此值的字詞視為需要核對。</summary>
        private const double LowConfidence = 0.6;

        /// <summary>掃描影像的 JPEG 品質。</summary>
        private const long JpegQuality = 85;

        public string Convert(string pdfPath, ConversionOptions options = null)
        {
            options = options ?? new ConversionOptions();
            var imageLinks = new Dictionary<int, string>();
            var contents = ReadPages(pdfPath, options, imageLinks);

            var pages = Analyze(contents);
            foreach (var page in pages)
            {
                page.ImageLink = imageLinks.TryGetValue(page.Number, out var link) ? link : null;
            }

            ParagraphBuilder.MergeAcrossPages(pages);
            var bodyFontSize = StructureDetector.FindBodyFontSize(pages.Where(page => !page.IsScanned));
            foreach (var page in pages)
            {
                // OCR 推估的字級不準，掃描頁不依字級判斷標題
                StructureDetector.Apply(page, page.IsScanned ? 0 : bodyFontSize);
            }

            return MarkdownWriter.Write(pages);
        }

        /// <summary>
        /// 讀取每頁內容；掃描頁視選項輸出影像（記錄於 <paramref name="imageLinks"/>）並以 OCR 字詞取代內容。
        /// </summary>
        private static List<PageContent> ReadPages(string pdfPath, ConversionOptions options, IDictionary<int, string> imageLinks)
        {
            var contents = new List<PageContent>();

            using (var ocr = options.TessdataPath == null ? null : new TesseractOcr(options.TessdataPath))
            using (var document = PdfDocument.Open(pdfPath))
            {
                foreach (var pdfPage in document.GetPages())
                {
                    var content = PdfPageReader.Read(pdfPage);
                    contents.Add(content);

                    if (!content.IsScanned || (options.ImageDirectory == null && ocr == null))
                    {
                        continue;
                    }

                    using (var bitmap = ScannedPageRenderer.Render(pdfPage))
                    {
                        if (options.ImageDirectory != null)
                        {
                            imageLinks[content.Number] = options.ImageLinkPrefix + SaveJpeg(bitmap, content.Number, options.ImageDirectory);
                        }

                        if (ocr != null)
                        {
                            // 以 OCR 字詞取代掃描頁內容，之後與文字頁走相同的版面分析
                            options.Progress?.Invoke($"OCR 第 {content.Number} 頁");
                            var toPoints = 72 / ScannedPageRenderer.Dpi;
                            contents[contents.Count - 1] = new PageContent(
                                content.Number,
                                bitmap.Width * toPoints,
                                bitmap.Height * toPoints,
                                ocr.Recognize(bitmap, ScannedPageRenderer.Dpi),
                                isScanned: false)
                            {
                                IsOcr = true,
                            };
                        }
                    }
                }
            }

            return contents;
        }

        /// <summary>
        /// 將掃描頁影像存成 JPEG，回傳檔名（例如「p05.jpg」）。
        /// </summary>
        private static string SaveJpeg(Bitmap bitmap, int pageNumber, string directory)
        {
            Directory.CreateDirectory(directory);
            var fileName = Invariant($"p{pageNumber:00}.jpg");

            using (var parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JpegQuality);
                var jpeg = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                bitmap.Save(Path.Combine(directory, fileName), jpeg, parameters);
            }

            return fileName;
        }

        /// <summary>
        /// 診斷用：輸出每頁重建後的行與座標，用來調整版面分析的門檻。
        /// </summary>
        public string DumpLines(string pdfPath, ConversionOptions options = null)
        {
            var dump = new StringBuilder();
            // 診斷輸出不需要存影像，只沿用 OCR 設定
            var readOptions = new ConversionOptions { TessdataPath = options?.TessdataPath, Progress = options?.Progress };

            foreach (var page in ReadPages(pdfPath, readOptions, new Dictionary<int, string>()))
            {
                dump.AppendLine(Invariant($"=== page {page.Number} ({page.Width:0}x{page.Height:0}){(page.IsScanned ? " scanned" : string.Empty)}{(page.IsOcr ? " ocr" : string.Empty)} rulings={page.Rulings.Count}"));
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
                .Select((page, index) => (Key: ContentRightKey(page), Lines: lines[index]))
                .GroupBy(page => page.Key)
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

                    var contentRight = contentRightByWidth[ContentRightKey(page)];
                    var blocks = BlockBuilder.Build(lines[index], contentRight, page.Rulings);
                    var layout = new PageLayout(page.Number, page.IsOcr, blocks, contentRight);
                    var confidences = page.Fragments.Where(f => f.Confidence.HasValue).Select(f => f.Confidence.Value).ToList();
                    if (confidences.Count > 0)
                    {
                        layout.OcrConfidence = confidences.Average();
                        layout.LowConfidenceWords = confidences.Count(c => c < LowConfidence);
                    }

                    return layout;
                })
                .ToList();
        }

        /// <summary>內文右界依頁寬分組計算；OCR 頁的座標來自影像，另成一組。</summary>
        private static string ContentRightKey(PageContent page)
        {
            return Invariant($"{Math.Round(page.Width)}{(page.IsOcr ? "-ocr" : string.Empty)}");
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
