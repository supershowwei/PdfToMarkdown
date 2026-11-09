using System;
using System.IO;
using System.Reflection;
using System.Text;
using PdfToMarkdown.Cli;
using PdfToMarkdown.Conversion;

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
                };

                var markdown = options.DumpLines
                    ? converter.DumpLines(options.InputPath)
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
