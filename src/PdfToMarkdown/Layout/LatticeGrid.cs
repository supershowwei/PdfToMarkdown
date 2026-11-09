using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 有完整格線（垂直線與水平線）的表格範圍。
    /// </summary>
    internal sealed class LatticeGrid
    {
        /// <summary>格線連通判斷的容許誤差（點）：線段接縫約 0.5 點。</summary>
        private const double ConnectTolerance = 2.0;

        /// <summary>座標相差在此範圍內視為同一條線（點）。</summary>
        private const double ClusterTolerance = 2.0;

        /// <summary>至少要有這麼多條不同位置的垂直線，才是格線表格（會計格式只有水平底線）。</summary>
        private const int MinVerticalLines = 3;

        /// <summary>至少要有這麼多條不同位置的水平線。</summary>
        private const int MinHorizontalLines = 2;

        private LatticeGrid(Box bounds, IReadOnlyList<Box> verticals, IReadOnlyList<double> columnLines, IReadOnlyList<double> rowLines)
        {
            Bounds = bounds;
            Verticals = verticals;
            ColumnLines = columnLines;
            RowLines = rowLines;
        }

        public Box Bounds { get; }

        /// <summary>所有垂直線段。</summary>
        public IReadOnlyList<Box> Verticals { get; }

        /// <summary>垂直線的 X 位置（由小到大，已合併相近者）。</summary>
        public IReadOnlyList<double> ColumnLines { get; }

        /// <summary>水平線的 Y 位置（由小到大，已合併相近者）。</summary>
        public IReadOnlyList<double> RowLines { get; }

        public bool Contains(TextLine line)
        {
            var x = (line.Bounds.Left + line.Bounds.Right) / 2;
            var y = line.Bounds.CenterY;
            return x >= Bounds.Left && x <= Bounds.Right && y >= Bounds.Top && y <= Bounds.Bottom;
        }

        /// <summary>穿過指定高度的垂直線 X 位置。</summary>
        public IReadOnlyList<double> ColumnLinesAt(double y)
        {
            return Cluster(Verticals
                .Where(v => v.Top - ConnectTolerance <= y && v.Bottom + ConnectTolerance >= y)
                .Select(v => (v.Left + v.Right) / 2));
        }

        /// <summary>
        /// 找出頁面上的格線表格：把互相連接的格線分群，有足夠垂直線與水平線的群組即為格線表格。
        /// </summary>
        public static IReadOnlyList<LatticeGrid> Find(IReadOnlyList<Box> rulings)
        {
            var grids = new List<LatticeGrid>();

            foreach (var group in ConnectedGroups(rulings))
            {
                var verticals = group.Where(r => r.Height > r.Width).ToList();
                var horizontals = group.Where(r => r.Width >= r.Height).ToList();

                var columnLines = Cluster(verticals.Select(v => (v.Left + v.Right) / 2));
                var rowLines = Cluster(horizontals.Select(h => h.CenterY));
                if (columnLines.Count < MinVerticalLines || rowLines.Count < MinHorizontalLines)
                {
                    continue;
                }

                var bounds = group.Skip(1).Aggregate(group[0], (box, r) => box.Union(r));
                grids.Add(new LatticeGrid(bounds, verticals, columnLines, rowLines));
            }

            return grids;
        }

        /// <summary>以 union-find 將互相接觸的格線分群。</summary>
        private static List<List<Box>> ConnectedGroups(IReadOnlyList<Box> rulings)
        {
            var parent = Enumerable.Range(0, rulings.Count).ToArray();

            int Root(int i)
            {
                while (parent[i] != i)
                {
                    parent[i] = parent[parent[i]];
                    i = parent[i];
                }

                return i;
            }

            for (var i = 0; i < rulings.Count; i++)
            {
                for (var j = i + 1; j < rulings.Count; j++)
                {
                    if (Touches(rulings[i], rulings[j]))
                    {
                        parent[Root(i)] = Root(j);
                    }
                }
            }

            return Enumerable.Range(0, rulings.Count)
                .GroupBy(Root)
                .Select(group => group.Select(i => rulings[i]).ToList())
                .ToList();
        }

        private static bool Touches(Box a, Box b)
        {
            return a.Left - ConnectTolerance <= b.Right && b.Left - ConnectTolerance <= a.Right
                && a.Top - ConnectTolerance <= b.Bottom && b.Top - ConnectTolerance <= a.Bottom;
        }

        private static List<double> Cluster(IEnumerable<double> values)
        {
            var result = new List<double>();
            foreach (var value in values.OrderBy(v => v))
            {
                if (result.Count == 0 || value - result[result.Count - 1] > ClusterTolerance)
                {
                    result.Add(value);
                }
            }

            return result;
        }
    }
}
