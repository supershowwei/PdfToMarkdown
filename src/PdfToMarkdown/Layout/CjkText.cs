namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 中日韓（CJK）字元判斷。
    /// </summary>
    internal static class CjkText
    {
        /// <summary>
        /// 是否為 CJK 文字或全形標點；這類字元之間通常不需要空白。
        /// </summary>
        public static bool IsCjk(char c)
        {
            return (c >= '一' && c <= '鿿')   // CJK 統一表意文字
                || (c >= '㐀' && c <= '䶿')   // CJK 擴充 A
                || (c >= '豈' && c <= '﫿')   // CJK 相容表意文字
                || (c >= '　' && c <= '〿')   // CJK 符號與標點（、。「」等）
                || (c >= '㄀' && c <= 'ㄯ')   // 注音符號
                || (c >= '＀' && c <= '￯');  // 全形字元（（）：～等）
        }
    }
}
