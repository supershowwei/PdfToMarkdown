using System;
using System.IO;

namespace PdfToMarkdown.Tests
{
    /// <summary>
    /// 測試用的儲存庫路徑。
    /// </summary>
    internal static class RepoPaths
    {
        private static readonly Lazy<string> _root = new Lazy<string>(FindRoot);

        /// <summary>儲存庫根目錄（含 PdfToMarkdown.sln 的資料夾）。</summary>
        public static string Root => _root.Value;

        private static string FindRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "PdfToMarkdown.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("找不到儲存庫根目錄（PdfToMarkdown.sln）");
        }
    }
}
