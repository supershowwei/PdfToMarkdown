using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Tokens;

namespace PdfToMarkdown.Pdf
{
    /// <summary>
    /// 將掃描頁的影像合成為一張轉正後的點陣圖。
    /// </summary>
    /// <remarks>
    /// 樣本的掃描頁有兩種：
    /// 單張 JPEG（可能帶 /Rotate）；或「分層壓縮」——低解析度 JPEG 底圖上疊多張高解析度黑白遮罩（CCITT），
    /// 文字主要在遮罩層。因此依各影像的擺放位置畫到同一張畫布上。
    /// </remarks>
    internal static class ScannedPageRenderer
    {
        /// <summary>輸出解析度（DPI）；樣本掃描解析度為 300。</summary>
        public const double Dpi = 300;

        public static Bitmap Render(Page page)
        {
            var media = page.MediaBox.Bounds;
            var scale = Dpi / 72;
            var width = (int)Math.Round(media.Width * scale);
            var height = (int)Math.Round(media.Height * scale);

            var canvas = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            canvas.SetResolution((float)Dpi, (float)Dpi);

            using (var graphics = Graphics.FromImage(canvas))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                foreach (var image in page.GetImages())
                {
                    using (var bitmap = Decode(image))
                    {
                        if (bitmap == null)
                        {
                            continue;
                        }

                        // PDF 座標原點在左下，畫布原點在左上
                        var (left, bottom, right, top) = ToUnrotated(image.BoundingBox, page.Rotation.Value, media.Width, media.Height);
                        var destination = new RectangleF(
                            (float)((left - media.Left) * scale),
                            (float)((media.Top - top) * scale),
                            (float)((right - left) * scale),
                            (float)((top - bottom) * scale));
                        graphics.DrawImage(bitmap, destination);
                    }
                }
            }

            Rotate(canvas, page.Rotation.Value);
            return canvas;
        }

        /// <summary>
        /// PdfPig 對有 /Rotate 的頁面，影像框是以轉正後的顯示座標回報；換算回未旋轉的頁面座標。
        /// </summary>
        /// <param name="box">影像框（可能上下顛倒，取四邊的極值）。</param>
        /// <param name="degrees">頁面 /Rotate 角度（順時針）。</param>
        /// <param name="width">未旋轉頁面寬度。</param>
        /// <param name="height">未旋轉頁面高度。</param>
        private static (double Left, double Bottom, double Right, double Top) ToUnrotated(
            UglyToad.PdfPig.Core.PdfRectangle box, int degrees, double width, double height)
        {
            var minX = Math.Min(box.Left, box.Right);
            var maxX = Math.Max(box.Left, box.Right);
            var minY = Math.Min(box.Top, box.Bottom);
            var maxY = Math.Max(box.Top, box.Bottom);

            switch (((degrees % 360) + 360) % 360)
            {
                case 90:
                    // 顯示 (x', y') = (y, width - x)
                    return (width - maxY, minX, width - minY, maxX);
                case 180:
                    return (width - maxX, height - maxY, width - minX, height - minY);
                case 270:
                    // 顯示 (x', y') = (height - y, x)
                    return (minY, height - maxX, maxY, height - minX);
                default:
                    return (minX, minY, maxX, maxY);
            }
        }

        /// <summary>依頁面的 /Rotate（順時針角度）轉正。</summary>
        private static void Rotate(Bitmap bitmap, int degrees)
        {
            switch (((degrees % 360) + 360) % 360)
            {
                case 90:
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                case 180:
                    bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case 270:
                    bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
            }
        }

        /// <summary>解碼影像；不支援的格式回傳 null。</summary>
        private static Bitmap Decode(IPdfImage image)
        {
            var filters = Filters(image);

            if (filters.LastOrDefault() == "DCTDecode")
            {
                var jpeg = image.RawBytes.ToArray();
                if (filters.Count == 2 && filters[0] == "FlateDecode")
                {
                    jpeg = Inflate(jpeg);
                }
                else if (filters.Count != 1)
                {
                    return null;
                }

                using (var stream = new MemoryStream(jpeg))
                using (var decoded = Image.FromStream(stream))
                {
                    return new Bitmap(decoded);
                }
            }

            if (image.BitsPerComponent == 1 && image.TryGetBytesAsMemory(out var samples))
            {
                return FromBilevel(samples.ToArray(), image.WidthInSamples, image.HeightInSamples, IsDecodeInverted(image));
            }

            if (image.TryGetPng(out var png))
            {
                using (var stream = new MemoryStream(png))
                using (var decoded = Image.FromStream(stream))
                {
                    return new Bitmap(decoded);
                }
            }

            return null;
        }

        /// <summary>
        /// 黑白影像（遮罩）：取樣值 0 為黑色（遮罩的預設 Decode [0 1] 即「0 的位置塗上填色」），其餘透明。
        /// </summary>
        private static Bitmap FromBilevel(byte[] samples, int width, int height, bool inverted)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                var stride = (width + 7) / 8;
                var row = new int[width];
                const int black = unchecked((int)0xFF000000);

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var index = y * stride + x / 8;
                        var bit = index < samples.Length && (samples[index] & (0x80 >> (x % 8))) != 0;
                        row[x] = bit == inverted ? black : 0;
                    }

                    Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, width);
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        /// <summary>Decode 陣列為 [1 0] 時取樣值相反。</summary>
        private static bool IsDecodeInverted(IPdfImage image)
        {
            return image.Decode != null && image.Decode.Count >= 2 && image.Decode[0] > image.Decode[1];
        }

        private static List<string> Filters(IPdfImage image)
        {
            if (!image.ImageDictionary.TryGet(NameToken.Filter, out var token))
            {
                return new List<string>();
            }

            switch (token)
            {
                case NameToken name:
                    return new List<string> { name.Data };
                case ArrayToken array:
                    return array.Data.OfType<NameToken>().Select(n => n.Data).ToList();
                default:
                    return new List<string>();
            }
        }

        /// <summary>解開 zlib 格式（略過 2 位元組標頭）的 FlateDecode 資料。</summary>
        private static byte[] Inflate(byte[] data)
        {
            using (var input = new MemoryStream(data, 2, data.Length - 2))
            using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
