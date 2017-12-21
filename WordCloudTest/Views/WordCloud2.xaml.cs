using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WordCloudTest.WordCloud;
using WordCloudTest.WordClouds;

namespace WordCloudTest.Views
{
    /// <summary>
    /// Interaction logic for WordCloud2.xaml
    /// </summary>
    public partial class WordCloud2
    {
        private const int MaxWords = 100;
        private readonly PixelFormat _pixelFormat = PixelFormats.Pbgra32;
        private const int Pbgra32Bytes = 4;
        private const int Pbgra32Alpha = 0;
        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        private int _currentWord;
        private DrawingGroup _wordDrawingGroup;

        private CenterPoints _currentCenterPoint = CenterPoints.M;
        private double _fontMultiplier;
        private int _minWordWeight;
        private readonly DrawingGroup _mainDrawingGroup;

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

                Debug.WriteLine(_currentWord + " " + word.Word);
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

        private bool _spiralMode = true;

        private bool CalculateNextStartingPoint(ref Rect bounds, double adjustment = 0.0)
        {
            if (adjustment < 0.0)
            {
                return false;
            }

            var center = new Point();
            if (adjustment > WordCloudConstants.MaxSprialLength || Math.Abs(adjustment) < 0.001)
            {
                if (_lastplacedbounds != Rect.Empty)
                {
                    center.X = _lastplacedbounds.X;
                    center.X = _lastplacedbounds.Y;
                    _lastplacedbounds = Rect.Empty;
                    adjustment = 1.0;
                }
                else
                {
                    _spiralMode = false;
                    bounds.X = Width / 2;
                    bounds.Y = Height / 4;
                    return false;
                }
            }

            var multi = adjustment / WordCloudConstants.DoublePi * WordCloudConstants.SpiralRadius;
            var angle = adjustment % WordCloudConstants.DoublePi;

            bounds.X = Width / 2 + multi * Math.Sin(angle) + center.X;
            bounds.Y = Height / 2 + multi * Math.Cos(angle) + center.Y;
            return true;
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
            var textGeometry = text.BuildGeometry(new Point(0, 0));
            var bounds = textGeometry.Bounds;

            var transformGroup = new TransformGroup();
            var rotateTransform = new RotateTransform(word.Angle, text.Width / 2, text.Height / 2);
            transformGroup.Children.Add(rotateTransform);

            textGeometry.Transform = transformGroup;

            bounds = rotateTransform.TransformBounds(bounds);
            var translateTransform = new TranslateTransform(-bounds.X, -bounds.Y);
            transformGroup.Children.Add(translateTransform);
            bounds = translateTransform.TransformBounds(bounds);
            var geo = new GeometryDrawing
            {
                Geometry = textGeometry,
                Brush = new SolidColorBrush(word.Color),Pen = new Pen(Brushes.Transparent,10)
            };

            var adjustment = 0.0;

            var collide = new Stopwatch();
            collide.Start();
            if (_currentWord == 0)
            {
                SetupCollisionMap(textGeometry, ref bounds);
                geo.Freeze();
                return geo;
            }

            var bitmap = GetPixels(textGeometry, ref bounds);
            byte[] newBytes;
            using (bitmap.GetBitmapContext(ReadWriteMode.ReadOnly))
            {
                newBytes = bitmap.ToByteArray();
            }

            bounds.X = (Width - bounds.Width) / 2;
            bounds.Y = (Height - bounds.Height) / 2;
            _spiralMode = true;
            while (PerformCollisionTests(newBytes, ref bounds, ref adjustment))
            {
                if (!CalculateNextStartingPoint(ref bounds, adjustment))
                {
                    Debug.WriteLine("Failed: " + word.Word + " " + _spiralMode);
                    return null;
                }
            }

            translateTransform.X += bounds.X;
            translateTransform.Y += bounds.Y;
            UpdateMainImage(newBytes, bounds);

            collide.Stop();
            geo.Freeze();
            _lastplacedbounds = bounds;
            return geo;
        }

