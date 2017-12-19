using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WordCloudTest.BoundingBox;
using WordCloudTest.QuadTree;
using WordCloudTest.WordCloud;
using WordCloudTest.WordClouds;

namespace WordCloudTest.Views
{
    /// <summary>
    /// Interaction logic for WordCloud2.xaml
    /// </summary>
    public partial class WordCloud2
    {
        private const int MaxWords = 10;
        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        private int _currentWord;
        private DrawingGroup _wordDrawingGroup;

        private CenterPoints _currentCenterPoint = CenterPoints.M;
        private double _fontMultiplier;
        private int _minWordWeight;
        private readonly DrawingGroup _mainDrawingGroup;
        private GeometryDrawing _previousCollidedWord;

        private RectQuadTree<GeometryDrawing> _recQuadTree;

        //     private readonly Dictionary<GeometryDrawing, HierarchicalBoundingBox<GeometryDrawing>> _wordChops = new Dictionary<GeometryDrawing, HierarchicalBoundingBox<GeometryDrawing>>();
        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Interpris;

        public WordCloud2()
        {
            InitializeComponent();

            var di = new DrawingImage();

            _mainDrawingGroup = new DrawingGroup();
            di.Drawing = _mainDrawingGroup;

            BaseImage.Source = di;
        }

        private void Setup()
        {
            _minWordWeight = _words.Min(e => e.wordWeight);
            _fontMultiplier = GetFontMultiplier();
            _recQuadTree = new RectQuadTree<GeometryDrawing>(new Rect(0, 0, Width, Height));

            var bgDrawingGroup = new DrawingGroup();
            _wordDrawingGroup = new DrawingGroup();

            using (var context = bgDrawingGroup.Open())
            {
                context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Width, Height));
            }

            bgDrawingGroup.Freeze();

            _mainDrawingGroup.Children.Add(bgDrawingGroup);
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);
        }

        public void AddWord()
        {
            var word = _words[_currentWord];
            var geo = CreateWordGeometry(word);

            if (geo != null)
            {
                _wordDrawingGroup.Children.Add(geo);
            }

            _currentWord++;
        }

        public void AddWords()
        {
            foreach (var word in _words)
            {
                var geo = CreateWordGeometry(word);

                if (geo != null)
                {
                    _wordDrawingGroup.Children.Add(geo);
                }

                _currentWord++;
            }

            //           BaseImage.Stretch = Stretch.UniformToFill;
            //       _mainDrawingGroup.Children.RemoveAt(0);
            _currentWord = 0;
        }

        public void DoStuff(WordCloudData wordCloudData)
        {
            if (_currentWord == _words.Count)
            {
                PopulateWordList(wordCloudData);
            }

            if (_mainDrawingGroup.Children.Count == 0)
            {
                Setup();
            }

            AddWord();
        }

        public void RestartCloud(WordCloudData wordCloudData)
        {
            var s = new Stopwatch();
            s.Start();
            PopulateWordList(wordCloudData);
            Setup();
            AddWords();
            s.Stop();
            Debug.WriteLine(s.ElapsedMilliseconds);
        }

        private Point CalculateNextStartingPoint(double adjustment = 0.0)
        {
            var center = new Point();
            if (adjustment > WordCloudConstants.MaxSprialLength)
            {
                adjustment = 2.0;
                center = GetCenterPoint();
            }

            var multi = adjustment / WordCloudConstants.DoublePi * WordCloudConstants.SpiralRadius;
            var angle = adjustment % WordCloudConstants.DoublePi;
            var spiralPoint = new Point(Width / 2 + multi * Math.Sin(angle), Height / 2 + multi * Math.Cos(angle));
            //return new Point(Width / 2 + multi * Math.Sin(angle), Height / 2 + multi * Math.Cos(angle));
            return new Point(spiralPoint.X + center.X, spiralPoint.Y + center.Y);
        }

        private GeometryDrawing CreateWordGeometry(WordCloudEntry word)
        {
            var text = new FormattedText(word.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Typeface,
                GetFontSize(word.wordWeight),
                new SolidColorBrush(word.Color),
                DpiScale.PixelsPerDip);

            var textGeometry = text.BuildGeometry(new Point(Width / 2 - text.Width / 2, Height / 2 - text.Height / 2));

            var transformGroup = new TransformGroup();
            var rotateTransform = new RotateTransform(word.Angle, Width / 2 - text.Width / 2, Width / 2 - text.Height / 2);
            var translateTransform = new TranslateTransform(0.0, 0.0);
            transformGroup.Children.Add(translateTransform);
            transformGroup.Children.Add(rotateTransform);
            textGeometry.Transform = transformGroup;

            var geo = new GeometryDrawing
            {
                Geometry = textGeometry,
                Brush = new SolidColorBrush(word.Color),
            };

            var bounds = geo.Bounds;
            //      var boundingBox = ChopWord(textGeometry);

            var halfGeoWidth = bounds.Width / 2;
            var halfGeoHeight = bounds.Height / 2;

            var initialPoint = bounds;

            var adjustment = 0.0;

            var collide = new Stopwatch();
            collide.Start();
            _wordGeoLookup.Add(geo, word);
            while (PerformCollisionTests(geo, bounds, ref adjustment, translateTransform))
            {
                var nextPosition = CalculateNextStartingPoint(adjustment);

                bounds.X = nextPosition.X - halfGeoWidth;
                bounds.Y = nextPosition.Y - halfGeoHeight;

                translateTransform.X = bounds.X - initialPoint.X;
                translateTransform.Y = bounds.Y - initialPoint.Y;
            }


            collide.Stop();
            _previousCollidedWord = null;
            geo.Freeze();
            bounds = geo.Bounds;
            //     boundingBox.GlobalLocation = bounds.Location;
            //   boundingBox.Freeze();
            _recQuadTree.Insert(geo, bounds);

            return geo;
        }

        private readonly Dictionary<GeometryDrawing, WordCloudEntry> _wordGeoLookup = new Dictionary<GeometryDrawing, WordCloudEntry>();
        private Dictionary<GeometryDrawing, RenderTargetBitmap> _bitmapCache = new Dictionary<GeometryDrawing, RenderTargetBitmap>();

