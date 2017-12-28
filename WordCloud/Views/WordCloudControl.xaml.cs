using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
        private const int MaxWords = 600;
        private const double WorkingAreaBuffer = 4.0D;
        public int CurrentWord;
        private double _fontMultiplier;

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
        private readonly int _wordAnimationThreshold = 200;
        private double _recenterWordDrawingGroupThreshold = 0.90;
        private int _recenterScaleTransformDuration = 500;
        private int _recenterTranslateTransformDuration = 300;
        private int _rangeRotation = 160;
        private int _maxRotation = 80;
        private int _totalWordWeights;
        private int _largestWordLength;
        private int _standardFontSize = 100;
        private int _minFontSize = 10;
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);

        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Default;
        public int Failures => _cloudSpace.FailedPlacements;

        public WordCloudControl()
        {
            InitializeComponent();

            BaseImage.Source = new DrawingImage { Drawing = _mainDrawingGroup };
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

            _fontMultiplier = GetFontMultiplier();

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
                    var displayTask = Task.Run(() => { WordGeometryConsumer(geometryDrawings); }, _cts.Token);
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
            RecenterFinishedWordGroup();
        }

        private void WordGeometryConsumer(BlockingCollection<GeometryDrawing> geometryDrawings)
        {
            try
            {
                while (!geometryDrawings.IsAddingCompleted || geometryDrawings.Count > 0)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var geometryDrawing = geometryDrawings.Take();
                    Dispatcher.InvokeAsync(() =>
                    {
                        var wordFadeAnimation = new DoubleAnimation
                        {
                            From = 0.0,
                            To = 1.0,
                            Duration = _wordFadeInDuration,
                            AccelerationRatio = 0.2
                        };

                        using (var c = _wordDrawingGroup.Append())
                        {
                            if (_words.Count < _wordAnimationThreshold)
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

        private void WordGeometryProducer(BlockingCollection<GeometryDrawing> geometryDrawings)
        {
            foreach (var word in _words)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var geometryDrawing = CreateWordGeometryDrawing(word);

                if (geometryDrawing != null)
                {
                    geometryDrawings.Add(geometryDrawing);
                }
                CurrentWord++;
            }

            geometryDrawings.CompleteAdding();
        }

        private void RecenterFinishedWordGroup()
        {
            _finalTransformGroup.Children.Clear();
            var wordGroupBounds = _wordDrawingGroup.Bounds;

            _recenterWordDrawingGroupThreshold = 0.90;
            if (wordGroupBounds.Width < _cloudSpace.Width * _recenterWordDrawingGroupThreshold || wordGroupBounds.Height < _cloudSpace.Height * _recenterWordDrawingGroupThreshold)
            {
                var scalePercent = Math.Min(_cloudSpace.Height / wordGroupBounds.Height, _cloudSpace.Width / wordGroupBounds.Width);
                scalePercent += scalePercent > 1 ? -0.075 : 0.075;
                var scaleAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = scalePercent,
                    Duration = new Duration(TimeSpan.FromMilliseconds(_recenterScaleTransformDuration)),
                    AccelerationRatio = 0.2
                };
                var scaleTransform = new ScaleTransform(scalePercent, scalePercent, wordGroupBounds.Width / 2, wordGroupBounds.Height / 2);
                var scaledBounds = scaleTransform.TransformBounds(wordGroupBounds);

                var translateX = (_cloudSpace.Width - scaledBounds.Width) / 2 - scaledBounds.X;
                var translateY = (_cloudSpace.Height - scaledBounds.Height) / 2 - scaledBounds.Y;

                var translateTransform = new TranslateTransform(translateX, translateY);

                var translateTransformX = new DoubleAnimation
                {
                    From = 0,
                    To = translateX,
                    Duration = new Duration(TimeSpan.FromMilliseconds(_recenterTranslateTransformDuration)),
                    AccelerationRatio = 0.2
                };

                var translateTransformY = new DoubleAnimation
                {
                    From = 0,
                    To = translateY,
                    Duration = new Duration(TimeSpan.FromMilliseconds(_recenterTranslateTransformDuration)),
                    AccelerationRatio = 0.2
                };

                _finalTransformGroup.Children.Add(translateTransform);
                _finalTransformGroup.Children.Add(scaleTransform);

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                translateTransform.BeginAnimation(TranslateTransform.XProperty, translateTransformX);
                translateTransform.BeginAnimation(TranslateTransform.YProperty, translateTransformY);
            }

            using (var context = _bgDrawingGroup.Open())
            {
                context.DrawRectangle(CurrentTheme.BackgroundBrush, null, new Rect(0, 0, Width - WorkingAreaBuffer, Height - WorkingAreaBuffer));
            }

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

        private GeometryDrawing CreateWordGeometryDrawing(WordCloudEntry word)
        {
            var text = new FormattedText(word.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Typeface,
                GetFontSize(word),
                word.Brush,
                DpiScale.PixelsPerDip);

            var textGeometry = text.BuildGeometry(new Point(0, 0));
            var wordDrawing = new WordDrawing(textGeometry, word);

            if (!_cloudSpace.AddWordGeometry(wordDrawing)) return null;

            _addedWordDrawings.Add(wordDrawing);
            return wordDrawing.GetDrawing();
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
                    if (wordList.Any() && _randomizer.RandomInt(10) >= 7)
                    {
                        angle = -90;
                    }
                }
                else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
                {
                    if (wordList.Any())
                    {
                        angle = -_maxRotation + _randomizer.RandomInt(_rangeRotation);
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

        private double GetFontMultiplier()
        {
            _totalWordWeights = _words.Sum(w => w.Weight);
            _largestWordLength = _words.OrderByDescending(w => w.Weight).First().Word.Length;
            var averageLetterWidth = GetAverageLetterWidth();
            var possibleRows = Math.Floor(_cloudSpace.Height / averageLetterWidth);
            var totalWidth = _words.Sum(w => w.Word.Length * averageLetterWidth * (w.Weight / (double)_totalWordWeights));
            var sizePerRow = totalWidth / possibleRows;

            return _cloudSpace.Width / sizePerRow;
        }

        private int GetFontSize(WordCloudEntry word)
        {
            var maxFontSize = _cloudSpace.Width / _largestWordLength;
            var relativeSize = word.Weight / (double)_totalWordWeights;
            var fontSize = _standardFontSize * relativeSize;

            var scaledFontSize = Math.Max(Math.Min(fontSize * _fontMultiplier * _words.Count / 100, maxFontSize), _minFontSize);
            return (int)scaledFontSize;
        }

        private double GetAverageLetterWidth()
        {
            var formattedText = new FormattedText("M",
                Thread.CurrentThread.CurrentCulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Typeface,
                _standardFontSize,
                Brushes.Black,
                DpiScale.PixelsPerDip);

            return formattedText.BuildGeometry(new Point(0, 0)).Bounds.Width;
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