﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WordCloudTest.QuadTree;
using WordCloudTest.WordCloud;
using WordCloudTest.WordClouds;

namespace WordCloudTest.Views
{
    /// <summary>
    /// Interaction logic for WordCloud2.xaml
    /// </summary>
    public partial class WordCloud2 : UserControl
    {
        private const int MaxWords = 200;
        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private readonly Dictionary<GeometryDrawing, Rect> _geoBoundsLookup = new Dictionary<GeometryDrawing, Rect>();
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        private int _currentWord = 0;
        private DrawingImage _di;
        private DrawingGroup _wordDrawingGroup;

        private CenterPoints _currentCenterPoint = CenterPoints.M;
        private double _fontMultiplier;
        private int _minWordWeight;
        private readonly DrawingGroup _mainDrawingGroup;
        private GeometryDrawing _previousCollidedWord;
        private RectQuadTree<GeometryDrawing> _pointQuadTree;
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
            _minWordWeight = _words.Min(e => e.wordWeight);
            _fontMultiplier = GetFontMultiplier();
            _pointQuadTree = new RectQuadTree<GeometryDrawing>(new Rect(0, 0, Width, Height));

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
            }

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

            var mult = adjustment / WordCloudConstants.DoublePi * WordCloudConstants.SpiralRadius;
            var angle = adjustment % WordCloudConstants.DoublePi;
            var spiralPoint = new Point(Width / 2 + mult * Math.Sin(angle), Height / 2 + mult * Math.Cos(angle));
            //return new Point(Width / 2 + mult * Math.Sin(angle), Height / 2 + mult * Math.Cos(angle));
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

            var initialPoint = new Point(Width / 2 - text.Width / 2, Height / 2 - text.Height / 2);
            var geo = new GeometryDrawing();
            geo.Brush = new SolidColorBrush(word.Color);
            geo.Geometry = text.BuildGeometry(initialPoint);
            var bounds = geo.Bounds;

            var drawPoint = new Point(Width / 2, Height / 2);

            var rotateTransform = new RotateTransform(word.Angle, drawPoint.X, drawPoint.Y);
            var transformGroup = new TransformGroup();

            transformGroup.Children.Add(rotateTransform);

            geo.Geometry.Transform = transformGroup;

            bounds = geo.Geometry.Bounds;

            var halfGeoWidth = bounds.Width / 2;
            var halfGeoHeight = bounds.Height / 2;

            initialPoint.X = bounds.X;
            initialPoint.Y = bounds.Y;
            var adjustment = 0.0;

            var colliders = new Stopwatch();
            colliders.Start();

            var translateTransform = new TranslateTransform(0.0, 0.0);
            transformGroup.Children.Add(translateTransform);

            while (PerformCollisionTests(geo, bounds, ref adjustment))
            {
                var nextPosition = CalculateNextStartingPoint(adjustment);

                bounds.X = nextPosition.X - halfGeoWidth;
                bounds.Y = nextPosition.Y - halfGeoHeight;

                translateTransform.X = bounds.X - initialPoint.X;
                translateTransform.Y = bounds.Y - initialPoint.Y;
            }


            colliders.Stop();
            _previousCollidedWord = null;
            geo.Freeze();
            _geoBoundsLookup.Add(geo, bounds);
            _pointQuadTree.Insert(geo, bounds);

            return geo;
        }

        private bool PerformCollisionTests(GeometryDrawing geo, Rect bounds, ref double adjustment)
        {
            if (_previousCollidedWord != null && adjustment > 0 && !Equals(geo, _previousCollidedWord))
            {
                if (DoGeometricDrawingsCollide(geo, _previousCollidedWord, bounds, ref adjustment)) return true;
            }

            foreach (var dr in _geoBoundsLookup.Keys)
            {
                if (DoGeometricDrawingsCollide(geo, dr, bounds, ref adjustment)) return true;
            }

            return false;
        }

        private bool DoGeometricDrawingsCollide(GeometryDrawing geo, GeometryDrawing dr, Rect bounds, ref double adjustment)
        {
            if (Equals(geo, dr)) return false;

            //TODO: Start from new place
            if (bounds.X < 0 || bounds.Y < 0 || bounds.Right > Width || bounds.Top > Height)
            {
                _previousCollidedWord = null;
                adjustment += Math.Min(bounds.Width, bounds.Height) * .1;
                return true;
            }

            if (!_geoBoundsLookup[dr].Contains(bounds) && !_geoBoundsLookup[dr].IntersectsWith(bounds))
            {
                return false;
            }

            if (geo.Geometry.FillContainsWithDetail(dr.Geometry) != IntersectionDetail.Empty)
            {
                adjustment += Math.Min(bounds.Width, bounds.Height) * .1;
                _previousCollidedWord = dr;
                return true;
            }

            return false;
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
                    if (_words.Any() && random.Next(0, 10) >= 8)
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