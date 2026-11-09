namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 帶座標的文字片段。PDF 來源為單一字元，OCR 來源可為一個字詞。
    /// </summary>
    internal sealed class TextFragment
    {
        public TextFragment(string text, Box bounds, double fontSize)
        {
            Text = text;
            Bounds = bounds;
            FontSize = fontSize;
        }

        public string Text { get; }

        public Box Bounds { get; }

        /// <summary>字級（點）；OCR 來源以字高估算。</summary>
        public double FontSize { get; }

        public override string ToString()
        {
            return $"{Text} {Bounds}";
        }
    }
}
