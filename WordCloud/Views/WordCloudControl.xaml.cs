using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WordCloud.Structures;

// ReSharper disable AccessToDisposedClosure

namespace WordCloud.Views
{
    /// <summary>
    /// Interaction logic for WordCloudControl.xaml
    /// </summary>
    public sealed partial class WordCloudControl : IDisposable
    {
        private const int MaxWords = 200;
        private const double WorkingAreaBuffer = 4.0D;
        private const int WordAnimationThreshold = 200;
        private const int RangeRotation = 160;
        private const int MaxRotation = 80;

        public int CurrentWord;

        private IReadOnlyList<WordCloudEntry> _words;
        private readonly DrawingGroup _wordDrawingGroup = new DrawingGroup();
        private readonly IList<WordDrawing> _addedWordDrawings = new List<WordDrawing>();
        private readonly DrawingGroup _mainDrawingGroup = new DrawingGroup();
        private readonly DrawingGroup _bgDrawingGroup = new DrawingGroup();
        private readonly TransformGroup _finalTransformGroup = new TransformGroup();
        private readonly SemaphoreSlim _cloudGenerationSemaphore = new SemaphoreSlim(1, 1);
        private readonly IRandomizer _randomizer;

        private CloudSpace _cloudSpace;
        private CancellationTokenSource _cts;
        private Task _cloudGenerationTask;
        private readonly Duration _wordFadeInDuration = new Duration(TimeSpan.FromMilliseconds(200));

        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);
        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Default;

        public int Failures => _cloudSpace.FailedPlacements;

        public WordCloudControl()
        {
            InitializeComponent();

            BaseImage.Source = new DrawingImage {Drawing = _mainDrawingGroup};
            BaseImage.Stretch = Stretch.None;
            _randomizer = new CryptoRandomizer();
        }

        public WordCloudControl(IRandomizer randomizer) : this()
        {
            _randomizer = randomizer ?? new CryptoRandomizer();
        }

