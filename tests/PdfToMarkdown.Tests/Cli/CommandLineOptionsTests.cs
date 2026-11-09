using PdfToMarkdown.Cli;
using Xunit;

namespace PdfToMarkdown.Tests.Cli
{
    public class CommandLineOptionsTests
    {
        [Fact]
        public void 只給輸入檔_輸出路徑預設為同名md()
        {
            var ok = CommandLineOptions.TryParse(new[] { @"doc\a.pdf" }, out var options, out _);

            Assert.True(ok);
            Assert.Equal(@"doc\a.pdf", options.InputPath);
            Assert.Equal(@"doc\a.md", options.OutputPath);
        }

        [Theory]
        [InlineData("-o")]
        [InlineData("--output")]
        public void 指定輸出路徑(string flag)
        {
            var ok = CommandLineOptions.TryParse(new[] { "a.pdf", flag, @"out\b.md" }, out var options, out _);

            Assert.True(ok);
            Assert.Equal(@"out\b.md", options.OutputPath);
        }

        [Fact]
        public void 輸出選項可放在輸入檔之前()
        {
            var ok = CommandLineOptions.TryParse(new[] { "-o", "b.md", "a.pdf" }, out var options, out _);

            Assert.True(ok);
            Assert.Equal("a.pdf", options.InputPath);
            Assert.Equal("b.md", options.OutputPath);
        }

        [Fact]
        public void 預設輸出路徑時標示為未指定()
        {
            CommandLineOptions.TryParse(new[] { "doc" }, out var defaults, out _);
            CommandLineOptions.TryParse(new[] { "doc", "-o", "out" }, out var specified, out _);

            Assert.False(defaults.IsOutputSpecified);
            Assert.True(specified.IsOutputSpecified);
        }

        [Fact]
        public void OCR選項()
        {
            var ok = CommandLineOptions.TryParse(new[] { "a.pdf", "--tessdata", @"tessdata-dir", "--no-ocr" }, out var options, out _);

            Assert.True(ok);
            Assert.Equal(@"tessdata-dir", options.TessdataPath);
            Assert.True(options.NoOcr);
        }

        [Fact]
        public void 診斷選項()
        {
            var ok = CommandLineOptions.TryParse(new[] { "a.pdf", "--dump-lines" }, out var options, out _);

            Assert.True(ok);
            Assert.True(options.DumpLines);
        }

        [Theory]
        [InlineData("-h")]
        [InlineData("--help")]
        public void 說明選項不需要輸入檔(string flag)
        {
            var ok = CommandLineOptions.TryParse(new[] { flag }, out var options, out _);

            Assert.True(ok);
            Assert.True(options.ShowHelp);
        }

        [Theory]
        [InlineData("-v")]
        [InlineData("--version")]
        public void 版本選項不需要輸入檔(string flag)
        {
            var ok = CommandLineOptions.TryParse(new[] { flag }, out var options, out _);

            Assert.True(ok);
            Assert.True(options.ShowVersion);
        }

        [Theory]
        [InlineData(new string[0], "未指定輸入檔")]
        [InlineData(new[] { "a.pdf", "-o" }, "-o 需要指定輸出路徑")]
        [InlineData(new[] { "a.pdf", "-o", " " }, "-o 需要指定輸出路徑")]
        [InlineData(new[] { "a.pdf", "-o", "b.md", "-o", "c.md" }, "輸出路徑重複指定")]
        [InlineData(new[] { "a.pdf", "b.pdf" }, "只能指定一個輸入檔")]
        [InlineData(new[] { "a.pdf", "--unknown" }, "未知的選項：--unknown")]
        public void 格式錯誤時回傳錯誤訊息(string[] args, string expectedError)
        {
            var ok = CommandLineOptions.TryParse(args, out _, out var error);

            Assert.False(ok);
            Assert.Equal(expectedError, error);
        }
    }
}
