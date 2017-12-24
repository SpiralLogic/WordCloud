using System;
using System.Collections;
using System.Collections.Generic;
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
        private const int MaxWords = 300;

        private int _currentWord;
        private int _minWordWeight;
        private double _fontMultiplier;

        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private readonly DrawingGroup _wordDrawingGroup = new DrawingGroup();
        private readonly DrawingGroup _mainDrawingGroup = new DrawingGroup();
        private readonly DrawingGroup _bgDrawingGroup = new DrawingGroup();

        private CloudSpace _cloudSpace;
        private readonly IRandomizer _randomizer;
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);

        public WordCloud2()
        {
            InitializeComponent();

            var di = new DrawingImage {Drawing = _mainDrawingGroup};

            BaseImage.Source = di;
            BaseImage.Stretch = Stretch.None;
            _randomizer = new CryptoRandomizer();
        }

        public WordCloud2(IRandomizer randomizer) : this()
        {
            _randomizer = randomizer ?? new CryptoRandomizer();
        }

        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Interpris;
        public Rect LastAddedBounds { get; private set; }
        public int Failures => _cloudSpace.FailedPlacements;
        
            private void Setup()
        {
            _cloudSpace = new CloudSpace(Width - 4, Height - 4, _randomizer);

            _minWordWeight = _words.Min(e => e.wordWeight);
            _fontMultiplier = GetFontMultiplier();

            _mainDrawingGroup.Children.Clear();
            _bgDrawingGroup.Children.Clear();

            using (var context = _bgDrawingGroup.Open())
            {
                context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Width, Height));
            }

            _mainDrawingGroup.Children.Add(_bgDrawingGroup);
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);
        }

        private void AddWordInternal()
        {
            var word = _words[_currentWord];
            var wordGeometry = CreateWordGeometry(word);

            if (wordGeometry != null)
            {
                _wordDrawingGroup.Children.Add(wordGeometry);
                    LastAddedBounds = wordGeometry.Bounds;
            }

            _currentWord++;

            _mainDrawingGroup.Children.Clear();
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);

            RecenterFinishedWordGroup();
        }

        private void AddWordsInternal()
        {
            foreach (var word in _words)
            {
                var geo = CreateWordGeometry(word);

                if (geo != null)
                {
                    _wordDrawingGroup.Children.Add(geo);
                    LastAddedBounds = geo.Bounds;
                }
                _currentWord++;
            }

            _mainDrawingGroup.Children.Remove(_bgDrawingGroup);

            RecenterFinishedWordGroup();

            _currentWord = 0;
        }

        private void RecenterFinishedWordGroup()
        {
            var finalTransformGroup = new TransformGroup();
            _wordDrawingGroup.Transform = finalTransformGroup;

            var wordGroupBounds = _wordDrawingGroup.Bounds;
            if (wordGroupBounds.Width > Width || wordGroupBounds.Height > Height)
            {
                finalTransformGroup.Children.Add(new ScaleTransform(.95, .95, Width / 2, Height / 2));
                BaseImage.Stretch = Stretch.None;
            }
            else
            {
                finalTransformGroup.Children.Add(new TranslateTransform((Width - wordGroupBounds.Width) / 2 - wordGroupBounds.X, (Height - wordGroupBounds.Height) / 2 - wordGroupBounds.Y));
                BaseImage.Stretch = Stretch.Uniform;
            }
        }

        public void AddWord(WordCloudData wordCloudData)
        {
            if (_currentWord == _words.Count)
            {
                PopulateWordList(wordCloudData);
            }

            if (_mainDrawingGroup.Children.Count == 0)
            {
                Setup();
            }

            AddWordInternal();
        }

        public void AddWords(WordCloudData wordCloudData)
        {
            PopulateWordList(wordCloudData);
            Setup();
            AddWordsInternal();
        }

        private GeometryDrawing CreateWordGeometry(WordCloudEntry word)
        {
            var text = new FormattedText(word.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Typeface,
                GetFontSize(word.wordWeight),
                word.Color,
                DpiScale.PixelsPerDip);

            var textGeometry = text.BuildGeometry(new Point(0, 0));
            var wordGeo = new WordGeo(textGeometry, word);

            
            return _cloudSpace.AddWordGeometry(wordGeo) ? wordGeo.GetDrawing(): null;
        }


