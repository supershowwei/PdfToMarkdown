namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 帶座標的文字片段。PDF 來源為單一字元，OCR 來源可為一個字詞。
    /// </summary>
    internal sealed class TextFragment
    {
        public TextFragment(string text, Box bounds, double fontSize, bool spaceBefore = false, double? confidence = null)
        {
            Text = text;
            Bounds = bounds;
            FontSize = fontSize;
            SpaceBefore = spaceBefore;
            Confidence = confidence;
        }

        public string Text { get; }

        public Box Bounds { get; }

        /// <summary>字級（點）；OCR 來源以字高估算。</summary>
        public double FontSize { get; }

        /// <summary>來源中此片段前面原本有空白字元（PDF 內容串流順序）。</summary>
        public bool SpaceBefore { get; }

        /// <summary>OCR 辨識信心（0～1）；PDF 文字層為 null。</summary>
        public double? Confidence { get; }

        public override string ToString()
        {
            return $"{Text} {Bounds}";
        }
    }
}
