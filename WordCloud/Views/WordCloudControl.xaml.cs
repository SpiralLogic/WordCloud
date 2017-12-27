using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WordCloud.Structures;

// ReSharper disable AccessToDisposedClosure

namespace WordCloud.Views
{
    /// <summary>
    /// Interaction logic for WordCloudControl.xaml
    /// </summary>
    public partial class WordCloudControl
    {
        private const int MaxWords = 100;

        private int _currentWord;
        private double _fontMultiplier;

        private IReadOnlyList<WordCloudEntry> _words;
        private readonly DrawingGroup _wordDrawingGroup = new DrawingGroup();
        private readonly DrawingGroup _mainDrawingGroup = new DrawingGroup();
        private readonly DrawingGroup _bgDrawingGroup = new DrawingGroup();

        private CloudSpace _cloudSpace;
        private readonly IRandomizer _randomizer;
        private CancellationTokenSource _cts;
        private Task _cloudGenerationTask;
        private readonly SemaphoreSlim _cloudGenerationSemaphore = new SemaphoreSlim(1, 1);
        private DpiScale DpiScale => VisualTreeHelper.GetDpi(this);

        public WordCloudControl()
        {
            InitializeComponent();

            var di = new DrawingImage {Drawing = _mainDrawingGroup};

            BaseImage.Source = di;
            BaseImage.Stretch = Stretch.None;
            _randomizer = new CryptoRandomizer();
        }

        public WordCloudControl(IRandomizer randomizer) : this()
        {
            _randomizer = randomizer ?? new CryptoRandomizer();
        }

        public WordCloudTheme CurrentTheme { get; set; } = WordCloudThemes.Interpris;
        public int Failures => _cloudSpace.FailedPlacements;

        private void Setup()
        {
            _cloudSpace = new CloudSpace(Width - 4, Height - 4, _randomizer);

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

            var geo = CreateWordGeometry(word);

            if (geo != null)
            {
                _wordDrawingGroup.Children.Add(geo);
            }

            _currentWord++;

            _mainDrawingGroup.Children.Clear();
            _mainDrawingGroup.Children.Add(_wordDrawingGroup);

            //     RecenterFinishedWordGroup();
        }

        private async Task AddWordsInternal()
        {
            var previousCts = _cts;
            _cts = new CancellationTokenSource();

            if (previousCts != null)
            {
                // cancel the previous cloud generation and wait for its termination
                previousCts.Cancel();

                try
                {
                    await _cloudGenerationTask;
                }
                // we don't care about canceled cloud failures including other exceptions they cause
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }

            await _cloudGenerationSemaphore.WaitAsync(_cts.Token);
            var geoList = new BlockingCollection<GeometryDrawing>(_words.Count);
            try
            {
                var addTask = Task.Run(() =>
                {
                    foreach (var word in _words)
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var geo = CreateWordGeometry(word);

                        if (geo != null)
                        {
                            geoList.Add(geo);
                        }
                        _currentWord++;
                    }
                    geoList.CompleteAdding();
                }, _cts.Token);

                var displayTask = Task.Run(() =>
                {
                    try
                    {
                        while (!geoList.IsAddingCompleted || geoList.Count > 0)
                        {
                            _cts.Token.ThrowIfCancellationRequested();
                            var add = geoList.Take();
                            Dispatcher.InvokeAsync(() => { _wordDrawingGroup.Children.Add(add.Clone()); });
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }, _cts.Token);

                _cloudGenerationTask = Task.WhenAll(addTask, displayTask);
                await _cloudGenerationTask;

                //           _mainDrawingGroup.Children.Remove(_bgDrawingGroup);

                //    RecenterFinishedWordGroup();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                geoList.Dispose();
                _currentWord = 0;
                _cloudGenerationSemaphore.Release();
            }
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

        /*  private void AddCollisionDebug()
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
          }*/

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
            }
        }

