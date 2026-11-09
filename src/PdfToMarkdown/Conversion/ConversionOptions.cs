namespace PdfToMarkdown.Conversion
{
    /// <summary>
    /// 轉換選項。
    /// </summary>
    internal sealed class ConversionOptions
    {
        /// <summary>掃描頁影像的輸出資料夾；為 null 時不輸出影像。</summary>
        public string ImageDirectory { get; set; }

        /// <summary>Markdown 中引用影像時，加在檔名前的相對路徑（例如「report_images/」）。</summary>
        public string ImageLinkPrefix { get; set; } = string.Empty;

        /// <summary>Tesseract 語言資料資料夾；為 null 時不進行 OCR。</summary>
        public string TessdataPath { get; set; }

        /// <summary>進度訊息回呼（例如 OCR 頁碼）；可為 null。</summary>
        public System.Action<string> Progress { get; set; }
    }
}