/*
        private HierarchicalBoundingBox<GeometryDrawing> ChopWord(GeometryDrawing geo)
        {
            var bb = new HierarchicalBoundingBox<GeometryDrawing>(geo);
            _wordChops.Add(geo, bb);
            return bb;
        }
*/

        private bool PerformCollisionTests(GeometryDrawing newGeometry, Rect bounds, ref double adjustment, Transform t)
        {
            if (_previousCollidedWord != null && adjustment > 0 && !Equals(newGeometry, _previousCollidedWord))
            {
                if (DoGeometricDrawingsCollide(newGeometry, _previousCollidedWord, bounds, ref adjustment, t)) return true;
            }
         //   var found = _recQuadTree.QueryLocation(bounds).Where(x => x != _previousCollidedWord).ToList();
            var found = _wordGeoLookup.Keys.Where(x => !Equals(x, newGeometry)).ToList();
            Debug.WriteLine("Placing: " + _wordGeoLookup[newGeometry].Word);
            Debug.WriteLine("Found: " + string.Join(" ", found.Select((k, v) => _wordGeoLookup[k].Word)));
            foreach (var existingGeometry in  found)
            {
                Debug.WriteLine("Avoid: " + _wordGeoLookup[existingGeometry].Word);

                if (DoGeometricDrawingsCollide(newGeometry, existingGeometry, bounds, ref adjustment, t))
                {
                    Debug.WriteLine("Hit: " + _wordGeoLookup[existingGeometry].Word);

                    return true;
                }
            }
            return false;
        }

        private bool DoGeometricDrawingsCollide(GeometryDrawing newGeo, GeometryDrawing existingGeo, Rect bounds, ref double adjustment, Transform t)
        {
            if (Equals(newGeo, existingGeo)) return true;
            if (bounds.X < 0 || bounds.Y < 0 || bounds.Right > Width || bounds.Top > Height)
            {
                _previousCollidedWord = null;
                adjustment = 1.0;
                return true;
            }
            //   _wordChops[newGeo].GlobalLocation = bounds.Location;
            // var bbTest = newGeo.FillContainsWithDetail(existingGeo) != IntersectionDetail.Empty;
            //      var bbTest = _wordChops[existingGeo].DoBoxesCollide(_wordChops[newGeo]);

            var pixels2 = GetPixels(existingGeo, newGeo.Geometry.Bounds, true);
            var pixels = GetPixels(newGeo, newGeo.Geometry.Bounds);
            Debug.WriteLine(pixels.Max());
            Debug.WriteLine(pixels2.Max());
            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] > 0 && pixels2[i] > 0)
                {
                    adjustment += Math.Min(bounds.Width, bounds.Height) * .5;
                    _previousCollidedWord = existingGeo;
                    Console.WriteLine(i);
                    return true;
                }
            }
            return false;
        }

        private int[] GetPixels(GeometryDrawing existingGeo, Rect r, bool useCache = false)
        {
            
            var r2 = new Int32Rect((int) r.Width, (int) r.Height, (int) r.X, (int) r.Y);

            var stride = ((int)Width * PixelFormats.Pbgra32.BitsPerPixel + 7) / 8;
            var pixels = new int[(int)Height * stride];

            if (useCache && _bitmapCache.TryGetValue(existingGeo, out var bm2))
            {
                bm2.CopyPixels(Int32Rect.Empty, pixels, stride, 0);

                return pixels;
            }


            var bm = new RenderTargetBitmap((int) Width, (int) Height, DpiScale.PixelsPerInchX, DpiScale.PixelsPerInchY, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(existingGeo);
            }
            //dv.Transform = t;
            bm.Render(dv);
      //      BaseImage.Source = bm;
            
            BaseImage.C
            bm.CopyPixels(Int32Rect.Empty, pixels, stride, 0);
            if (useCache) _bitmapCache[existingGeo] = bm;

            return pixels;
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
            Point point;
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

            if (_currentCenterPoint == CenterPoints.B)
            {
                _currentCenterPoint = CenterPoints.M;
            }
            else
            {
                _currentCenterPoint = _currentCenterPoint + 1;
            }

            return point;
        }

        private double GetFontMultiplier()
        {
            var maxWordWeight = Math.Max(_words.Max(e => e.wordWeight), WordCloudConstants.MinimumLargestValue);
            var wordWeightRange = Math.Max(0.00001, maxWordWeight - _minWordWeight);
            var areaPerLetter = GetAverageLetterPixelWidth() / wordWeightRange;

            var targetWidth = (Width + Height) / WordCloudConstants.TargetWidthFactor * WordCloudConstants.LargestSizeWidthProportion;
            var largestWord = _words.OrderByDescending(e => (e.wordWeight - _minWordWeight) * e.Word.Length).First();

            // Use minimum word length of MINIMUM_LARGEST_WORD_LENGTH to avoid over scaling
            var largestWordLength = Math.Max(largestWord.Word.Length, WordCloudConstants.MinimumLargestWordLength);

            var maxWordSize = 100 / ((largestWord.wordWeight - _minWordWeight) * largestWordLength * areaPerLetter / targetWidth);

            // Reduce the maximum word size for random theme to avoid placement/collision issues due to high angle values
            if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
            {
                maxWordSize *= 1;
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
                CurrentTheme.Typeface,
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