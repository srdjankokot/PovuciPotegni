using UnityEngine;
using UnityEngine.UIElements;

namespace PovuciPotegni.Game
{
    /// <summary>A stroked line icon on a 24 x 24 grid, with the shapes from the screen design.</summary>
    [UxmlElement]
    public partial class IconView : VisualElement
    {
        private const float GridSize = 24f;

        private string _icon = "";
        private Color _stroke = new Color32(0x12, 0x13, 0x1C, 0xFF);
        private float _width = 2f;

        [UxmlAttribute]
        public string Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public Color Stroke
        {
            get => _stroke;
            set
            {
                _stroke = value;
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute("line-width")]
        public float LineWidth
        {
            get => _width;
            set
            {
                _width = value;
                MarkDirtyRepaint();
            }
        }

        public IconView()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            float scale = Mathf.Min(contentRect.width, contentRect.height) / GridSize;
            if (scale <= 0)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.strokeColor = _stroke;
            painter.lineWidth = _width * scale;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            void Line(params float[] points)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(points[0], points[1]) * scale);
                for (int i = 2; i < points.Length; i += 2)
                {
                    painter.LineTo(new Vector2(points[i], points[i + 1]) * scale);
                }

                painter.Stroke();
            }

            void Circle(float x, float y, float radius)
            {
                painter.BeginPath();
                painter.Arc(new Vector2(x, y) * scale, radius * scale, 0, 360);
                painter.Stroke();
            }

            switch (_icon)
            {
                case "arrow":
                    Line(5, 12, 19, 12);
                    Line(13, 6, 19, 12, 13, 18);
                    break;
                case "back":
                    Line(19, 12, 5, 12);
                    Line(11, 6, 5, 12, 11, 18);
                    break;
                case "close":
                    Line(6, 6, 18, 18);
                    Line(18, 6, 6, 18);
                    break;
                case "sliders":
                    Line(4, 7, 14, 7);
                    Line(18, 7, 20, 7);
                    Line(4, 17, 6, 17);
                    Line(10, 17, 20, 17);
                    Circle(16, 7, 2.2f);
                    Circle(8, 17, 2.2f);
                    break;
                case "chart":
                    Line(4, 20, 4, 10);
                    Line(10, 20, 10, 4);
                    Line(16, 20, 16, 13);
                    Line(22, 20, 2, 20);
                    break;
                case "lock":
                    Line(5, 11, 19, 11, 19, 21, 5, 21, 5, 11);
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(8, 11) * scale);
                    painter.LineTo(new Vector2(8, 8) * scale);
                    painter.Arc(new Vector2(12, 8) * scale, 4 * scale, 180, 360);
                    painter.LineTo(new Vector2(16, 11) * scale);
                    painter.Stroke();
                    break;
            }
        }
    }
}
