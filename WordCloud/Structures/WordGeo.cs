using System;
using System.Windows;
using System.Windows.Media;

namespace WordCloud.Structures
{
    class WordGeo
    {
        private Rect _bounds;
        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly WordCloudEntry _wordEntry;
        private readonly TranslateTransform _translateTransform = new TranslateTransform();
        private readonly Geometry _geo;

        public WordGeo(Geometry geo, WordCloudEntry wordEntry)
        {
            _geo = geo;
            _bounds = geo.Bounds;
            _wordEntry = wordEntry;

            _geo.Transform = _transformGroup;

            Center = new Point(_bounds.Width / 2, _bounds.Height / 2);
            var rotateTransform = new RotateTransform(_wordEntry.Angle, Center.X, Center.Y);
            _transformGroup.Children.Add(rotateTransform);
            _bounds = rotateTransform.TransformBounds(_bounds);

            _transformGroup.Children.Add(new TranslateTransform(-_bounds.X, -_bounds.Y));

            _bounds.X = 0;
            _bounds.Y = 0;
            IntWidth = (int) Math.Ceiling(_bounds.Width);
            IntHeight = (int) Math.Ceiling(_bounds.Height);
            _transformGroup.Children.Add(_translateTransform);
        }

        public Geometry Geo
        {
            get
            {
                _translateTransform.X = _bounds.X;
                _translateTransform.Y = _bounds.Y;
                return _geo;
            }
        }

        public double X
        {
            get => _bounds.X;
            set => _bounds.X = value;
        }

        public double Y
        {
            get => _bounds.Y;
            set => _bounds.Y = value;
        }

        public double Bottom => _bounds.Y + Height;
        public double Right => _bounds.X + Width;

        public double Width => _bounds.Width;
        public double Height => _bounds.Height;

        public Point Center { get; }

        public int IntWidth { get; }
        public int IntHeight { get; }

        public int IntX => (int) Math.Floor(_bounds.X);
        public int IntY => (int) Math.Floor(_bounds.Y);

        public int IntBottom => IntY + IntHeight;
        public int IntRight => IntX + IntWidth;

        public GeometryDrawing GetDrawing()
        {
            var geoDrawing = new GeometryDrawing
            {
                Geometry = _geo,
                Brush = _wordEntry.Color,
            };

            _translateTransform.X = _bounds.X;
            _translateTransform.Y = _bounds.Y;

            if (!geoDrawing.IsFrozen) geoDrawing.Freeze();
            return geoDrawing;
        }

        public override string ToString()
        {
            return _wordEntry.Word;
        }
    }
}