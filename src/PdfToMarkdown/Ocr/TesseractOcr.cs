using System;
using System.Drawing;
using System.IO;
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

        /// <summary>辨識整張影像，回傳純文字。</summary>
        public string RecognizeText(Bitmap bitmap)
        {
            using (var pix = ToPix(bitmap))
            using (var page = _engine.Process(pix))
            {
                return page.GetText();
            }
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
