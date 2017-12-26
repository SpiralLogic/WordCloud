using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

using System.Windows.Input;
using WordCloud.WordCloud;

namespace WordCloud.Views
{
    /// <summary>
    /// Interaction logic for WordCloud.xaml
    /// </summary>
    public partial class WordCloud
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

        public WordCloud()
        {
            InitializeComponent();

            var di = new DrawingImage {Drawing = _mainDrawingGroup};

            BaseImage.Source = di;
            BaseImage.Stretch = Stretch.None;
            _randomizer = new CryptoRandomizer();
        }

        public WordCloud(IRandomizer randomizer) : this()
        {
            _randomizer = randomizer ?? new CryptoRandomizer();
        }

        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Interpris;
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

        private async Task AddWordInternal()
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

            RecenterFinishedWordGroup();
        }

        private async Task AddWordsInternal()
        {
            var geoList = new BlockingCollection<Drawing>();
            var addTask = Task.Run(() =>
            {
                foreach (var word in _words)
                {
                    var geo = CreateWordGeometry(word);

                    if (geo != null)
                    {
                        geoList.Add(geo);
                    }
                    _currentWord++;
                }
                geoList.CompleteAdding();
            });
            var displayTask = Task.Run(() =>
            {
                while (!geoList.IsAddingCompleted || geoList.Count > 0)
                {
                    var add = geoList.Take();
                    Dispatcher.InvokeAsync(() =>
                    {
                        DoubleAnimation opacityAnimation = new DoubleAnimation();
                        opacityAnimation.To = 0.0;
                        opacityAnimation.Duration = TimeSpan.FromSeconds(0.5);
                        opacityAnimation.AutoReverse = true;
                        Storyboard.SetTargetName(opacityAnimation, "MyAnimatedBrush");
                        Storyboard.SetTargetProperty(
                            opacityAnimation, new PropertyPath(SolidColorBrush.OpacityProperty));
                        Storyboard mouseLeftButtonDownStoryboard = new Storyboard();
                        mouseLeftButtonDownStoryboard.Children.Add(opacityAnimation);
                        add.MouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e)
                        {
                            mouseLeftButtonDownStoryboard.Begin(this);
                        }; _wordDrawingGroup.Children.Add(add);
                    });
                }
            });

            await Task.WhenAll(addTask, displayTask);
            geoList.Dispose();

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

  //           AddCollisionDebug();
        }

        private void AddCollisionDebug()
        {
            var dg = new DrawingGroup();
            using (var c = dg.Open())
            {
                for (var y = 0; y < _cloudSpace._collisionMapHeight; y++)
                {
                    for (var x = 0; x < _cloudSpace._collisionMapWidth; x++)
                    {
                        if (_cloudSpace._collisionMap[x + y * _cloudSpace._collisionMapWidth])
                        {
                            c.DrawEllipse(Brushes.Purple, null, new Point(x, y), 1, 1);
                        }
                    }
                }
                foreach (var rect in _cloudSpace._collisionRects)
                {
                    c.DrawRectangle(null, new Pen(Brushes.Red, 1), rect);
                }
            }
            _mainDrawingGroup.Children.Add(dg);
        }

        public async Task AddWord(WordCloudData wordCloudData)
        {
            if (_currentWord == _words.Count)
            {
                PopulateWordList(wordCloudData);
            }

            if (_mainDrawingGroup.Children.Count == 0)
            {
                Setup();
            }

            await AddWordInternal();
        }

        public async Task AddWords(WordCloudData wordCloudData)
        {
            PopulateWordList(wordCloudData);
            if (_cloudSpace == null) Setup();
            await AddWordsInternal();
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


            return _cloudSpace.AddWordGeometry(wordGeo) ? wordGeo.GetDrawing() : null;
        }


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

    enum StartPosition
    {
        Center,
        Random,
    }
}