using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WordCloudTest.WordCloud;
using WordCloudTest.WordClouds;

namespace WordCloudTest.Views
{
    /// <summary>
    /// Interaction logic for WordCloud2.xaml
    /// </summary>
    public partial class WordCloud2 : UserControl
    {
        private const int MaxWords = 30;
        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private Point _initScale;
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        private int _currentWord = 0;
        private Rect _geoSize = new Rect();
        private DrawingImage _di;
        private DrawingGroup _mainDrawingGroup;

        private double _spiralPosition = 0.0;
        private CenterPoints _currentCenterPoint = CenterPoints.M;
        private double _fontMultiplier;
        private int _minWordWeight;
        private DrawingGroup _debugDrawingGroup = new DrawingGroup();
        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Interpris;

        public WordCloud2()
        {
            InitializeComponent();

            _di = new DrawingImage();

            _mainDrawingGroup = new DrawingGroup();
            _di.Drawing = _mainDrawingGroup;

            BaseImage.Source = _di;
        }

        private void Setup()
        {
 
            _initScale = new Point(5, 5);

            _minWordWeight = _words.Min(e => e.wordWeight);
            _fontMultiplier = GetFontMultiplier();

            var dg = new DrawingGroup();
            using (var context = dg.Open())
            {
                context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Width, Height));
            }

