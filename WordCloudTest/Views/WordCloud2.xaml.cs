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
        private readonly List<WordCloudEntry> _words = new List<WordCloudEntry>();
        private Point _initScale;
        private Point _nextTransform;
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        private int _currentWord = 0;
        private Rect _geoSize = new Rect();
        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Interpris;

        public WordCloud2()
        {
            InitializeComponent();

            _initScale = new Point(5, 5);
            _nextTransform = new Point(0, 0);
        }

        public void DoStuff()
        {
            var wordCount = _words.Count;
            var word = _words[_currentWord];
            
            var text = new FormattedText(word.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Font,
                GetFontSize(word.SizeValue),
                new SolidColorBrush(word.Color),
                DpiScale.PixelsPerDip);

            switch (_currentWord)
            {
                case 1:
                    _nextTransform.X += 20;
                    break;
                case 2:

                    _nextTransform.Y += 20;
                    break;
                case 3:

                    _nextTransform.X -= 20;
                    break;
                case 4:
                    _nextTransform.Y -= 20;
                    break;
            }


            var geo = text.BuildGeometry(new Point(0, 0));
            var scale = new ScaleTransform(_initScale.X, _initScale.Y);
            var translate = new TranslateTransform(_nextTransform.X, _nextTransform.Y);


            var dg = new DrawingGroup();
            using (var context = dg.Open())
            {
                context.PushTransform(scale);
                context.PushTransform(translate);
                context.DrawGeometry(new SolidColorBrush(word.Color), null, geo);
            }
            /*
            using (var context = Groupie.Drawing.)
            {
                context.DrawDrawing(dg);
            }
            _geoSize = geo.Bounds;
*/
            _initScale.X -= 5.0 / wordCount;
            _initScale.Y -= 5.0 / wordCount;
            Debug.WriteLine(Groupie.Height + "\t" + Groupie.Width);
            _currentWord++;
        }

        private void WordCloud2_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is WordCloudData wordCloudData)) return;
            PopulateWordList(wordCloudData);
            DoStuff();
        }

        private void PopulateWordList(WordCloudData wordCloudData)
        {
            var random = new Random();
            foreach (var row in wordCloudData.Words.Rows)
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
                    if (!_words.Any() && random.Next(0, 10) >= 7)
                    {
                        angle = WordCloudConstants.MixedRotationVertical;
                    }
                }
                else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
                {
                    // First word always horizontal
                    if (!_words.Any())
                    {
                        angle = -WordCloudConstants.RandomMaxRotationAbs + random.Next(0, WordCloudConstants.RandomRange);
                    }
                }

                // At this stage, the word alpha value is set to be the same as the size value making the word color fade proportionally with word size
                _words.Add(new WordCloudEntry
                    {
                        Word = row.Item.Word,
                        SizeValue = row.Count * WordCloudConstants.WeightedFrequencyMultiplier,
                        Color = CurrentTheme.ColorList[colorIndex],
                        AlphaValue = row.Count,
                        Angle = angle
                    }
                );
            }
        }

        private int GetFontSize(int size)
        {
            var minSize = _words.Min(e => e.SizeValue);
            var maxSize = Math.Max(_words.Max(e => e.SizeValue), WordCloudConstants.MinimumLargestValue);
            var wordSizeRange = Math.Max(0.00001, maxSize - minSize);
            var areaPerLetter = GetAverageLetterPixelWidth() / wordSizeRange;
            var targetWidth = (ActualWidth + ActualHeight) / WordCloudConstants.TargetWidthFactor * WordCloudConstants.LargestSizeWidthProportion;
            var largestWord = _words.OrderByDescending(e => (e.SizeValue - minSize) * e.Word.Length).First();

// Use minimum word length of MINIMUM_LARGEST_WORD_LENGTH to avoid overscalling
            var largestWordLength = Math.Max(largestWord.Word.Length, WordCloudConstants.MinimumLargestWordLength);

            var maxWordSize = 100 / ((largestWord.SizeValue - minSize) * largestWordLength * areaPerLetter / targetWidth);

            // Reduce the maximum word size for random theme to avoid placement/collision issues due to high angle values
            if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
            {
                maxWordSize *= 0.8;
            }

            var maxFontSize = Math.Max(WordCloudConstants.MinFontSize * 2.7, maxWordSize);

            var fontMultiplier = (int) Math.Min((maxFontSize - WordCloudConstants.MinFontSize) / wordSizeRange, 200);
            return (int) (((size - minSize) * fontMultiplier) + WordCloudConstants.MinFontSize);
        }

        private double GetAverageLetterPixelWidth()
        {
            double totalOfAverages = 0.0;

// Average the letter width over the top 10 (or total count if less) words
            int wordCount = Math.Min(10, _words.Count);
            foreach (var entry in _words)
            {
                var txt = new FormattedText(entry.Word,
                    Thread.CurrentThread.CurrentCulture,
                    FlowDirection.LeftToRight,
                    CurrentTheme.Font,
                    100,
                    Brushes.Black,
                    DpiScale.PixelsPerDip);
                totalOfAverages += txt.Width / entry.Word.Length;
            }

            return (totalOfAverages / wordCount);
        }
    }
}