using System;
using System.IO;

namespace PdfToMarkdown.Cli
{
    /// <summary>
    /// 命令列參數解析結果。
    /// </summary>
    internal sealed class CommandLineOptions
    {
        public const string Usage =
            "用法：PdfToMarkdown <input.pdf> [-o <output.md>]\n" +
            "\n" +
            "選項：\n" +
            "  -o, --output <path>   輸出 Markdown 檔路徑（預設為輸入檔同名 .md）\n" +
            "  -h, --help            顯示說明\n" +
            "  -v, --version         顯示版本";

        public string InputPath { get; private set; }

        public string OutputPath { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowVersion { get; private set; }

        /// <summary>
        /// 解析參數；格式錯誤時回傳 false 並以 <paramref name="error"/> 說明原因。
        /// 只檢查格式，不檢查檔案是否存在。
        /// </summary>
        public static bool TryParse(string[] args, out CommandLineOptions options, out string error)
        {
            options = new CommandLineOptions();
            error = null;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;

                    case "-v":
                    case "--version":
                        options.ShowVersion = true;
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                        {
                            error = $"{arg} 需要指定輸出路徑";
                            return false;
                        }

                        if (options.OutputPath != null)
                        {
                            error = "輸出路徑重複指定";
                            return false;
                        }

                        options.OutputPath = args[++i];
                        break;

                    default:
                        if (arg.StartsWith("-", StringComparison.Ordinal))
                        {
                            error = $"未知的選項：{arg}";
                            return false;
                        }

                        if (options.InputPath != null)
                        {
                            error = "只能指定一個輸入檔";
                            return false;
                        }

                        options.InputPath = arg;
                        break;
                }
            }

            if (options.ShowHelp || options.ShowVersion)
            {
                return true;
            }

            if (options.InputPath == null)
            {
                error = "未指定輸入檔";
                return false;
            }

            if (options.OutputPath == null)
            {
                options.OutputPath = Path.ChangeExtension(options.InputPath, ".md");
            }

            return true;
        }
    }
}