        private void Setup()
        {
            _cloudSpace = new CloudSpace(Width - WorkingAreaBuffer, Height - WorkingAreaBuffer, _randomizer);
            MaxWidth = Width;
            MaxHeight = Height;

            _mainDrawingGroup.Children.Clear();
            _bgDrawingGroup.Children.Clear();
            _finalTransformGroup.Children.Clear();

            using (var context = _bgDrawingGroup.Open())
            {
                context.DrawRectangle(CurrentTheme.BackgroundBrush, null, new Rect(0, 0, Width - WorkingAreaBuffer, Height - WorkingAreaBuffer));
            }

            _mainDrawingGroup.Children.Add(_bgDrawingGroup);
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);
            _wordDrawingGroup.Transform = _finalTransformGroup;
        }

        private async Task AddWordsInternal()
        {
            _finalTransformGroup.Children.Clear();
            var previousCts = _cts;
            _cts = new CancellationTokenSource();

            if (previousCts != null)
            {
                previousCts.Cancel();

                try
                {
                    await _cloudGenerationTask;
                }
                catch
                {
                    // don't worry about canceled cloud exceptions
                }
            }

            await _cloudGenerationSemaphore.WaitAsync(_cts.Token);
            using (var geometryDrawings = new BlockingCollection<GeometryDrawing>(_words.Count))
            {
                try
                {
                    var addTask = Task.Run(() => { WordGeometryProducer(geometryDrawings); }, _cts.Token);
                    var displayTask = Task.Run(() =>
                    {
                        try
                        {
                            if (_words.Count < WordAnimationThreshold)
                            {
                                WordGeometryConsumerAnimated(geometryDrawings);
                            }
                            else
                            {
                                WordGeometryConsumer(geometryDrawings);
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }, _cts.Token);
                    _cloudGenerationTask = Task.WhenAll(addTask, displayTask);
                    await _cloudGenerationTask;
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    CurrentWord = 0;
                    _cloudGenerationSemaphore.Release();
                }
            }
        }

        private void WordGeometryConsumerAnimated(BlockingCollection<GeometryDrawing> geometryDrawings)
        {
            try
            {
                while (!geometryDrawings.IsAddingCompleted || geometryDrawings.Count > 0)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var geometryDrawing = geometryDrawings.Take();

                    Dispatcher.InvokeAsync(() =>
                    {
                        using (var c = _wordDrawingGroup.Append())
                        {
                            var wordFadeAnimation = new DoubleAnimation
                            {
                                From = 0.0,
                                To = 1.0,
                                Duration = _wordFadeInDuration,
                                AccelerationRatio = 0.2
                            };

                            if (_words.Count < WordAnimationThreshold)
                            {
                                geometryDrawing = geometryDrawing.Clone(); // Need unfrozen for animations
                                geometryDrawing.Brush.BeginAnimation(Brush.OpacityProperty, wordFadeAnimation);
                            }

                            c.DrawDrawing(geometryDrawing);
                        }
                    });
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void WordGeometryConsumer(BlockingCollection<GeometryDrawing> geometryDrawings)
        {
            while (!geometryDrawings.IsAddingCompleted || geometryDrawings.Count > 0)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var geometryDrawing = geometryDrawings.Take();
                Dispatcher.InvokeAsync(() => { _wordDrawingGroup.Children.Add(geometryDrawing); });
            }
        }

        private void WordGeometryProducer(BlockingCollection<GeometryDrawing> geometryDrawings)
        {
            var wordList = _words.Select(CreateWordGeometryDrawing).ToList();

            CalculateAndPerformScaling(wordList);

            foreach (var wordDrawing in wordList)
            {
                _cts.Token.ThrowIfCancellationRequested();

                if (AddDrawingToCloudSpace(wordDrawing))
                    geometryDrawings.Add(wordDrawing.GetDrawing());

                CurrentWord++;
            }

            geometryDrawings.CompleteAdding();
        }

        private void CalculateAndPerformScaling(ICollection<WordDrawing> wordList)
        {
            double maxWeight = wordList.First().Weight;
            var scaleArea = new Size(_cloudSpace.Width - 10, _cloudSpace.Height - 10);
            foreach (var wordDrawing in wordList)
            {
                wordDrawing.ApplyScale(wordDrawing.Weight / maxWeight);
            }

            var longestDrawing = wordList.Max(w => w.Width);
            var tallestDrawing = wordList.Max(w => w.Height);

            var requiredSize = DetermineRequiredArea(wordList);
            var scale = Math.Max((scaleArea.Width - 100) / requiredSize.Width, (scaleArea.Width - 100) / requiredSize.Height);

            if (scale * longestDrawing > scaleArea.Width) scale = (scaleArea.Width) / longestDrawing;
            if (scale * tallestDrawing > scaleArea.Height) scale = (scaleArea.Height) / tallestDrawing;

            foreach (var wordDrawing in wordList)
            {
                wordDrawing.ApplyScale(scale);
            }
        }

        private Size DetermineRequiredArea(ICollection<WordDrawing> words)
        {
            var length = Math.Max(words.First().Width, words.First().Height) + 10;
            var currentWidth = length;
            var currentHeight = length;
            var currentIndex = 0;
            var levels = new List<Point> {new Point(0, 0)};
            var maxWidth = 0D;
            var maxHeight = 0D;
            foreach (var word in words.OrderByDescending(w1 => w1.Height * w1.Width))
            {
                Point currentLevel;
                while (true)
                {
                    currentLevel = levels[currentIndex];

                    if (currentLevel.X + word.Width < currentWidth && currentLevel.Y + word.Height < currentHeight)
                    {
                        var y = currentIndex == 0 ? currentLevel.Y : levels[currentIndex - 1].Y + word.Height;
                        if (currentIndex == levels.Count - 1)
                        {
                            currentLevel.Y = Math.Max(y, word.Height);
                            currentLevel.X += word.Width;
                            levels[currentIndex] = currentLevel;
                            break;
                        }

                        if (currentLevel.Y + word.Height < levels[currentIndex + 1].Y)
                        {
                            currentLevel.X += word.Width;
                            levels[currentIndex] = currentLevel;

                            break;
                        }
                    }

                    var isLastLevel = currentIndex == levels.Count - 1;
                    if (isLastLevel && currentLevel.Y + word.Height < currentHeight && word.Width < currentWidth)
                    {
                        currentIndex++;
                        levels.Add(new Point(0, currentLevel.Y));
                        continue;
                    }

                    if (!isLastLevel && currentLevel.Y + word.Height < currentHeight)
                    {
                        currentIndex++;
                        continue;
                    }

                    var adjust = word.Width;
                    if (word.Width > word.Height)
                        adjust = word.Height;

                    currentHeight += adjust + 10;
                    currentWidth += adjust + 10;
                    currentIndex = 0;
                }

                if (currentLevel.X > maxWidth) maxWidth = currentLevel.X;
                if (currentLevel.Y > maxHeight) maxHeight = currentLevel.Y;
            }

            return new Size(maxWidth, maxHeight);
        }

        public async Task AddWords(WordCloudData wordCloudData)
        {
            PopulateWordList(wordCloudData);
            if (_cloudSpace == null) Setup();
            try
            {
                await AddWordsInternal();
            }
            catch (OperationCanceledException)
            {
                // If semaphore is canceled catch exception
            }
        }

        private WordDrawing CreateWordGeometryDrawing(WordCloudEntry word)
        {
            var wordDrawing = new WordDrawing(word, CurrentTheme, DpiScale);

            return wordDrawing;
        }

        private bool AddDrawingToCloudSpace(WordDrawing wordDrawing)
        {
            if (!_cloudSpace.AddWordGeometry(wordDrawing)) return false;

            _addedWordDrawings.Add(wordDrawing);

            return true;
        }

        private void PopulateWordList(WordCloudData wordCloudData)
        {
            var wordList = new List<WordCloudEntry>();
            foreach (var row in wordCloudData.Words.Rows.Take(MaxWords))
            {
                var brushIndex = 0;
                var angle = 0;

                if (CurrentTheme.BrushList.Count > 1)
                {
                    brushIndex = _randomizer.RandomInt(CurrentTheme.BrushList.Count);
                }

                if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Mixed)
                {
                    if (wordList.Any() && _randomizer.RandomInt(10) >= 70)
                    {
                        angle = -90;
                    }
                }
                else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
                {
                    if (wordList.Any())
                    {
                        angle = -MaxRotation + _randomizer.RandomInt(RangeRotation);
                    }
                }

                // At this stage, the word alpha value is set to be the same as the size value making the word color fade proportionally with word size
                wordList.Add(new WordCloudEntry
                    {
                        Word = row.Item.Word,
                        Weight = row.Count,
                        Brush = CurrentTheme.BrushList[brushIndex],
                        Angle = angle
                    }
                );
            }

            _words = wordList.AsReadOnly();
        }

        private void BaseImage_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_wordDrawingGroup.Transform.Inverse == null) return;

            var position = _wordDrawingGroup.Transform.Inverse.Transform(e.GetPosition(this));
            position = new Point(position.X - WorkingAreaBuffer, position.Y - WorkingAreaBuffer);

            var word = LocateClickedDrawing(position);
            if (word == null) return;

            // TODO: make custom actions possible
            using (var c = _wordDrawingGroup.Append())
            {
                c.DrawRectangle(null, new Pen(Brushes.Red, 1), word.GetBounds());
            }
        }

        private WordDrawing LocateClickedDrawing(Point position)
        {
            return _addedWordDrawings.Reverse().FirstOrDefault(word => word.Contains(position.X, position.Y));
        }

        public void Dispose()
        {
            _cloudGenerationSemaphore?.Dispose();
            _cts?.Dispose();
            _cloudGenerationTask?.Dispose();
        }
    }
}