using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 依垂直位置將片段分群成行。
    /// </summary>
    internal static class LineBuilder
    {
        /// <summary>垂直重疊達較矮者高度的此比例，即視為同一行。</summary>
        private const double SameLineOverlapRatio = 0.5;

        public static IReadOnlyList<TextLine> Build(IEnumerable<TextFragment> fragments)
        {
            var groups = new List<List<TextFragment>>();
            var groupBounds = new List<Box>();

            foreach (var fragment in fragments.OrderBy(f => f.Bounds.Top).ThenBy(f => f.Bounds.Left))
            {
                var index = FindLine(groupBounds, fragment.Bounds);
                if (index < 0)
                {
                    groups.Add(new List<TextFragment> { fragment });
                    groupBounds.Add(fragment.Bounds);
                }
                else
                {
                    groups[index].Add(fragment);
                    groupBounds[index] = groupBounds[index].Union(fragment.Bounds);
                }
            }

            return groups
                .Select(group => new TextLine(group.OrderBy(f => f.Bounds.Left).ToList()))
                .OrderBy(line => line.Bounds.Top)
                .ThenBy(line => line.Bounds.Left)
                .ToList();
        }

        /// <summary>找出重疊程度最高且達門檻的行；找不到回傳 -1。</summary>
        private static int FindLine(IReadOnlyList<Box> lines, Box box)
        {
            var bestIndex = -1;
            var bestRatio = SameLineOverlapRatio;

            for (var i = 0; i < lines.Count; i++)
            {
                var minHeight = System.Math.Min(lines[i].Height, box.Height);
                if (minHeight <= 0)
                {
                    continue;
                }

                var ratio = lines[i].VerticalOverlap(box) / minHeight;
                if (ratio >= bestRatio)
                {
                    bestRatio = ratio;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
