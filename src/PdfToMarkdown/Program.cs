using System;
using System.Reflection;

namespace PdfToMarkdown
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // 目前只印出版本，轉換功能於後續步驟加入
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"PdfToMarkdown {version.ToString(3)}");
            return 0;
        }
    }
}