/*
        private void AdjustFinalPosition(byte[] newBytes, WordGeo wordGeo)
        {
            var previousX = wordGeo.X;
            while (!HasCollision(newBytes, wordGeo) && Math.Abs(CloudCenter.X - wordGeo.X) > Buffer * 3)
            {
                previousX = wordGeo.X;
                wordGeo.X += wordGeo.X > CloudCenter.X ? -Buffer * 3 : Buffer * 3;
            }
            wordGeo.X = previousX;

            var previousY = wordGeo.Y;
            while (!HasCollision(newBytes, wordGeo) && Math.Abs(CloudCenter.Y - wordGeo.Y) > Buffer * 3)
            {
                previousY = wordGeo.Y;
                wordGeo.Y += wordGeo.Y > CloudCenter.Y ? -Buffer * 3 : Buffer * 3;
            }
            wordGeo.Y = previousY;
        }
*/


        private void PopulateWordList(WordCloudData wordCloudData)
        {
            foreach (var row in wordCloudData.Words.Rows.Take(MaxWords))
            {
                var colorIndex = 0;
                var angle = WordCloudConstants.NoRotation;

                // Theme's define a color list, randomly assign one by index
                if (CurrentTheme.ColorList.Count > 1)
                {
                    colorIndex = _randomizer.RandomInt(CurrentTheme.ColorList.Count);
                }

                if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Mixed)
                {
                    // First word always horizontal 70% Horizontal (default), 30% Vertical
                    if (_words.Any() && _randomizer.RandomInt(10) >= 7)
                    {
                        angle = WordCloudConstants.MixedRotationVertical;
                    }
                }
                else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
                {
                    // First word always horizontal
                    if (!_words.Any())
                    {
                        angle = -WordCloudConstants.RandomMaxRotationAbs + _randomizer.RandomInt(WordCloudConstants.RandomRange);
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

            var targetWidth = (_cloudSpace.Width + _cloudSpace.Height) / WordCloudConstants.TargetWidthFactor * WordCloudConstants.LargestSizeWidthProportion;
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

        public int IntX => (int) Math.Ceiling(_bounds.X);
        public int IntY => (int) Math.Ceiling(_bounds.Y);

        public int IntBottom => IntY + IntHeight;
        public int IntRight => IntX + IntWidth;

        public Point Start;

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

    internal interface IPositioner
    {
        double Delta { get; set; }
        double Chord { get; set; }
        double StartX { get; set; }
        double StartY { get; set; }
        bool GetNextPoint(out double x, out double y);
    }

    class SpiralPositioner : IPositioner
    {
        private readonly double _awayStep;

        public SpiralPositioner(Point centerPoint)
        {
            var coils = Math.Max(centerPoint.X, centerPoint.Y) / Chord;
            DeltaMax = coils * 2 * Math.PI;

            _awayStep = Math.Max(centerPoint.X, centerPoint.Y) / DeltaMax;
        }

        public double DeltaMax { get; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double Delta { get; set; } = 1;
        public double Chord { get; set; } = 10;

        public bool GetNextPoint(out double x, out double y)
        {
            const double rotation = Math.PI / 30;

            var away = _awayStep * Delta;

            x = StartX + Math.Cos(Delta + rotation) * away;
            y = StartY + Math.Sin(Delta + rotation) * away;

            Delta += Chord / away;

            return Delta <= DeltaMax;
        }
    }

    class CloudSpace
    {
        private const StartPosition DefaultStartingPosition = StartPosition.Center;

        private readonly PixelFormat _pixelFormat = PixelFormats.Pbgra32;
        private readonly IRandomizer _randomizer;
        private readonly IPositioner _positioner;

        private int RepositionAttempts = 10;
        private const int Pbgra32Bytes = 4;
        private const int Pbgra32Alpha = 0;
        public double Width { get; }
        public double Height { get; }
        public Point CloudCenter;

        private BitArray _collisionMap;
        private int _collisionMapWidth;

        private int _collisionMapHeight;
        public int FailedPlacements { get; private set; } = 0;

        //  private Rect _lastPlacedBounds;

        private const int Buffer = 2;

        public CloudSpace(double width, double height)
        {
            Width = width;
            Height = height;
            CloudCenter = new Point(width / 2, height / 2);
            _positioner = new SpiralPositioner(CloudCenter);
        }

        public CloudSpace(double width, double height, IRandomizer randomizer = null) : this(width, height)
        {
            _randomizer = randomizer ?? new CryptoRandomizer();
            _positioner = new SpiralPositioner(CloudCenter);
        }

        private bool CalculateNextStartingPoint(WordGeo wordGeo)
        {
            if (!_positioner.GetNextPoint(out var x, out var y)) return false;
            
            wordGeo.X = x;
            wordGeo.Y = y;

            return true;
        }


        private void CreateCollisionMap(WordGeo wordGeo)
        {
            SetStartingPosition(wordGeo, StartPosition.Center);
            _collisionMapWidth = (int) Width;
            _collisionMapHeight = (int) Height;

            var mainImageBytes = GetPixels(wordGeo, _collisionMapWidth, _collisionMapHeight);
            var totalPixels = _collisionMapHeight * _collisionMapWidth;

            _collisionMap = new BitArray(totalPixels);

            for (var i = 0; i < totalPixels - Pbgra32Bytes; ++i)
            {
                if (mainImageBytes[i * Pbgra32Bytes + 3] > 0) AddNewCollisionPoint(i);
            }
        }

        public void SetStartingPosition(WordGeo wordGeo, StartPosition position)
        {
            _positioner.Delta = wordGeo.Width / wordGeo.Height;
            switch (position)
            {
                case StartPosition.Center:
                    _positioner.StartX = CloudCenter.X - wordGeo.Center.X;
                    _positioner.StartY = CloudCenter.Y - wordGeo.Center.Y;
                    break;
                case StartPosition.Random:
                    _positioner.StartX = wordGeo.Center.X + _randomizer.RandomInt((int) (_collisionMapWidth - wordGeo.Center.X));
                    _positioner.StartY = wordGeo.Center.Y + _randomizer.RandomInt((int) (_collisionMapHeight - wordGeo.Center.Y));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }

            CalculateNextStartingPoint(wordGeo);
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
                for (var j = 0; j < testWidth; ++j)
                {
                    var mapIndex = mapPosition + j;
                    var testIndex = testOffset + j * Pbgra32Bytes;
                    var isCollisionPoint = newWordBytes[testIndex] != Pbgra32Alpha;
                    if (_collisionMap[mapIndex] && isCollisionPoint) return true;

                    var y = mapIndex / _collisionMapWidth;
                    var x = mapIndex % _collisionMapWidth;

                    for (var i = 1; i <= Buffer; i++)
                    {
                        if (x < _collisionMapWidth - i && _collisionMap[mapIndex + i]) return true;
                        if (y < _collisionMapHeight - i && _collisionMap[mapIndex + _collisionMapWidth * i]) return true;
                        if (x > i - 1 && _collisionMap[mapIndex - i]) return true;
                        if (y > i - 1 && _collisionMap[mapIndex - _collisionMapWidth * i]) return true;
                    }
                }
                testOffset += testWidth;
                mapPosition += mapWidth;
            }

            return false;
        }

        private byte[] GetPixels(WordGeo wordGeo, int width, int height)
        {
            var bm = new RenderTargetBitmap(width, height, 96, 96, _pixelFormat);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Purple, new Pen(Brushes.Purple, Buffer), wordGeo.Geo);
            }
            bm.Render(dv);

            var bitmap = new WriteableBitmap(bm);
            var bitmapStride = (bitmap.PixelWidth * _pixelFormat.BitsPerPixel + 7) / 8;
            var newBytes = new byte[bitmap.PixelHeight * bitmapStride];
            bitmap.CopyPixels(newBytes, bitmapStride, 0);

            return newBytes;
        }

        private void UpdateCollisionMap(IReadOnlyList<byte> newBytes, WordGeo wordGeo)
        {
            if (wordGeo.IntBottom > _collisionMapHeight ||
                wordGeo.IntRight > _collisionMapWidth ||
                wordGeo.IntRight < 0 ||
                wordGeo.IntBottom < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newBytes));
            }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOutOfBounds(WordGeo wordGeo)
        {
            return wordGeo.IntRight > _collisionMapWidth || wordGeo.IntBottom > _collisionMapHeight || wordGeo.X < 0 || wordGeo.Y < 0;
        }

        public bool AddWordGeometry(WordGeo wordGeo)
        {
            if (_collisionMap == null)
            {
                CreateCollisionMap(wordGeo);
                return true;
            }

            var newBytes = GetPixels(wordGeo, wordGeo.IntWidth, wordGeo.IntHeight);
            SetStartingPosition(wordGeo, DefaultStartingPosition);

            var attempts = 0;
            while (HasCollision(newBytes, wordGeo))
            {
                if (!CalculateNextStartingPoint(wordGeo) || IsOutOfBounds(wordGeo))
                {
                    if (attempts > RepositionAttempts)
                    {
                        FailedPlacements++;
                        return false;
                    }

                    SetStartingPosition(wordGeo, StartPosition.Random);
                    attempts++;
                }
            }
            //      AdjustFinalPosition(newBytes, wordGeo);

            UpdateCollisionMap(newBytes, wordGeo);

            return true;
        }
    }

    enum StartPosition
    {
        Center,
        Random,
    }

    public interface IRandomizer
    {
        int RandomInt(int max);
    }


    internal class CryptoRandomizer : IRandomizer
    {
        int IRandomizer.RandomInt(int max)
        {
            // Create a byte array to hold the random value.
            var byteArray = new byte[4];

            using (var gen = new RNGCryptoServiceProvider())
            {
                gen.GetBytes(byteArray);
                return Math.Abs(BitConverter.ToInt32(byteArray, 0) % max);
            }
        }
    }
}