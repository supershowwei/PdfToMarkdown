using System.Collections.Generic;
using System.Linq;

namespace PdfToMarkdown.Layout
{
    /// <summary>
    /// 視覺上的一行文字，片段已依 X 座標由左至右排序。
    /// </summary>
    internal sealed class TextLine
    {
        public TextLine(IReadOnlyList<TextFragment> fragments)
        {
            Fragments = fragments;
            Bounds = fragments.Skip(1).Aggregate(fragments[0].Bounds, (box, f) => box.Union(f.Bounds));
            Text = TextJoiner.Join(fragments);
        }

        public IReadOnlyList<TextFragment> Fragments { get; }

        public Box Bounds { get; }

        public string Text { get; }

        public override string ToString()
        {
            return Text;
        }
    }
}