        private GeometryDrawing CreateWordGeometry(WordCloudEntry word)
        {
            var text = new FormattedText(word.Word,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Typeface,
                GetFontSize(word),
                word.Color,
                DpiScale.PixelsPerDip);


            var textGeometry = text.BuildGeometry(new Point(0, 0));
            var wordGeo = new WordGeo(textGeometry, word);

            return _cloudSpace.AddWordGeometry(wordGeo) ? wordGeo.GetDrawing() : null;
        }

        private void PopulateWordList(WordCloudData wordCloudData)
        {
            var wordList = new List<WordCloudEntry>();
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
                    if (wordList.Any() && _randomizer.RandomInt(10) >= 7)
                    {
                        angle = WordCloudConstants.MixedRotationVertical;
                    }
                }
                else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
                {
                    // First word always horizontal
                    if (!wordList.Any())
                    {
                        angle = -WordCloudConstants.RandomMaxRotationAbs + _randomizer.RandomInt(WordCloudConstants.RandomRange);
                    }
                }

                // At this stage, the word alpha value is set to be the same as the size value making the word color fade proportionally with word size
                wordList.Add(new WordCloudEntry
                    {
                        Word = row.Item.Word,
                        wordWeight = row.Count,
                        Color = CurrentTheme.ColorList[colorIndex],
                        AlphaValue = row.Count,
                        Angle = angle
                    }
                );
            }
            _words = wordList.AsReadOnly();
        }

        private double GetFontMultiplier()
        {
            var averageLetterWidth = GetAverageLetterPixelWidth();
            var totalWeight = _words.Sum(w => w.wordWeight);
            var possibleRows = Math.Floor(_cloudSpace.Height / averageLetterWidth);
            var totalWidth = _words.Sum(w => w.Word.Length * averageLetterWidth * (w.wordWeight / (double) totalWeight));
            var sizePerRow = totalWidth / possibleRows;

            return _cloudSpace.Width / sizePerRow;
        }

        private int GetFontSize(WordCloudEntry word)
        {
            var maxFontSize = _cloudSpace.Width / _words.First().Word.Length;
            var totalWeight = _words.Sum(w => w.wordWeight);
            double relativeSize = word.wordWeight / (double) totalWeight;
            var fontSize = 100 * relativeSize;


            var normalizedFontSize = Math.Max(Math.Min(fontSize * _fontMultiplier , maxFontSize), WordCloudConstants.MinFontSize);
            return (int) normalizedFontSize;
        }

        private double GetAverageLetterPixelWidth()
        {
            var txt = new FormattedText("M",
                Thread.CurrentThread.CurrentCulture,
                FlowDirection.LeftToRight,
                CurrentTheme.Typeface,
                WordCloudConstants.WeightedFrequencyMultiplier,
                Brushes.Black,
                DpiScale.PixelsPerDip);

            return txt.BuildGeometry(new Point(0, 0)).Bounds.Width;
        }

        private void BaseImage_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = e.Source as Image;
            var position = e.GetPosition(this);
            Debug.WriteLine(position);
            position = new Point(position.X - 4, position.Y - 4);
            Debug.WriteLine(_wordDrawingGroup.Children.OfType<GeometryDrawing>().Sum(w=>w.Geometry.Bounds.Width));
            Debug.WriteLine(_wordDrawingGroup.Bounds.Width);
            foreach (var word in _wordDrawingGroup.Children.OfType<GeometryDrawing>().ToList())
            {
                if(word.Geometry.Bounds.Contains(position))
                using (var c = _wordDrawingGroup.Append())
                {
                    c.DrawRectangle(null, new Pen(Brushes.Red, 1), word.Geometry.Bounds);
                }
            }
            using (var c = _wordDrawingGroup.Append())
            {
                
                c.DrawEllipse(null, new Pen(Brushes.Purple, 1), position, 1, 1);
            }
        }
    }

    internal enum StartPosition
    {
        Center,
        Random,
    }
}