using System.Collections.Generic;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;

namespace HRtoVRChat.Utils
{
    public class RichTextModel
    {
        private readonly List<(int Index, int Length, HighlightingColor Color)> _highlightings = new();

        public void ApplyHighlighting(int index, int length, HighlightingColor color)
        {
            _highlightings.Add((index, length, color));
        }

        public IEnumerable<(int Index, int Length, HighlightingColor Color)> GetHighlightings(int offset, int length)
        {
            foreach (var h in _highlightings)
            {
                if (h.Index + h.Length > offset && h.Index < offset + length)
                {
                    yield return h;
                }
            }
        }
    }

    public class RichTextColorizer : DocumentColorizingTransformer
    {
        private readonly RichTextModel _model;

        public RichTextColorizer(RichTextModel model)
        {
            _model = model;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            foreach (var h in _model.GetHighlightings(line.Offset, line.Length))
            {
                var start = h.Index;
                var end = h.Index + h.Length;

                if (start < line.Offset) start = line.Offset;
                if (end > line.EndOffset) end = line.EndOffset;

                if (start < end)
                {
                    ChangeLinePart(start, end, element =>
                    {
                        if (h.Color.Foreground != null)
                        {
                            element.TextRunProperties.SetForegroundBrush(h.Color.Foreground.GetBrush(null));
                        }
                    });
                }
            }
        }
    }
}
