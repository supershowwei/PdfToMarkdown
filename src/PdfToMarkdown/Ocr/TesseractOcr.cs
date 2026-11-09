using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PdfToMarkdown.Layout;
using Tesseract;

namespace PdfToMarkdown.Ocr
{
    /// <summary>
    /// Tesseract OCR 引擎包裝。
    /// </summary>
    internal sealed class TesseractOcr : IDisposable
    {
        /// <summary>辨識語言：繁體中文為主，英文與數字為輔。</summary>
        public const string Languages = "chi_tra+eng";

        /// <summary>文字行框是墨跡範圍，高度約為字級的此倍數（數字約 0.7、中文約 0.9）。</summary>
        private const double LineHeightToFontSize = 0.8;

        /// <summary>可帶 $ 與括號的整數金額。</summary>
        private static readonly System.Text.RegularExpressions.Regex _amount = new System.Text.RegularExpressions.Regex(
            @"^(?<prefix>[(（$]*)(?<digits>\d[\d,]*\d)(?<suffix>[)）]*)$");

        private readonly TesseractEngine _engine;

        public TesseractOcr(string tessdataPath)
        {
            _engine = new TesseractEngine(tessdataPath, Languages, EngineMode.LstmOnly);
        }

        /// <summary>
        /// 依序尋找語言資料資料夾：指定路徑、TESSDATA_PREFIX 環境變數、執行檔旁、目前目錄；找不到回傳 null。
        /// </summary>
        public static string FindTessdata(string preferred)
        {
            var candidates = new[]
            {
                preferred,
                Environment.GetEnvironmentVariable("TESSDATA_PREFIX"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(Path.Combine(candidate, "chi_tra.traineddata")))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// 辨識整張影像，回傳以「點」為單位、原點在左上的字詞片段，可直接交給版面分析。
        /// </summary>
        /// <param name="bitmap">影像。</param>
        /// <param name="dpi">影像解析度，用來把像素換算成點。</param>
        public List<TextFragment> Recognize(Bitmap bitmap, double dpi)
        {
            var toPoints = 72 / dpi;
            var words = new List<(string Text, Rect Word, Rect Line)>();

            using (var prepared = OcrPreprocessor.Prepare(bitmap))
            using (var pix = ToPix(prepared))
            using (var page = _engine.Process(pix))
            using (var iterator = page.GetIterator())
            {
                iterator.Begin();
                do
                {
                    var text = iterator.GetText(PageIteratorLevel.Word)?.Trim();
                    if (!string.IsNullOrEmpty(text)
                        && iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var word)
                        && iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var line))
                    {
                        words.Add((text, word, line));
                    }
                }
                while (iterator.Next(PageIteratorLevel.Word));
            }

            if (words.Count == 0)
            {
                return new List<TextFragment>();
            }

            // 各行框高度差異很大（受標點、底線影響），整頁採用行高中位數推估的字級
            var lineHeights = words.Select(w => w.Line.Height).OrderBy(h => h).ToList();
            var fontSize = lineHeights[lineHeights.Count / 2] * toPoints / LineHeightToFontSize;

            // 垂直範圍採用整行的框：同一行字詞高度一致，「一」這類扁平字才不會被分到別行
            return words
                .Select(w => new TextFragment(
                    FixThousandsSeparators(w.Text),
                    new Box(w.Word.X1 * toPoints, w.Line.Y1 * toPoints, w.Word.X2 * toPoints, w.Line.Y2 * toPoints),
                    fontSize,
                    spaceBefore: true))
                .ToList();
        }

        /// <summary>
        /// OCR 常漏掉千分位逗號（例如「4764,142」）：已含逗號但分組不正確的整數，依三位重新分組。
        /// 只處理已含逗號的數字，避免改動代碼、郵遞區號等原本就沒有逗號的數字。
        /// </summary>
        public static string FixThousandsSeparators(string text)
        {
            var match = _amount.Match(text);
            if (!match.Success || !match.Groups["digits"].Value.Contains(","))
            {
                return text;
            }

            var digits = match.Groups["digits"].Value.Replace(",", string.Empty);
            var grouped = long.TryParse(digits, out var value) ? value.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture) : null;
            if (grouped == null || digits.StartsWith("0", StringComparison.Ordinal))
            {
                return text;
            }

            return match.Groups["prefix"].Value + grouped + match.Groups["suffix"].Value;
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        /// <summary>經由 PNG 轉成 Tesseract 的 Pix（避免依賴額外的轉換套件）。</summary>
        private static Pix ToPix(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return Pix.LoadFromMemory(stream.ToArray());
            }
        }
    }
}
