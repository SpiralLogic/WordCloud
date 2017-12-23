using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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
        private const int MaxWords = 200;
        private readonly PixelFormat _pixelFormat = PixelFormats.Pbgra32;
        private const int Pbgra32Bytes = 4;
        private const int Pbgra32Alpha = 0;
        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        private int _currentWord;
        private readonly DrawingGroup _wordDrawingGroup = new DrawingGroup();
        private double _cloudWidth;
        private double _cloudHeight;
        private double _fontMultiplier;
        private Point _centre = new Point();
        private int _minWordWeight;
        private readonly DrawingGroup _mainDrawingGroup;
        private bool _spiralMode = true;
        private BitArray _collisionMap;
        private int _collisionMapWidth;
        private int _collisionMapHeight;
        private Rect _lastPlacedBounds;
        private double _adjustment = 1;
        private const int Buffer = 1;

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
            _cloudWidth = Width - 4;
            _cloudHeight = Height - 4;
            _centre = new Point(_cloudWidth / 2, _cloudHeight / 2);
            _minWordWeight = _words.Min(e => e.wordWeight);
            _mainDrawingGroup.Children.Clear();
            _fontMultiplier = GetFontMultiplier();

            _fontMultiplier = GetFontMultiplier();
            _bgDrawingGroup = new DrawingGroup();

            BaseImage.Stretch = Stretch.None;

            using (var context = _bgDrawingGroup.Open())
            {
                context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Width, Height));
            }

            _bgDrawingGroup.Freeze();
            _mainDrawingGroup.Children.Add(_bgDrawingGroup);
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);
            _mainDrawingGroup.Children.Add(_debugDrawingGroup);
        }

        public void AddWord()
        {
            var word = _words[_currentWord];
            var wordGeometry = CreateWordGeometry(word);

            if (wordGeometry != null)
            {
                _wordDrawingGroup.Children.Add(wordGeometry);
            }

            _currentWord++;

            /*       _mainDrawingGroup.Children.Clear();
                   _mainDrawingGroup.Children.Add(_wordDrawingGroup);
                   var finalTransformGroup = new TransformGroup();
                   _wordDrawingGroup.Transform = finalTransformGroup;
       
                   if (_wordDrawingGroup.Bounds.Width > _cloudWidth || _wordDrawingGroup.Bounds.Height > _cloudHeight)
                   {
                       finalTransformGroup.Children.Add(new ScaleTransform(.95, .95, BaseImage.RenderSize.Width / 2, BaseImage.RenderSize.Height / 2));
                   }
       
                   finalTransformGroup.Children.Add(new TranslateTransform(-(Width - _wordDrawingGroup.Bounds.Width) / 2, -(Height - _wordDrawingGroup.Bounds.Height) / 2));
            */
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
                else
                {
                    break;

                }

                _currentWord++;
            }
            using (var c = _debugDrawingGroup.Append())
            {
                c.DrawEllipse(Brushes.Purple, null, new Point(_centre.X, _centre.Y), 5, 5);
            }
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);
            /*    var finalTransformGroup = new TransformGroup();
                _wordDrawingGroup.Transform = finalTransformGroup;
                var wordGroupBounds = _wordDrawingGroup.Bounds;
                if (wordGroupBounds.Width + Buffer > Width || wordGroupBounds.Height + Buffer > Height)
                {
                    finalTransformGroup.Children.Add(new ScaleTransform(.95, .95, Width / 2, Height / 2));
                }
                else
                {
                    finalTransformGroup.Children.Add(new TranslateTransform(Width / 2 - wordGroupBounds.Width / 2 - wordGroupBounds.X, Height / 2 - wordGroupBounds.Height / 2 - wordGroupBounds.Y));
                    BaseImage.Stretch = Stretch.Uniform;
                }
    */
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

        private DrawingGroup _debugDrawingGroup = new DrawingGroup();
        private DrawingGroup _bgDrawingGroup;
        private double _flip=1;

        private bool CalculateNextStartingPoint(WordGeo wordGeo)
        {
            if (_spiralMode && SpiralPoint(wordGeo))
            {
              
                return true;
            }

            _spiralMode = false;

            //   if (RectPoint(newWord)) return true;


            return false;
        }

        private void DrawDebugPoint(Point point, SolidColorBrush brush)
        {
            using (var c = _debugDrawingGroup.Append())
            {
                c.DrawEllipse(brush, null, new Point(point.X, point.Y), 3, 3);
            }
        }

        private bool SpiralPoint(WordGeo wordGeo)
        {
            var k =5;
            wordGeo.X += _flip*(Math.Sqrt(k * _adjustment) * Math.Cos(Math.Sqrt(k * _adjustment)))  ;
            wordGeo.Y += _flip * (Math.Sqrt(k * _adjustment) * Math.Sin(Math.Sqrt(k * _adjustment)));


            _adjustment++;
            /*      if (wordGeo.Right > _cloudWidth || wordGeo.Bottom > _cloudHeight || wordGeo.X < 0 || wordGeo.Y < 0)
                  {
                      var count = 0;
                      if (wordGeo.Right > _cloudWidth) count++;
                      if (wordGeo.Bottom > _cloudHeight) count++;
                      if (wordGeo.X < 0) count++;
                      if (wordGeo.Y < 0) count++;
      
                      if (count > 1) return false;
      
                      _centre.X = _lastPlacedBounds.X + _cloudWidth / 2 * (_lastPlacedBounds.X > _cloudWidth / 2 ? -1 : 1);
                      _centre.Y = _lastPlacedBounds.Y + _cloudHeight / 2 * (_lastPlacedBounds.Y > _cloudHeight / 2 ? -1 : 1);
                      _adjustment = 1;
                      wordGeo.X = _cloudWidth / 2 - wordGeo.Width  * Math.Sqrt(k * _adjustment) * Math.Cos(Math.Sqrt(k * _adjustment)) + _centre.X;
                      wordGeo.Y = _cloudHeight / 2 - wordGeo.Height  * Math.Sqrt(k * _adjustment) * Math.Sin(Math.Sqrt(k * _adjustment)) + _centre.Y;
                  }*/

            return true;
        }


        private GeometryDrawing CreateWordGeometry(WordCloudEntry word)
        {
            _flip *= -1;
            _debugDrawingGroup.Children.Clear();

            var text = new FormattedText(word.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Typeface,
                GetFontSize(word.wordWeight),
                word.Color,
                DpiScale.PixelsPerDip);

            var textGeometry = text.BuildGeometry(new Point(0, 0));
            var wordGeo = new WordGeo(textGeometry, word);

            if (_collisionMap == null)
            {
                CreateCollisionMap(wordGeo);

                return wordGeo.GetDrawing();
            }

            var newBytes = GetPixels(wordGeo, wordGeo.IntWidth, wordGeo.IntHeight);
            wordGeo.X += _centre.X - wordGeo.Center.X;
            wordGeo.Y += _centre.Y - wordGeo.Center.Y;
            _spiralMode = true;
            _adjustment = 1;
            DrawDebugPoint(new Point(wordGeo.X, wordGeo.Y), Brushes.Blue);

            while (HasCollision(newBytes, wordGeo))
            {
                DrawDebugPoint(new Point(wordGeo.X + wordGeo.Center.X, wordGeo.Y + wordGeo.Center.Y), Brushes.Black);

                if (!CalculateNextStartingPoint(wordGeo) || IsOutOfBounds(wordGeo))
                {
                    return null;
                }
            }
            AdjustFinalPosition(newBytes, wordGeo);


            DrawDebugPoint(new Point(wordGeo.X , wordGeo.Y ), Brushes.Red);

            UpdateCollisionMap(newBytes, wordGeo);
            return wordGeo.GetDrawing();
        }

        private void AdjustFinalPosition(byte[] newBytes, WordGeo wordGeo)
        {
            var previousX = wordGeo.X;
            while (!HasCollision(newBytes, wordGeo) && Math.Abs(_centre.X - wordGeo.X) > Buffer*3)
            {
                previousX = wordGeo.X;
                wordGeo.X += wordGeo.X > _centre.X ? -Buffer*3 : Buffer*3;
            }
            wordGeo.X = previousX;

            var previousY = wordGeo.Y;
            while (!HasCollision(newBytes, wordGeo) && Math.Abs(_centre.Y - wordGeo.Y) > Buffer*3)
            {
                previousY = wordGeo.Y;
                wordGeo.Y += wordGeo.Y > _centre.Y ? -Buffer*3 : Buffer*3;
            }
            wordGeo.Y = previousY;
        }

        private void CreateCollisionMap(WordGeo wordGeo)
        {
            wordGeo.X += _centre.X - wordGeo.Center.X;
            wordGeo.Y += _centre.Y - wordGeo.Center.Y;
            _collisionMapWidth = (int) _cloudWidth;
            _collisionMapHeight = (int) _cloudHeight;


            var mainImageBytes = GetPixels(wordGeo, _collisionMapWidth, _collisionMapHeight);
            var totalPixels = _collisionMapHeight * _collisionMapWidth;

            _collisionMap = new BitArray(totalPixels);

            for (var i = 0; i < totalPixels - Pbgra32Bytes; ++i)
            {
                if (mainImageBytes[i * Pbgra32Bytes + 3] > 0) AddNewCollisionPoint(i);
            }
        }

        private void AddNewCollisionPoint(int index)
        {
            var y = index / _collisionMapWidth;
            var x = index % _collisionMapWidth;
            _collisionMap[index] = true;

            for (var i = 1; i <= Buffer; i++)
            {
                if (x < _collisionMapWidth - i) _collisionMap[index + i] = true;
                if (y < _collisionMapHeight - i) _collisionMap[index + _collisionMapWidth * i] = true;
                if (x > i - 1) _collisionMap[index - i] = true;
                if (y > i - 1) _collisionMap[index - _collisionMapWidth * i] = true;
            }
        }

        private bool HasCollision(IReadOnlyList<byte> newWordBytes, WordGeo newWord)
        {
            if (IsOutOfBounds(newWord)) return true;

            var mapWidth = _collisionMapWidth;

            var testX = newWord.IntX;
            var testY = newWord.IntY;
            var testWidth = newWord.IntWidth;
            var testHeight = newWord.IntHeight;
            var testOffset = 0;
            var mapPosition = testY * mapWidth + testX;
            for (var line = 0; line < testHeight; ++line)
            {
                for (var i = 0; i < testWidth; ++i)
                {
                    if (_collisionMap[mapPosition + i] && newWordBytes[testOffset + i * Pbgra32Bytes] != Pbgra32Alpha) return true;
                }
                testOffset += testWidth;
                mapPosition += mapWidth;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOutOfBounds(WordGeo wordGeo)
        {
            return wordGeo.Right > _cloudWidth || wordGeo.Bottom > _cloudHeight || wordGeo.X < 0 || wordGeo.Y < 0;
        }

        private void UpdateCollisionMap(IReadOnlyList<byte> newBytes, WordGeo wordGeo)
        {
            if (wordGeo.Bottom > _collisionMapHeight ||
                wordGeo.Right > _collisionMapWidth ||
                wordGeo.Right < 0 ||
                wordGeo.Bottom < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newBytes));
            }

            _lastPlacedBounds = new Rect(wordGeo.X, wordGeo.Y, wordGeo.Width, wordGeo.Height);

            var srcWidth = _collisionMapWidth;
            var testX = wordGeo.IntX;
            var testY = wordGeo.IntY;
            var testWidth = wordGeo.IntWidth;
            var testHeight = wordGeo.IntHeight;

            for (var line = 0; line < testHeight; ++line)
            {
                var mapPosition = (testY + line) * srcWidth + testX;
                var testOffset = line * testWidth * Pbgra32Bytes;

                for (var i = 0; i < testWidth * Pbgra32Bytes; i += Pbgra32Bytes)
                {
                    if (newBytes[testOffset + i] > 0) AddNewCollisionPoint(mapPosition + i / Pbgra32Bytes);
                }
            }
        }

        private byte[] GetPixels(WordGeo wordGeo, int width, int height)
        {
            var bm = new RenderTargetBitmap(width, height, 96, 96, _pixelFormat);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Purple, new Pen(Brushes.Purple, 10), wordGeo.Geo);
            }
            bm.Render(dv);

            var bitmap = new WriteableBitmap(bm);
            var bitmapStride = (bitmap.PixelWidth * _pixelFormat.BitsPerPixel + 7) / 8;
            var newBytes = new byte[bitmap.PixelHeight * bitmapStride];
            bitmap.CopyPixels(newBytes, bitmapStride, 0);

            return newBytes;
        }


        private void PopulateWordList(WordCloudData wordCloudData)
        {
            // Create a byte array to hold the random value.
            var randomNumber = new byte[1];

            // Create a new instance of the RNGCryptoServiceProvider. 
            using (var gen = new RNGCryptoServiceProvider())
            {
                foreach (var row in wordCloudData.Words.Rows.Take(MaxWords))
                {
                    var colorIndex = 0;
                    var angle = WordCloudConstants.NoRotation;

                    // Theme's define a color list, randomly assign one by index
                    if (CurrentTheme.ColorList.Count > 1)
                    {
                        gen.GetBytes(randomNumber);
                        var rand = Convert.ToInt32(randomNumber[0]);

                        colorIndex = rand % CurrentTheme.ColorList.Count;
                    }

                    if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Mixed)
                    {
                        gen.GetBytes(randomNumber);
                        var rand = Convert.ToInt32(randomNumber[0]);

                        // First word always horizontal 70% Horizontal (default), 30% Vertical
                        if (_words.Any() && rand % 10 >= 7)
                        {
                            angle = WordCloudConstants.MixedRotationVertical;
                        }
                    }
                    else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
                    {
                        // First word always horizontal
                        if (!_words.Any())
                        {
                            gen.GetBytes(randomNumber);
                            var rand = Convert.ToInt32(randomNumber[0]);
                            angle = -WordCloudConstants.RandomMaxRotationAbs + rand % WordCloudConstants.RandomRange;
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
        }

        private double GetFontMultiplier()
        {
            var maxWordWeight = Math.Max(_words.Max(e => e.wordWeight), WordCloudConstants.MinimumLargestValue);
            var wordWeightRange = Math.Max(0.00001, maxWordWeight - _minWordWeight);
            var areaPerLetter = GetAverageLetterPixelWidth() / wordWeightRange;

            var targetWidth = (_cloudWidth + _cloudHeight) / WordCloudConstants.TargetWidthFactor * WordCloudConstants.LargestSizeWidthProportion;
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
    }

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

        public int IntWidth => (int) Math.Ceiling(_bounds.Width);
        public int IntHeight => (int) Math.Ceiling(_bounds.Height);
        public int IntX => (int) Math.Ceiling(_bounds.X);
        public int IntY => (int) Math.Ceiling(_bounds.Y);

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