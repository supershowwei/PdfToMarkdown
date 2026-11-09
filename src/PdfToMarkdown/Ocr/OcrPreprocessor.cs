using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PdfToMarkdown.Ocr
{
    /// <summary>
    /// OCR 前的影像處理：轉灰階、去除紅色印章、擦除表格底線。
    /// </summary>
    /// <remarks>
    /// 不自行二值化：固定門檻會讓細筆畫斷裂而降低中文辨識率，交給 Tesseract 內建的 Otsu 處理。
    /// </remarks>
    internal static class OcrPreprocessor
    {
        /// <summary>紅色印章判斷：紅色分量比綠、藍分量高出此值以上。</summary>
        private const int StampRedDominance = 50;

        /// <summary>偵測底線用的門檻（0～255 灰階），低於此值視為黑。</summary>
        private const int BlackThreshold = 160;

        /// <summary>水平黑線至少這麼長（像素，約 0.4 公分 @300DPI）才視為底線並擦除。</summary>
        private const int MinUnderlineLength = 50;

        /// <summary>底線最大粗細（像素）；文字筆畫較粗或較短，不會被擦除。</summary>
        private const int MaxUnderlineThickness = 5;

        /// <summary>回傳處理後的新灰階影像。</summary>
        public static Bitmap Prepare(Bitmap source)
        {
            var width = source.Width;
            var height = source.Height;
            var gray = ToGray(source);

            var black = new bool[gray.Length];
            for (var i = 0; i < gray.Length; i++)
            {
                black[i] = gray[i] < BlackThreshold;
            }

            var erased = RemoveUnderlines(black, width, height);
            for (var i = 0; i < gray.Length; i++)
            {
                if (erased[i])
                {
                    gray[i] = 255;
                }
            }

            return ToBitmap(gray, width, height, source.HorizontalResolution, source.VerticalResolution);
        }

        /// <summary>轉灰階；紅色（印章）轉為白色。</summary>
        private static byte[] ToGray(Bitmap source)
        {
            var width = source.Width;
            var height = source.Height;
            var gray = new byte[width * height];

            using (var rgb = source.Clone(new Rectangle(0, 0, width, height), PixelFormat.Format24bppRgb))
            {
                var data = rgb.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    var row = new byte[data.Stride];
                    for (var y = 0; y < height; y++)
                    {
                        Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Stride);
                        for (var x = 0; x < width; x++)
                        {
                            int b = row[x * 3], g = row[x * 3 + 1], r = row[x * 3 + 2];
                            var isStamp = r - g > StampRedDominance && r - b > StampRedDominance;
                            gray[y * width + x] = isStamp ? (byte)255 : (byte)((r * 299 + g * 587 + b * 114) / 1000);
                        }
                    }
                }
                finally
                {
                    rgb.UnlockBits(data);
                }
            }

            return gray;
        }

        /// <summary>
        /// 找出細長的水平黑線（會計格式的單、雙底線）：
        /// 找出夠長的水平黑色線段，若其上下方在粗細範圍外都是白色，表示是線而非文字筆畫。
        /// </summary>
        /// <returns>應擦除的像素。</returns>
        private static bool[] RemoveUnderlines(bool[] black, int width, int height)
        {
            var erased = new bool[black.Length];

            for (var y = 0; y < height; y++)
            {
                var x = 0;
                while (x < width)
                {
                    if (!black[y * width + x])
                    {
                        x++;
                        continue;
                    }

                    var start = x;
                    while (x < width && black[y * width + x])
                    {
                        x++;
                    }

                    if (x - start >= MinUnderlineLength)
                    {
                        MarkLineRun(black, erased, width, height, start, x, y);
                    }
                }
            }

            return erased;
        }

        /// <summary>
        /// 標記 [start, end) 這段水平線在粗細範圍內的黑點；與文字筆畫相連的直行（向上或向下延伸超過粗細）保留。
        /// </summary>
        private static void MarkLineRun(bool[] black, bool[] erased, int width, int height, int start, int end, int y)
        {
            for (var x = start; x < end; x++)
            {
                var top = y;
                while (top > 0 && black[(top - 1) * width + x])
                {
                    top--;
                }

                var bottom = y;
                while (bottom < height - 1 && black[(bottom + 1) * width + x])
                {
                    bottom++;
                }

                if (bottom - top + 1 <= MaxUnderlineThickness)
                {
                    for (var yy = top; yy <= bottom; yy++)
                    {
                        erased[yy * width + x] = true;
                    }
                }
            }
        }

        private static Bitmap ToBitmap(byte[] gray, int width, int height, float dpiX, float dpiY)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            bitmap.SetResolution(dpiX, dpiY);
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                var row = new byte[data.Stride];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var value = gray[y * width + x];
                        row[x * 3] = row[x * 3 + 1] = row[x * 3 + 2] = value;
                    }

                    Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, data.Stride);
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }
    }
}
