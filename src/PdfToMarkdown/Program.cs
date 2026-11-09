using System;
using System.IO;
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

            if (!File.Exists(options.InputPath))
            {
                Console.Error.WriteLine($"找不到輸入檔：{options.InputPath}");
                return ExitCode.InputNotFound;
            }

            try
            {
                var converter = new PdfToMarkdownConverter();
                var imageFolder = Path.GetFileNameWithoutExtension(options.OutputPath) + "_images";
                var conversionOptions = new ConversionOptions
                {
                    ImageDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(options.OutputPath)), imageFolder),
                    ImageLinkPrefix = imageFolder + "/",
                    TessdataPath = options.NoOcr ? null : TesseractOcr.FindTessdata(options.TessdataPath),
                    Progress = message => Console.WriteLine(message),
                };

                if (!options.NoOcr && conversionOptions.TessdataPath == null)
                {
                    Console.Error.WriteLine("找不到 Tesseract 語言資料（chi_tra.traineddata），掃描頁將不進行 OCR。可執行 tools/get-tessdata.ps1 下載。");
                }

                var markdown = options.DumpLines
                    ? converter.DumpLines(options.InputPath, conversionOptions)
                    : converter.Convert(options.InputPath, conversionOptions);

                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath));
                Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(options.OutputPath, markdown, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"轉換失敗：{ex.Message}");
                return ExitCode.ConversionFailed;
            }

            Console.WriteLine($"{options.InputPath} -> {options.OutputPath}");
            return ExitCode.Success;
        }
    }
}