            _mainDrawingGroup.Children.Add(dg);
            
            
        }

        public void AddWord()
        {
            var word = _words[_currentWord];

            var geo = CreateWordGeometry(word);

            if (geo != null)
            {
                var dg = new DrawingGroup();
                using (var context = dg.Open())
                {
                    context.DrawDrawing(geo);
                }
                _mainDrawingGroup.Children.Add(dg);
            }


            _currentWord++;
        }

        public void DoStuff()
        {
            if (_currentWord == _words.Count )
            {
                PopulateWordList(DataContext as WordCloudData);
                Setup();
            }
             AddWord();
        }

        private Point CalculateNextStartingPoint(double adjustment = 0.0)
        {
            _spiralPosition += adjustment;
            if (_spiralPosition > WordCloudConstants.MaxSprialLength)
            {
                _spiralPosition = 0.0;
            }

            var spiralPoint = GetSpiralPoint(_spiralPosition);
            
            
            //var center = GetCenterPoint();
            //return new Point(spiralPoint.X + center.X, spiralPoint.Y + center.Y);
            return new Point(spiralPoint.X + Width / 2, spiralPoint.Y + Height / 2);
        }

        private GeometryDrawing CreateWordGeometry(WordCloudEntry word)
        {
            var text = new FormattedText(word.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Font,
                GetFontSize(word.wordWeight),
                new SolidColorBrush(word.Color),
                DpiScale.PixelsPerDip);
            _spiralPosition = 0.0;

            var position = new Point(Width/2 - text.Width / 2,Height/2 - text.Height / 2);
            var geo = new GeometryDrawing();
            geo.Brush = new SolidColorBrush(word.Color);
            geo.Geometry = text.BuildGeometry(position);

            var translateTransform = new TranslateTransform(0.0, 0.0);
            var rotateTransform = new RotateTransform(word.Angle, position.X, position.Y);
            var transformGroup = new TransformGroup();

            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);

            geo.Geometry.Transform = transformGroup;

            bool collided;
            var adjustment = 0.0;
            do
            {
                var newPosition = CalculateNextStartingPoint(adjustment);
                translateTransform.X = Width / 2 - newPosition.X;
                translateTransform.Y = Height / 2 - newPosition.Y;
                collided = WordGeometryHasCollision(geo);
                adjustment = WordCloudConstants.DoublePi /30;
            } while (collided);


            geo.Freeze();

            return geo;
        }

        private bool WordGeometryHasCollision(GeometryDrawing geo)
        {
            foreach (var dr in _mainDrawingGroup.Children.OfType<DrawingGroup>().SelectMany(dg => dg.Children.OfType<GeometryDrawing>()).Where(gd => gd.Geometry is GeometryGroup))
            {
                var intersectionDetail = geo.Geometry.FillContainsWithDetail(dr.Geometry);
                if (intersectionDetail != IntersectionDetail.Empty && intersectionDetail != IntersectionDetail.NotCalculated) return true;
            }

            return false;
        }
        

        private void WordCloud2_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is WordCloudData wordCloudData)) return;
            PopulateWordList(wordCloudData);
            Setup();
            for (var i = 0; i < _words.Count; i++)
                AddWord();
        }

        private void PopulateWordList(WordCloudData wordCloudData)
        {
            var random = new Random();
            foreach (var row in wordCloudData.Words.Rows.Take(MaxWords))
            {
                var colorIndex = 0;
                var angle = WordCloudConstants.NoRotation;

                // Theme's define a color list, randomly assign one by index
                if (CurrentTheme.ColorList.Count > 1)
                {
                    colorIndex = random.Next(0, CurrentTheme.ColorList.Count);
                }

                if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Mixed)
                {
                    // First word always horizontal 70% Horizontal (default), 30% Vertical
                    if (_words.Any() && random.Next(0, 10) >= 7)
                    {
                        angle = WordCloudConstants.MixedRotationVertical;
                    }
                }
                else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
                {
                    // First word always horizontal
                    if (!_words.Any())
                    {
                        angle = -WordCloudConstants.RandomMaxRotationAbs +
                                random.Next(0, WordCloudConstants.RandomRange);
                    }
                }

                // At this stage, the word alpha value is set to be the same as the size value making the word color fade proportionally with word size
                _words.Add(new WordCloudEntry
                    {
                        Word = row.Item.Word,
                        wordWeight = row.Count * WordCloudConstants.WeightedFrequencyMultiplier,
                        Color = CurrentTheme.ColorList[colorIndex],
                        AlphaValue = row.Count,
                        Angle = angle
                    }
                );
            }
        }

        private Point GetCenterPoint()
        {
            var point = default(Point);
            switch (_currentCenterPoint)
            {
                case CenterPoints.M:
                    point = new Point((int) (Width / 2), (int) (Height / 2));
                    break;
                case CenterPoints.R:
                    point = new Point((int) (Width / 4), (int) (Height / 4));
                    break;
                case CenterPoints.T:
                    point = new Point((int) (Width / 4), (int) (3 * Height / 2));
                    break;
                case CenterPoints.L:
                    point = new Point((int) (3 * Width / 4), (int) (Height / 2));
                    break;
                case CenterPoints.B:
                    point = new Point((int) (3 * Width / 4), (int) (3 * Height / 4));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

/*
            if (_currentCenterPoint == CenterPoints.B)
            {
                _currentCenterPoint = CenterPoints.M;
            }
            else
            {
                _currentCenterPoint = _currentCenterPoint + 1;
            }
*/

            return point;
        }

        private Point GetSpiralPoint(double position)
        {
            var mult = position / WordCloudConstants.DoublePi * WordCloudConstants.SpiralRadius;
            var angle = position % WordCloudConstants.DoublePi;
            return new Point(mult * Math.Sin(angle), mult * Math.Cos(angle));
        }

        private double GetFontMultiplier()
        {
            var maxWordWeight = Math.Max(_words.Max(e => e.wordWeight), WordCloudConstants.MinimumLargestValue);
            var wordWeightRange = Math.Max(0.00001, maxWordWeight - _minWordWeight);
            var areaPerLetter = GetAverageLetterPixelWidth() / wordWeightRange;

            var targetWidth = (Width + Height) / WordCloudConstants.TargetWidthFactor * WordCloudConstants.LargestSizeWidthProportion;
            var largestWord = _words.OrderByDescending(e => (e.wordWeight - _minWordWeight) * e.Word.Length).First();

            // Use minimum word length of MINIMUM_LARGEST_WORD_LENGTH to avoid overscalling
            var largestWordLength = Math.Max(largestWord.Word.Length, WordCloudConstants.MinimumLargestWordLength);

            var maxWordSize = 100 / ((largestWord.wordWeight - _minWordWeight) * largestWordLength * areaPerLetter / targetWidth);

            // Reduce the maximum word size for random theme to avoid placement/collision issues due to high angle values
            if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
            {
                maxWordSize *= 0.8;
            }

            var maxFontSize = Math.Max(WordCloudConstants.MinFontSize * 2.7, maxWordSize);

            return Math.Min((maxFontSize - WordCloudConstants.MinFontSize) / wordWeightRange, 200);
        }

        private int GetFontSize(int size)
        {
            return (int) ((size - _minWordWeight) * _fontMultiplier + WordCloudConstants.MinFontSize);
        }

        private double GetAverageLetterPixelWidth()
        {
            var txt = new FormattedText("X",
                Thread.CurrentThread.CurrentCulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Font,
                WordCloudConstants.WeightedFrequencyMultiplier,
                Brushes.Black,
                DpiScale.PixelsPerDip);

            return txt.Width;
        }

        private enum CenterPoints
        {
            M,
            R,
            T,
            L,
            B
        }
    }
}