        private void SetupCollisionMap(Geometry textGeometry, ref Rect bounds)
        {
            var center = new Point(Width / 2 - bounds.Width / 2, Height / 2 - bounds.Height / 2);
            var centretransform = new TranslateTransform(center.X, center.Y);

            var transformGroup = textGeometry.Transform as TransformGroup;
            transformGroup?.Children.Add(centretransform);

            bounds.Width = Width;
            bounds.Height = Height;

            var mainImageBitmap = GetPixels(textGeometry, ref bounds);
            int totalPixels;
            byte[] mainImageBytes;
            using (mainImageBitmap.GetBitmapContext(ReadWriteMode.ReadOnly))
            {
                mainImageBytes = mainImageBitmap.ToByteArray();
                totalPixels = mainImageBytes.Length / Pbgra32Bytes;
            }

            _collisionMap = new bool[totalPixels];
            _collisionMapWidth = mainImageBitmap.PixelWidth;
            _collisionMapHeight = mainImageBitmap.PixelHeight;

            for (var i = 0; i < totalPixels; ++i)
            {
                _collisionMap[i] = mainImageBytes[i * Pbgra32Bytes + 3] > 0;
            }
        }

        private bool PerformCollisionTests(IReadOnlyList<byte> newBytes, ref Rect bounds, ref double adjustment)
        {
            if (bounds.Y + bounds.Height > _collisionMapHeight ||
                bounds.X + bounds.Width > _collisionMapWidth ||
                bounds.X < 0 ||
                bounds.Y < 0)
            {
                adjustment += WordCloudConstants.DoublePi / 10;

                return true;
            }

            adjustment += WordCloudConstants.DoublePi / 50;

            var srcWidth = _collisionMapWidth;
            var testBoundary = bounds;
            var testX = (int) testBoundary.X;
            var testY = (int) testBoundary.Y;
            var testWidth = (int) testBoundary.Width;
            var testHeight = (int) testBoundary.Height;

            for (var line = 0; line < testHeight; ++line)
            {
                var mapPosition = (testY + line) * srcWidth + testX;
                var testOffest = line * testWidth * Pbgra32Bytes;

                for (var i = 0; i < testWidth * Pbgra32Bytes; i += 4)
                {
                    if (_collisionMap[mapPosition + i / 4] && newBytes[testOffest + i] != Pbgra32Alpha) return true;
                }
            }

            return false;
        }

        private void UpdateMainImage(IReadOnlyList<byte> newBytes, Rect copyRectangle)
        {
            if (copyRectangle.Y + copyRectangle.Height > _collisionMapHeight ||
                copyRectangle.X + copyRectangle.Width > _collisionMapWidth ||
                copyRectangle.X + copyRectangle.Width < 0 ||
                copyRectangle.Y + copyRectangle.Height < 0)
            {
                throw new IndexOutOfRangeException("Image copyRectangle are out of range");
            }

            var srcWidth = _collisionMapWidth;
            var testX = (int) copyRectangle.X;
            var testY = (int) copyRectangle.Y;
            var testWidth = (int) copyRectangle.Width;
            var testHeight = (int) copyRectangle.Height;

            for (var line = 0; line < testHeight; ++line)
            {
                var mapPosition = (testY + line) * srcWidth + testX;
                var testOffest = line * testWidth * Pbgra32Bytes;

                for (var i = 0; i < testWidth * Pbgra32Bytes; i += Pbgra32Bytes)
                {
                    if (newBytes[testOffest + i] > 0) _collisionMap[mapPosition + i / Pbgra32Bytes] = true;
                }
            }
        }

        private bool[] _collisionMap;
        private int _collisionMapWidth;
        private int _collisionMapHeight;
        private Rect _lastplacedbounds;

        private WriteableBitmap GetPixels(Geometry existingGeo, ref Rect bounds)
        {
            var bm = new RenderTargetBitmap((int) bounds.Width, (int) bounds.Height, 96, 96, _pixelFormat);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Purple, new Pen(Brushes.Purple, 10),  existingGeo);
            }

            bm.Render(dv);

            return new WriteableBitmap(bm);
        }

        public bool Intersect(ref Rect rect1, ref Rect rect2)
        {
            if (rect2.IsEmpty || rect1.IsEmpty || rect1.Left > rect2.Right || rect1.Right < rect2.Left || rect1.Top > rect2.Bottom || rect1.Bottom >= rect2.Top)
            {
                rect1 = Rect.Empty;
                return false;
            }

            var num1 = Math.Max(rect1.Left, rect2.Left);
            var num2 = Math.Max(rect1.Top, rect2.Top);
            rect1.Width = Math.Max(Math.Min(rect1.Right, rect2.Right) - num1, 0.0);
            rect1.Height = Math.Max(Math.Min(rect1.Bottom, rect2.Bottom) - num2, 0.0);
            rect1.X = num1;
            rect1.Y = num2;
            return true;
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