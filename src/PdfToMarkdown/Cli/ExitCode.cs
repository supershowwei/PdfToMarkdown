namespace PdfToMarkdown.Cli
{
    /// <summary>
    /// 程式結束代碼。
    /// </summary>
    internal enum ExitCode
    {
        Success = 0,

        /// <summary>參數格式錯誤。</summary>
        InvalidArguments = 1,

        /// <summary>輸入檔不存在。</summary>
        InputNotFound = 2,

        /// <summary>轉換過程發生錯誤。</summary>
        ConversionFailed = 3,
    }
}
