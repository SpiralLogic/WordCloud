using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WordCloud.Structures
{
    internal class WordDrawing
    {
        private Rect _bounds;
        private double _scale = 1;

        private readonly TransformGroup _transformGroup = new TransformGroup();
        private readonly TranslateTransform _translateTransform = new TranslateTransform();
        private readonly TransformGroup _scaleTransformGroup = new TransformGroup();
        private readonly WordCloudEntry _wordEntry;
        private readonly Geometry _geo;

        public WordDrawing(WordCloudEntry wordEntry, WordCloudTheme theme, DpiScale scale)
        {
            var text = new FormattedText(wordEntry.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                theme.Typeface,
                100,
                wordEntry.Brush,
                scale.PixelsPerDip);

            var textGeometry = text.BuildGeometry(new Point(0, 0));
            _geo = textGeometry;
            _wordEntry = wordEntry;
            _bounds = textGeometry.Bounds;
            _geo.Transform = _transformGroup;

            var rotateTransform = new RotateTransform(_wordEntry.Angle, _bounds.Width / 2, _bounds.Height / 2);

            _transformGroup.Children.Add(rotateTransform);
            _bounds = rotateTransform.TransformBounds(_bounds);

            _transformGroup.Children.Add(_scaleTransformGroup);

            _initialPlacementTransform = new TranslateTransform(-_bounds.X, -_bounds.Y);


            _transformGroup.Children.Add(_initialPlacementTransform);

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

        public int Weight => _wordEntry.Weight;

        public double Scale
        {
            get => _scale;
            set
            {
                var scaleTransform = new ScaleTransform(value, value);
                _scaleTransformGroup.Children.Add(scaleTransform);


                _bounds = _initialPlacementTransform.Inverse.TransformBounds(_bounds);
                _bounds = scaleTransform.TransformBounds(_bounds);

                //scaleTransform.CenterX = _bounds.Width / 2;
                //scaleTransform.CenterY = _bounds.Height/ 2;


                IntWidth = (int) Math.Ceiling(_bounds.Width);
                IntHeight = (int) Math.Ceiling(_bounds.Height);

                _initialPlacementTransform.X = -_bounds.X;
                _initialPlacementTransform.Y = -_bounds.Y;

                _bounds.X = 0;
                _bounds.Y = 0;
            }
        }

        public Point Center { get; }

        public int IntWidth;
        public int IntHeight;
        private TranslateTransform _initialPlacementTransform;

        public int IntX => (int) Math.Ceiling(_bounds.X);
        public int IntY => (int) Math.Ceiling(_bounds.Y);

        public int IntBottom => IntY + IntHeight;
        public int IntRight => IntX + IntWidth;

        public GeometryDrawing GetDrawing()
        {
            var geoDrawing = new GeometryDrawing
            {
                Geometry = _geo,
                Brush = _wordEntry.Brush,
            };

            _translateTransform.X = _bounds.X;
            _translateTransform.Y = _bounds.Y;

            if (!geoDrawing.IsFrozen) geoDrawing.Freeze();
      //      _bounds = geoDrawing.Bounds;
            return geoDrawing;
        }

        public override string ToString()
        {
            return _wordEntry.Word;
        }

        public bool Contains(double x, double y)
        {
            if (x >= _bounds.X && x - _bounds.Width <= _bounds.X && y >= _bounds.Y)
                return y - _bounds.Height <= _bounds.Y;
            return false;
        }

        public Rect GetBounds()
        {
            return new Rect(X, Y, Width, Height);
        }
    }
}