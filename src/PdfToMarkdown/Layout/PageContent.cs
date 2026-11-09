using System.Collections.Generic;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 與 PDF 函式庫無關的頁面內容，文字頁與 OCR 頁共用。
    /// </summary>
    internal sealed class PageContent
    {
        public PageContent(int number, double width, double height, IReadOnlyList<TextFragment> fragments, bool isScanned)
        {
            Number = number;
            Width = width;
            Height = height;
            Fragments = fragments;
            IsScanned = isScanned;
        }

        /// <summary>頁碼（從 1 開始）。</summary>
        public int Number { get; }

        public double Width { get; }

        public double Height { get; }

        public IReadOnlyList<TextFragment> Fragments { get; }

        /// <summary>沒有文字層、只有影像的掃描頁。</summary>
        public bool IsScanned { get; }

        /// <summary>格線（水平或垂直的細長矩形），座標與文字相同。</summary>
        public IReadOnlyList<Box> Rulings { get; set; } = new Box[0];
    }
}
