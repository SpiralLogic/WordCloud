﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int MaxWords = 100;
        private readonly PixelFormat _pixelFormat = PixelFormats.Pbgra32;
        private const int Pbgra32Bytes = 4;
        private const int Pbgra32Alpha = 0;
        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        private int _currentWord;
        private DrawingGroup _wordDrawingGroup;
        private double _cloudWidth;
        private double _cloudHeight;
        private double _fontMultiplier;
        private int _minWordWeight;
        private readonly DrawingGroup _mainDrawingGroup;
        private bool _spiralMode = true;

        private BitArray _collisionMap;
        private int _collisionMapWidth;
        private int _collisionMapHeight;
        private Rect _lastPlacedBounds;
        private double _adjustment;
        private const int Buffer = 3;

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
            _cloudWidth = Width;
            _cloudHeight = Height;
            _minWordWeight = _words.Min(e => e.wordWeight);
            _fontMultiplier = GetFontMultiplier();
            var bgDrawingGroup = new DrawingGroup();
            _wordDrawingGroup = new DrawingGroup();
            BaseImage.Stretch = Stretch.None;

            using (var context = bgDrawingGroup.Open())
            {
                context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, _cloudWidth, _cloudHeight));
            }

            bgDrawingGroup.Freeze();
            _wordDrawingGroup.Transform = null;
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

            _mainDrawingGroup.Children.Clear();
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);
            var finalTransformGroup = new TransformGroup();
            _wordDrawingGroup.Transform = finalTransformGroup;

            if (_wordDrawingGroup.Bounds.Width > _cloudWidth || _wordDrawingGroup.Bounds.Height > _cloudHeight)
            {
                finalTransformGroup.Children.Add(new ScaleTransform(.95, .95, BaseImage.RenderSize.Width / 2, BaseImage.RenderSize.Height / 2));
            }

            finalTransformGroup.Children.Add(new TranslateTransform(-(_cloudWidth - _wordDrawingGroup.Bounds.Width) / 2, -(_cloudHeight - _wordDrawingGroup.Bounds.Height) / 2));
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

            _mainDrawingGroup.Children.Clear();
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);
            var finalTransformGroup = new TransformGroup();
            _wordDrawingGroup.Transform = finalTransformGroup;
            var wordGroupBounds = _wordDrawingGroup.Bounds;
            if (wordGroupBounds.Width > _cloudWidth || wordGroupBounds.Height > _cloudHeight)
            {
                finalTransformGroup.Children.Add(new ScaleTransform(.95, .95, BaseImage.RenderSize.Width / 2, BaseImage.RenderSize.Height / 2));
            }
            else
            {
                BaseImage.Stretch = Stretch.Uniform;
            }

            finalTransformGroup.Children.Add(new TranslateTransform(-(_cloudWidth - wordGroupBounds.Width) / 2, -(_cloudHeight - wordGroupBounds.Height) / 2));

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

        private bool CalculateNextStartingPoint(ref Rect bounds)
        {
            if (_spiralMode && SpiralPoint(ref bounds)) return true;

            _spiralMode = false;

            if (RectPoint(ref bounds)) return true;


            return false;
        }

        private bool SpiralPoint(ref Rect bounds)
        {
            if (bounds.Bottom > _collisionMapHeight || bounds.Right > _collisionMapWidth || bounds.X < 0 || bounds.Y < 0)
            {
                _adjustment += WordCloudConstants.DoublePi / 40;
            }
            else
            {
                _adjustment += WordCloudConstants.DoublePi / 50;
            }

            if (_adjustment > WordCloudConstants.MaxSprialLength)
            {
                return false;
            }


            var multi = _adjustment / WordCloudConstants.DoublePi * WordCloudConstants.SpiralRadius;
            var angle = _adjustment % WordCloudConstants.DoublePi;

            bounds.X = _cloudWidth / 2 + multi * -Math.Sin(angle);
            bounds.Y = _cloudHeight / 2 + multi * -Math.Cos(angle);

            return true;
        }

        /*        private bool RandomGreedy(ref Rect bounds)
                {

                }*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool RectPoint(ref Rect bounds)
        {
            if (_lastPlacedBounds == Rect.Empty) return false;


            var maxRight = _cloudWidth;
            var maxBottom = _cloudHeight;

            if (bounds.Right + _adjustment < maxRight && bounds.Bottom > (_cloudWidth - Buffer * 2) / 2)
            {
                bounds.X += _adjustment;

                return true;
            }

            if (bounds.Right + _adjustment > maxRight && bounds.Bottom + _adjustment < maxBottom && bounds.Right > (_cloudWidth - Buffer * 2) / 2)
            {
                bounds.X = maxRight;
                bounds.Y += _adjustment;

                return true;
            }

            if (bounds.Right - _adjustment > 0 && bounds.Bottom + _adjustment > maxBottom)
            {
                bounds.X -= _adjustment;
                bounds.Y = maxBottom;

                return true;
            }

            if (bounds.Right - _adjustment > 0 && bounds.Bottom - _adjustment > 0)
            {
                bounds.Y -= _adjustment;
                bounds.X = 4;

                return true;
            }

            _lastPlacedBounds = Rect.Empty;
            return false;
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
            var bounds = textGeometry.Bounds;
            var transformGroup = new TransformGroup();

            var translateTransform = new TranslateTransform(-bounds.X, -bounds.Y);
            transformGroup.Children.Add(translateTransform);
            bounds = translateTransform.TransformBounds(bounds);


            var rotateTransform = new RotateTransform(-90, bounds.Width / 2, bounds.Height / 2);
            transformGroup.Children.Add(rotateTransform);
            bounds = rotateTransform.TransformBounds(bounds);

            textGeometry.Transform = transformGroup;

            bounds = rotateTransform.TransformBounds(bounds);


            var geo = new GeometryDrawing
            {
                Geometry = textGeometry,
                Brush = word.Color,
            };


            var collide = new Stopwatch();
            collide.Start();
            if (_currentWord == 0)
            {
                SetupCollisionMap(textGeometry, ref bounds);
                geo.Freeze();
                return geo;
            }

            var newBytes = GetPixels(textGeometry,(int) bounds.Width,(int)bounds.Height);


            bounds.X = (_cloudWidth - bounds.Width) / 2;
            bounds.Y = (_cloudHeight - bounds.Height) / 2;
            _spiralMode = true;
            _adjustment = 0.0;
            while (PerformCollisionTests(newBytes, ref bounds))
            {
                if (!CalculateNextStartingPoint(ref bounds))
                {
                    return null;
                }
            }

            translateTransform.X += bounds.X;
            translateTransform.Y += bounds.Y;
            UpdateMainImage(newBytes, bounds);

            collide.Stop();
            geo.Freeze();
            _lastPlacedBounds = bounds;
            return geo;
        }

        private void SetupCollisionMap(Geometry textGeometry, ref Rect bounds)
        {
            var centerTransform = new TranslateTransform(_cloudWidth / 2 - bounds.Width / 2, _cloudHeight / 2 - bounds.Height / 2);

            var transformGroup = textGeometry.Transform as TransformGroup;
            transformGroup?.Children.Add(centerTransform);

            bounds.Width = _cloudWidth;
            bounds.Height = _cloudHeight;
            _collisionMapWidth = (int) bounds.Width;
            _collisionMapHeight = (int) bounds.Height;
            var mainImageBytes = GetPixels(textGeometry, _collisionMapWidth, _collisionMapHeight);
            var totalPixels = _collisionMapHeight * _collisionMapWidth;


            _collisionMap = new BitArray(totalPixels);


            for (var i = 0; i < totalPixels - 4; ++i)
            {
                if (mainImageBytes[i * Pbgra32Bytes + 3] > 0) AddNewCollisionPoint(i);
            }
        }

        private void AddNewCollisionPoint(int i)
        {
            var y = i / _collisionMapWidth;
            var x = i % _collisionMapWidth;
            _collisionMap[i] = true;

            if (x + Buffer < _collisionMapWidth) _collisionMap[i + Buffer] = true;
            if (y + Buffer < _collisionMapHeight) _collisionMap[i + _collisionMapHeight * Buffer] = true;
            if (x - Buffer > 0) _collisionMap[i - Buffer] = true;
            if (y - Buffer > 0) _collisionMap[i - _collisionMapHeight * Buffer] = true;
        }

        private bool PerformCollisionTests(IReadOnlyList<byte> newBytes, ref Rect bounds)
        {
            if (bounds.Right > _cloudWidth - Buffer || bounds.Bottom > _cloudHeight - Buffer || bounds.X < Buffer || bounds.Y < Buffer) return true;

            var srcWidth = _collisionMapWidth;
            var testBoundary = bounds;
            var testX = (int) testBoundary.X;
            var testY = (int) testBoundary.Y;
            var testWidth = (int) testBoundary.Width;
            var testHeight = (int) testBoundary.Height;
            var testOffset = 0;
            var mapPosition = testY * srcWidth + testX;
            for (var line = 0; line < testHeight; ++line)
            {
                for (var i = 0; i < testWidth; ++i)
                {
                    if (_collisionMap[mapPosition + i] && newBytes[testOffset + i * 4] != Pbgra32Alpha) return true;
                }
                testOffset += testWidth;
                mapPosition += srcWidth;
            }

            return false;
        }

        private void UpdateMainImage(IReadOnlyList<byte> newBytes, Rect copyRectangle)
        {
            if (copyRectangle.Bottom > _collisionMapHeight ||
                copyRectangle.Right > _collisionMapWidth ||
                copyRectangle.Right < 0 ||
                copyRectangle.Bottom < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newBytes));
            }

            var srcWidth = _collisionMapWidth;
            var testX = (int) copyRectangle.X;
            var testY = (int) copyRectangle.Y;
            var testWidth = (int) copyRectangle.Width;
            var testHeight = (int) copyRectangle.Height;

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

        private byte[] GetPixels(Geometry existingGeo, int width, int height)
        {
            var bm = new RenderTargetBitmap(width, height, 96, 96, _pixelFormat);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Purple, new Pen(Brushes.Purple, 10), existingGeo);
            }
            bm.Render(dv);

            var bitmap = new WriteableBitmap(bm);
            var newBytes = new byte[bitmap.PixelHeight * (bitmap.PixelWidth * _pixelFormat.BitsPerPixel + 7) / 8];
            bitmap.CopyPixels(newBytes, (bitmap.PixelWidth * _pixelFormat.BitsPerPixel + 7) / 8, 0);

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
        private TransformGroup _transformGroup = new TransformGroup();

        public WordGeo(Geometry geo, WordCloudEntry wordEntry)
        {
            Geo = geo;
            _bounds = geo.Bounds;
            
            var rotateTransform = new RotateTransform(wordEntry.Angle, _bounds.Width / 2, _bounds.Height / 2);
            _transformGroup.Children.Add(rotateTransform);

            _bounds = rotateTransform.TransformBounds(_bounds);

            var translateTransform = new TranslateTransform(-_bounds.X, -_bounds.Y);
            _transformGroup.Children.Add(translateTransform);

            geo.Transform = _transformGroup;
        }

        public Geometry Geo { get; set; }

        public void Translate(int x, int y)
        {
            
        }
    }
}