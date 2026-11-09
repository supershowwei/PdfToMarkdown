using System.Collections.Generic;
using System.Linq;
using PdfToMarkdown.Layout;
using UglyToad.PdfPig.Content;

namespace PdfToMarkdown.Pdf
{
    /// <summary>
    /// 將 PdfPig 的頁面轉為 <see cref="PageContent"/>。
    /// </summary>
    internal static class PdfPageReader
    {
        /// <summary>字元框在基線以上的比例（約為一般字型的 ascent）。</summary>
        private const double AscentRatio = 0.8;

        /// <summary>字元框在基線以下的比例（約為一般字型的 descent）。</summary>
        private const double DescentRatio = 0.2;

        /// <summary>格線的最大粗細（點）。</summary>
        private const double MaxRulingThickness = 2.0;

        /// <summary>格線的最小長度（點），排除點狀圖形。</summary>
        private const double MinRulingLength = 3.0;

        public static PageContent Read(Page page)
        {
            // 空白字元本身不輸出，但記錄在下一個字元上，作為補空白的依據
            var fragments = new List<TextFragment>();
            var spaceBefore = false;
            foreach (var letter in page.Letters)
            {
                if (string.IsNullOrWhiteSpace(letter.Value))
                {
                    spaceBefore = true;
                    continue;
                }

                fragments.Add(ToFragment(letter, page.Height, spaceBefore));
                spaceBefore = false;
            }

            var isScanned = fragments.Count == 0 && page.GetImages().Any();

            return new PageContent(page.Number, page.Width, page.Height, fragments, isScanned)
            {
                Rulings = ReadRulings(page),
            };
        }

        /// <summary>
        /// 讀取格線：填滿或描邊、且寬或高小於門檻的細長矩形（表格框線、會計格式的底線）。
        /// </summary>
        private static IReadOnlyList<Box> ReadRulings(Page page)
        {
            var rulings = new List<Box>();

            foreach (var path in page.Paths)
            {
                if (path.IsClipping || !(path.IsFilled || path.IsStroked))
                {
                    continue;
                }

                foreach (var subpath in path)
                {
                    var rectangle = subpath.GetBoundingRectangle();
                    if (!rectangle.HasValue)
                    {
                        continue;
                    }

                    var r = rectangle.Value;
                    var isHorizontal = r.Height <= MaxRulingThickness && r.Width >= MinRulingLength;
                    var isVertical = r.Width <= MaxRulingThickness && r.Height >= MinRulingLength;
                    if (isHorizontal || isVertical)
                    {
                        rulings.Add(new Box(r.Left, page.Height - r.Top, r.Right, page.Height - r.Bottom));
                    }
                }
            }

            return rulings;
        }

        /// <summary>
        /// 以基線與字級推算字元框，而不用實際字形外框：
        /// 同一行的字元因此有一致的高度，標點符號也不會因外框過小而被分到別行。
        /// </summary>
        private static TextFragment ToFragment(Letter letter, double pageHeight, bool spaceBefore)
        {
            var size = letter.PointSize > 0 ? letter.PointSize : letter.BoundingBox.Height;
            var baseline = pageHeight - letter.StartBaseLine.Y;

            var bounds = new Box(
                letter.StartBaseLine.X,
                baseline - size * AscentRatio,
                letter.EndBaseLine.X,
                baseline + size * DescentRatio);

            return new TextFragment(letter.Value, bounds, size, spaceBefore);
        }
    }
}
