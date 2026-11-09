using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using PdfToMarkdown.Cli;
using PdfToMarkdown.Conversion;
using PdfToMarkdown.Ocr;

namespace PdfToMarkdown
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            return (int)Run(args);
        }

        private static ExitCode Run(string[] args)
        {
            if (!CommandLineOptions.TryParse(args, out var options, out var error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine();
                Console.Error.WriteLine(CommandLineOptions.Usage);
                return ExitCode.InvalidArguments;
            }

            if (options.ShowHelp)
            {
                Console.WriteLine(CommandLineOptions.Usage);
                return ExitCode.Success;
            }

            if (options.ShowVersion)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Console.WriteLine($"PdfToMarkdown {version.ToString(3)}");
                return ExitCode.Success;
            }

            var jobs = PlanJobs(options);
            if (jobs == null)
            {
                Console.Error.WriteLine($"找不到輸入檔或資料夾：{options.InputPath}");
                return ExitCode.InputNotFound;
            }

            var tessdata = options.NoOcr ? null : TesseractOcr.FindTessdata(options.TessdataPath);
            if (!options.NoOcr && tessdata == null)
            {
                Console.Error.WriteLine("找不到 Tesseract 語言資料（chi_tra.traineddata），掃描頁將不進行 OCR。可執行 tools/get-tessdata.ps1 下載。");
            }

            var failed = 0;
            for (var i = 0; i < jobs.Count; i++)
            {
                var (input, output) = jobs[i];
                var prefix = jobs.Count > 1 ? $"[{i + 1}/{jobs.Count}] " : string.Empty;

                if (!ConvertFile(input, output, tessdata, options.DumpLines, prefix))
                {
                    failed++;
                }
            }

            if (jobs.Count > 1)
            {
                Console.WriteLine($"完成：成功 {jobs.Count - failed} 個，失敗 {failed} 個");
            }

            return failed == 0 ? ExitCode.Success : ExitCode.ConversionFailed;
        }

        /// <summary>
        /// 決定要轉換的檔案與輸出路徑；輸入不存在時回傳 null。
        /// 輸入為資料夾時轉換其中所有 PDF，輸出到 -o 指定的資料夾（預設為輸入資料夾）。
        /// </summary>
        private static List<(string Input, string Output)> PlanJobs(CommandLineOptions options)
        {
            if (File.Exists(options.InputPath))
            {
                return new List<(string, string)> { (options.InputPath, options.OutputPath) };
            }

            if (!Directory.Exists(options.InputPath))
            {
                return null;
            }

            var outputDirectory = options.IsOutputSpecified ? options.OutputPath : options.InputPath;
            return Directory.GetFiles(options.InputPath, "*.pdf")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => (path, Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(path) + ".md")))
                .ToList();
        }

        /// <summary>轉換單一檔案；失敗時印出原因並回傳 false，不中斷批次。</summary>
        private static bool ConvertFile(string input, string output, string tessdata, bool dumpLines, string prefix)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"{prefix}{input}");

            try
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(output));
                var imageFolder = Path.GetFileNameWithoutExtension(output) + "_images";
                var conversionOptions = new ConversionOptions
                {
                    ImageDirectory = Path.Combine(outputDirectory, imageFolder),
                    ImageLinkPrefix = imageFolder + "/",
                    TessdataPath = tessdata,
                    Progress = message => Console.WriteLine($"  {message}"),
                };

                var converter = new PdfToMarkdownConverter();
                var markdown = dumpLines
                    ? converter.DumpLines(input, conversionOptions)
                    : converter.Convert(input, conversionOptions);

                Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(output, markdown, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  轉換失敗：{ex.Message}");
                return false;
            }

            Console.WriteLine($"  -> {output}（{stopwatch.Elapsed.TotalSeconds:0.0} 秒）");
            return true;
        }
    }
}
