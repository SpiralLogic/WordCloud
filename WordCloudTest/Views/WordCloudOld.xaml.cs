/*using WordCloudTest.WordClouds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WordCloudTest.Annotations;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Const = WordCloudTest.WordClouds.WordCloudConstants;
using FlowDirection = System.Windows.FlowDirection;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Drawing.Image;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

namespace WordCloudTest
{
    public partial class WordCloud :System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        #region fields

        /// <summary>
        /// Pixel - Entry mapping. This allows word selection, etc
        /// </summary>
        private Dictionary<Point, WordCloudEntry> _pixelToEntryMap = new Dictionary<Point, WordCloudEntry>();

        private readonly Dictionary<string, WordCloudTheme> _wordCloudThemeMap =
            new Dictionary<string, WordCloudTheme>();

        private BackgroundWorker _worker;
        private int _sourceBitmapHeight = -1;
        private int _sourceBitmapWidth = -1;
        private List<WordCloudEntry> _wordCloudEntries;
        private static readonly Random RandomGen = new Random();
        private readonly double _pixelsPerDip;

        public event EventHandler Refresh;


        #region Palette

        #endregion

        #endregion fields

        public WordCloud()
        {
            InitializeComponent();
            MyCurrentFontFamily = FontFamily;
            CurrentTheme = _wordCloudThemeMap["Interpris"];
            _pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            Refresh += (sender, args) => SetWords();
        }

        #region properties

        public List<WordCloudEntry> WordCloudEntries
        {
            get
            {
                if (_wordCloudEntries == null)
                {
                    _wordCloudEntries = new List<WordCloudEntry>(Const.MaxWords);
                }
                return _wordCloudEntries;
            }
            set { _wordCloudEntries = value.OrderByDescending(e => e.wordWeight).Take(Const.MaxWords).ToList(); }
        }

        public int BitmapHeight
        {
            get { return _sourceBitmapHeight; }
        }

        public int BitmapWidth
        {
            get { return _sourceBitmapWidth; }
        }

        public bool IsWorkerBusy { get; private set; }

        public WordCloudTheme CurrentTheme { get; set; }

        public FontFamily MyCurrentFontFamily { get; private set; }

        public Bitmap MySourceBitmap { get; private set; }

        public byte[] MySourcePixels { get; private set; }

        private WordCloudData ViewModel => DataContext as WordCloudData;

        #endregion properties

        #region Public Methods

        /// <summary>
        /// Gets the word cloud entry at point Performs a 2 pixel radius search
        /// </summary>
        /// <param name="aHitPoint">A hit point.</param>
        /// <returns>Word from WordCloud</returns>
        public WordCloudEntry GetWordCloudEntryAtPoint(Point aHitPoint)
        {
            Point centrePoint = aHitPoint;

            // If hit point maps to a word already, use that
            if (_pixelToEntryMap.ContainsKey(centrePoint))
            {
                return _pixelToEntryMap[aHitPoint];
            }

            // Do double for to check 1 pixel radius around centre point
            for (int y = -2; y < 3; y++)
            {
                for (int x = -2; x < 3; x++)
                {
                    Point radiusPoint = new Point(centrePoint.X + x, centrePoint.Y + y);

                    if (_pixelToEntryMap.ContainsKey(radiusPoint))
                    {
                        return _pixelToEntryMap[radiusPoint];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Renders to bitmap source.
        /// </summary>
        /// <returns></returns>
        public BitmapSource RenderToBitmapSource()
        {
            int height = (int) WordCloudImage.ActualHeight;
            int width = (int) WordCloudImage.ActualWidth;

            // Making a copy of wordCloud to avoid display/positioning issues
            System.Windows.Controls.Image wordCloudImageCopy = new System.Windows.Controls.Image
            {
                Source = WordCloudImage.Source
            };

            // Measure and Arrange to make sure the image is not clipped when the view is scrolled
            wordCloudImageCopy.Measure(new Size(width, height));
            wordCloudImageCopy.Arrange(new Rect(new System.Windows.Point(), new System.Windows.Point(width, height)));

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(wordCloudImageCopy);
            return rtb;
        }

        /// <summary>
        /// Cleanup on close as the form gets held onto after it close which means the collections don't get cleared.
        /// </summary>
        public void CleanupOnClose()
        {
            if (_pixelToEntryMap != null)
            {
                _pixelToEntryMap.Clear();
                _pixelToEntryMap = null;
            }

            if (_wordCloudEntries != null)
            {
                _wordCloudEntries.Clear();
                _wordCloudEntries = null;
            }

            if (MySourceBitmap != null)
            {
                MySourceBitmap.Dispose();
            }
        }

        #endregion Public Methods

        #region Private Methods

        #region Cloud Generation Methods

        /// <summary>
        /// Adjusts the size of the view.
        /// The Width to Hight ratio should be 4:3 for optimal results
        /// </summary>
        private void AdjustViewSize()
        {
            int height = 400;
            int width = (int) (height / 0.75f);

            // Blurry images are a known issue after scaling - this can be resolved in WPF4 using UseLayoutRendering=True
            WordCloudImage.SnapsToDevicePixels = true;
            WordCloudImage.Stretch = Stretch.None;
            WordCloudImage.MaxWidth = width;
            WordCloudImage.MaxHeight = height;
        }

        private void InternalRegenerateCloud()
        {
            if (IsWorkerBusy)
            {
                return;
            }

            IsWorkerBusy = true;

            // Workaround for issues with words overlapping, which only occurs if bitmap isn't square.
            AdjustViewSize();

            MySourceBitmap = new Bitmap((int) WordCloudImage.MaxWidth, (int) WordCloudImage.MaxHeight,
                PixelFormat.Format32bppPArgb);
            _pixelToEntryMap = new Dictionary<Point, WordCloudEntry>();

        }

        /// <summary>
        /// Handles the RunWorkerCompleted event of the myBackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _worker.DoWork -= worker_DoWork;
            _worker.RunWorkerCompleted -= worker_RunWorkerCompleted;

            WordCloudImage.Width = _sourceBitmapWidth = MySourceBitmap.Width;
            WordCloudImage.Height = _sourceBitmapHeight = MySourceBitmap.Height;

            RefreshImage();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            IsWorkerBusy = false;
        }

        /// <summary>
        /// Handles the DoWork event of the myBackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">Arguments</param>
        private void worker_DoWork(object sender, DoWorkEventArgs args)
        {
            if (WordCloudEntries.Count == 0) return;
            MySourcePixels = null;
            _pixelToEntryMap.Clear();

            // Get Bitmap's pixels array (as int[]) and Height and Width in Pixels
            var sourceBitmapPixels = GetSourceBitmapPixels();
            int sourceBitmapPixelHeight = MySourceBitmap.Height;
            int sourceBitmapPixelWidth = MySourceBitmap.Width;

            double minAlphaValue = WordCloudEntries.Min(e => e.AlphaValue);
            double maxAlphaValue = WordCloudEntries.Max(e => e.AlphaValue);
            double alphaValueRange = Math.Max(0, maxAlphaValue - minAlphaValue);

            double minSize = WordCloudEntries.Min(e => e.wordWeight);

            double maxSize = Math.Max(WordCloudEntries.Max(e => e.wordWeight), Const.MinimumLargestValue);
            double wordSizeRange = Math.Max(0.00001, maxSize - minSize);

            var areaPerLetter = GetAverageLetterPixelWidth() / wordSizeRange;

            double targetWidth = ((sourceBitmapPixelWidth + sourceBitmapPixelHeight) / Const.TargetWidthFactor) *
                                 Const.LargestSizeWidthProportion;

            WordCloudEntry largestWord = WordCloudEntries
                .OrderByDescending(e => (e.wordWeight - minSize) * e.Word.Length)
                .First();

            // Use minimum word length of MINIMUM_LARGEST_WORD_LENGTH to avoid overscalling
            int largestWordLength = Math.Max(largestWord.Word.Length, Const.MinimumLargestWordLength);

            double maxWordSize = 100 / (((largestWord.wordWeight - minSize) * largestWordLength * areaPerLetter) /
                                        targetWidth);

            // Reduce the maximum word size for random theme to avoid placement/collision issues due to high angle values
            if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
            {
                maxWordSize *= 0.8;
            }

            double maxFontSize = Math.Max(Const.MinFontSize * 2.7, maxWordSize);
            double fontMultiplier = Math.Min((maxFontSize - Const.MinFontSize) / wordSizeRange, 200);

            // These are the 5 centre points from where the program starts to place word(s) on main bitmap.
            // If the program is unable to find a placement for a word on any of these 5 points, it bails out and stops placing any more words.
            // This looks like an optimisation which hints the program about the last centre where there were no collisions found.
            var predefinedCentrePoints = new[]
            {
                new Point((sourceBitmapPixelWidth / 2), (sourceBitmapPixelHeight / 2)),
                new Point((sourceBitmapPixelWidth / 4), (sourceBitmapPixelHeight / 4)),
                new Point((sourceBitmapPixelWidth / 4), (3 * sourceBitmapPixelHeight / 2)),
                new Point((3 * sourceBitmapPixelWidth / 4), (sourceBitmapPixelHeight / 2)),
                new Point((3 * sourceBitmapPixelWidth / 4), (3 * sourceBitmapPixelHeight / 4))
            };

            int currentCentreIndex = 0;
            bool exitForEachLoop = false;

            // Loop through the top 100 words in WordCloudEntry collection, starting with most occuring words first
            foreach (WordCloudEntry cloudEntry in WordCloudEntries)
            {
                // Angle is as provided by entry
                int angle = cloudEntry.Angle;

                // Eord color is calculated from normalised alpha value and base color
                Color wordColor = GetWordColor((cloudEntry.AlphaValue - minAlphaValue) / alphaValueRange,
                    cloudEntry.Color);

                double fontSize = ((cloudEntry.wordWeight - minSize) * fontMultiplier) + Const.MinFontSize;
                fontSize /= _pixelsPerDip; //scale for high DPI

                // Create a bitmap for the word by giving its text, font size, color and angle
                // The word will have a different color if it was previously selected by user
                Bitmap wordBitmap = CreateImage(cloudEntry.Word, fontSize, angle, wordColor, CurrentTheme);

                // Get the pixels array for word's bitmap
                var wordBitmapPixels = GetWordBitmapPixels(wordBitmap);
                int wordBitmapWidth = wordBitmap.Width;
                int wordBitmapHeight = wordBitmap.Height;

                // Create a Collision list
                HashSet<Point> wordCollisionList = CreateCollisionList(wordBitmapHeight, wordBitmapWidth,
                    wordBitmapPixels, wordBitmap.PixelFormat);

                bool collided = true;
                double position = 0.0;
                Point centre = predefinedCentrePoints[currentCentreIndex];

                do
                {
                    Point spiralPoint = GetSpiralPoint(position);
                    int offsetX = (wordBitmapWidth / 2);
                    int offsetY = (wordBitmapHeight / 2);

                    var testPoint = new Point(spiralPoint.X + centre.X - offsetX, spiralPoint.Y + centre.Y - offsetY);

                    if (position > Const.MaxSprialLength)
                    {
                        // If position is outside the boundry then start again from a different (predefined) centre point
                        currentCentreIndex++;

                        if (currentCentreIndex >= predefinedCentrePoints.Length)
                        {
                            // The program has failed to place a word by starting at each of the 5 predefined centres.
                            // There is no room to place more words, Hence, exit the foreach loop and display the main bitmap with whatever words are there.
                            // This point is hit when the screen width and height is greatly disproportionate and an IndexOutOfRange exception is thrown.
                            exitForEachLoop = true;
                            break;
                        }

                        // The word couldn't be placed on the first of 5 predefined centres, start again from next centre point.
                        position = 0.0;
                        centre = predefinedCentrePoints[currentCentreIndex];
                    }
                    else
                    {
                        // Start from the first Test Point and count how many collisions are caused between source bitmap and word bitmap
                        // collision count of 0 means no overlaps/collision
                        // If collision count > 0, then we need to adjust the test point until we get no collision
                        int cols = CountAllCollisions(testPoint, wordCollisionList, sourceBitmapPixels,
                            sourceBitmapPixelWidth, sourceBitmapPixelHeight);

                        if (cols == 0)
                        {
                            // no collisions
                            int oldY;

                            do
                            {
                                oldY = testPoint.Y;

                                if (Math.Abs(testPoint.X + offsetX - centre.X) > 10)
                                {
                                    testPoint.X = AdjustXPoint(testPoint, offsetX, centre, wordCollisionList,
                                        sourceBitmapPixels, sourceBitmapPixelWidth, sourceBitmapPixelHeight);
                                }

                                if (Math.Abs(testPoint.Y + offsetY - centre.Y) > 10)
                                {
                                    testPoint.Y = AdjustYPoint(testPoint, offsetY, centre, wordCollisionList,
                                        sourceBitmapPixels, sourceBitmapPixelWidth, sourceBitmapPixelHeight);
                                }
                            } while (testPoint.Y != oldY);

                            // At the end of above loop, we have x,y points where there is no collision, so copy pixels from word bitmap to main bitmap and move to next word
                            collided = false;

                            // Copy the word bitmap onto main bitmap
                            CopyPixelsFromWordBitmapToSourceBitmap(MySourceBitmap, testPoint, wordBitmap);

                            // Update Pixel to entry map
                            UpdatePixelToEntryMap(wordCollisionList, testPoint, cloudEntry);

                            // The main bitmap has changed, reset its Pixels
                            sourceBitmapPixels = GetSourceBitmapPixels();
                        }
                        else
                        {
                            switch (cols)
                            {
                                case 5:
                                    position += Const.DoublePi / 20.0;
                                    break;

                                case 4:
                                    position += Const.DoublePi / 30.0;
                                    break;

                                case 3:
                                    position += Const.DoublePi / 40.0;
                                    break;

                                case 2:
                                    position += Const.DoublePi / 50.0;
                                    break;

                                default:
                                    position += Const.DoublePi / 100.0;
                                    break;
                            }
                        }
                    }
                } while (collided);

                if (exitForEachLoop)
                {
                    // Exit the foreach loop and diaply whatever words we managed to fit on bitmap
                    break;
                }
            }

            // Crops the top and bottom empty areas of the bitmap
            CropVerticalTop(sourceBitmapPixels);
            sourceBitmapPixels = GetSourceBitmapPixels();
            CropVerticalBottom(sourceBitmapPixels);
        }

        /// <summary>
        /// Get the average letter width in pixels, using the top 10 words.
        /// </summary>
        /// <returns>Average letter width in pixels</returns>
        private double GetAverageLetterPixelWidth()
        {
            double totalOfAverages = 0.0;

            // Average the letter width over the top 10 (or total count if less) words
            int wordCount = Math.Min(10, WordCloudEntries.Count);
            for (int i = 0; i < wordCount; i++)
            {
                string word = WordCloudEntries[i].Word;
                var txt = new FormattedText(word,
                    Thread.CurrentThread.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(MyCurrentFontFamily, FontStyles.Normal, FontWeights.Normal, new FontStretch()),
                    100,
                    Brushes.Black,
                    _pixelsPerDip);
                totalOfAverages += txt.Width / word.Length;
            }

            return (totalOfAverages / wordCount);
        }

        /// <summary>
        /// Get the color to write the word from it's base color, and alpha value
        /// </summary>
        /// <param name="anAlphaValue">Alpha value ratio (0 to 1)</param>
        /// <param name="aBaseColor">The base color for a word</param>
        /// <returns></returns>
        private Color GetWordColor(double anAlphaValue, Color aBaseColor)
        {
            int alpha = (byte) (Const.FromAlpha + (int) (anAlphaValue * (Const.ToAlpha - Const.FromAlpha)));
            return Color.FromArgb(alpha, aBaseColor.R, aBaseColor.G, aBaseColor.B);
        }

        /// <summary>
        /// Crops the top of source bitmap to get rid of any unnecessary white space
        /// </summary>
        /// <param name="aSourceBitmapPixels">A source bitmap pixels.</param>
        private void CropVerticalTop(byte[] aSourceBitmapPixels)
        {
            int sourceBitmapWidth = MySourceBitmap.Width;
            int sourceBitmapHeight = MySourceBitmap.Height;

            // stride = width * bits per pixel / bits per byte
            int stride = sourceBitmapWidth * (Image.GetPixelFormatSize(MySourceBitmap.PixelFormat) / 8);

            // Y offest with margin (to avoid cropping top to close to word entries)
            int yOffsetWithMargin = 0;

            bool breakLoop = false;

            // Goes through all individual bytes of the wordBitmapPixels array
            for (int y = 0; y < sourceBitmapHeight; y++)
            {
                for (int x = 0; x < stride; x++)
                {
                    if (aSourceBitmapPixels[(y * stride) + x] != 0)
                    {
                        yOffsetWithMargin = y - Const.VerticalCroppingMargin;
                        breakLoop = true;
                        break;
                    }
                }

                if (breakLoop)
                {
                    break;
                }
            }

            Bitmap blankBitmap = new Bitmap(sourceBitmapWidth, sourceBitmapHeight - yOffsetWithMargin);

            using (Graphics graphics = Graphics.FromImage(blankBitmap))
            {
                Rectangle rect = new Rectangle(0, -yOffsetWithMargin, sourceBitmapWidth, sourceBitmapHeight);

                graphics.DrawImage(MySourceBitmap, rect);

                MySourceBitmap = blankBitmap;
            }

            // Now update the myPixelToEntryMap to account for the cropping
            Dictionary<Point, WordCloudEntry> adjustedDictionary = new Dictionary<Point, WordCloudEntry>();

            foreach (KeyValuePair<Point, WordCloudEntry> pair in _pixelToEntryMap)
            {
                Point temp = pair.Key;
                adjustedDictionary.Add(new Point(temp.X, temp.Y - yOffsetWithMargin), pair.Value);
            }

            _pixelToEntryMap = adjustedDictionary;
        }

        /// <summary>
        /// Crops the bottom of source bitmap to get rid of any unnecessary white space
        /// </summary>
        /// <param name="aSourceBitmapPixels">A source bitmap pixels.</param>
        private void CropVerticalBottom(byte[] aSourceBitmapPixels)
        {
            int sourceBitmapWidth = MySourceBitmap.Width;
            int sourceBitmapHeight = MySourceBitmap.Height;

            // stride = width * bits per pixel / bits per byte
            int stride = sourceBitmapWidth * (Image.GetPixelFormatSize(MySourceBitmap.PixelFormat) / 8);
            int yOffset = 0;
            bool breakLoop = false;

            // Goes through all individual bytes of the wordBitmapPixels array
            for (int y = sourceBitmapHeight - 1; y > 0; y--)
            {
                for (int x = 0; x < stride; x++)
                {
                    if (aSourceBitmapPixels[(y * stride) + x] != 0)
                    {
                        yOffset = y + Const.VerticalCroppingMargin;
                        breakLoop = true;
                        break;
                    }
                }

                if (breakLoop)
                {
                    break;
                }
            }

            if (yOffset > 0)
            {
                Bitmap blankBitmap = new Bitmap(sourceBitmapWidth, yOffset);

                using (Graphics graphics = Graphics.FromImage(blankBitmap))
                {
                    Rectangle rect = new Rectangle(0, 0, sourceBitmapWidth, sourceBitmapHeight);

                    graphics.DrawImage(MySourceBitmap, rect);

                    MySourceBitmap = blankBitmap;
                }
            }
        }

        private void RefreshImage()
        {
            var bitmapWithBackground = new Bitmap(MySourceBitmap.Width, MySourceBitmap.Height);

            using (Graphics graphics = Graphics.FromImage(bitmapWithBackground))
            {
                var rect = new Rectangle(0, 0, MySourceBitmap.Width, MySourceBitmap.Height);
                var brush = new SolidBrush(CurrentTheme.BackgroundColor);

                graphics.FillRectangle(brush, rect);
                graphics.DrawImage(MySourceBitmap, new Point(0, 0));
            }

            // All the words have been correctly placed on main bitmap, refresh the bitmap
            ImageSource img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmapWithBackground.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(MySourceBitmap.Width, MySourceBitmap.Height));

            WordCloudImage.Source = img;
            WordCloudImage.InvalidateArrange();

            bitmapWithBackground.Dispose();
            MySourceBitmap.Dispose();
        }

        /// <summary>
        /// Adjust X coordinate point
        /// </summary>
        /// <param name="testPoint">Point to test collisions for</param>
        /// <param name="offsetX">Offset</param>
        /// <param name="centre">Centre point</param>
        /// <param name="collisionList">WordBitmap non-empty pixel list</param>
        /// <param name="sourceBitmapPixels">SourceBitmap pixels</param>
        /// <param name="sourceBitmapWidth">SourceBitmap width</param>
        /// <param name="sourceBitmapHeight">SourceBitmap height</param>
        /// <returns>
        /// Adjusted X point
        /// </returns>
        private int AdjustXPoint(Point testPoint, int offsetX, Point centre, HashSet<Point> collisionList,
            byte[] sourceBitmapPixels,
            int sourceBitmapWidth, int sourceBitmapHeight)
        {
            if (testPoint.X + offsetX < centre.X)
            {
                do
                {
                    // Increment the wordBitmapOrigin.X until there are no collisions
                    testPoint.X += 2;
                } while ((testPoint.X + offsetX < centre.X) &&
                         (CountCollisions(testPoint, collisionList, sourceBitmapPixels, sourceBitmapWidth,
                              sourceBitmapHeight) == 0));

                testPoint.X -= 2;
            }
            else
            {
                do
                {
                    // Decrease the wordBitmapOrigin.X until there are no collisions
                    testPoint.X -= 2;
                } while ((testPoint.X + offsetX > centre.X) &&
                         (CountCollisions(testPoint, collisionList, sourceBitmapPixels, sourceBitmapWidth,
                              sourceBitmapHeight) == 0));
                testPoint.X += 2;
            }

            return testPoint.X;
        }

        /// <summary>
        /// Adjust Y coordinate point
        /// </summary>
        /// <param name="testPoint">Point to test collisions for</param>
        /// <param name="offsetY">Offset</param>
        /// <param name="centre">Centre point</param>
        /// <param name="collisionList">WordBitmap non-empty pixel list</param>
        /// <param name="sourceBitmapPixels">SourceBitmap pixels</param>
        /// <param name="sourceBitmapWidth">SourceBitmap width</param>
        /// <param name="sourceBitmapHeight">SourceBitmap height</param>
        /// <returns>
        /// Adjusted Y point
        /// </returns>
        private int AdjustYPoint(Point testPoint, int offsetY, Point centre, HashSet<Point> collisionList,
            byte[] sourceBitmapPixels,
            int sourceBitmapWidth, int sourceBitmapHeight)
        {
            if (testPoint.Y + offsetY < centre.Y)
            {
                do
                {
                    // Increment the wordBitmapOrigin.Y until there are no collisions
                    testPoint.Y += 2;
                } while (testPoint.Y + offsetY < centre.Y && CountCollisions(testPoint, collisionList,
                             sourceBitmapPixels, sourceBitmapWidth, sourceBitmapHeight) == 0);

                testPoint.Y -= 2;
            }
            else
            {
                do
                {
                    // Decrement the wordBitmapOrigin.Y until there are no collisions
                    testPoint.Y -= 2;
                } while (testPoint.Y + offsetY > centre.Y && CountCollisions(testPoint, collisionList,
                             sourceBitmapPixels, sourceBitmapWidth, sourceBitmapHeight) == 0);

                testPoint.Y += 2;
            }

            return testPoint.Y;
        }

        /// <summary>
        /// Copies over the wordBitmap on top of _sourceBitmap
        /// </summary>
        /// <param name="aSourceBitmap">Souce bitmap</param>
        /// <param name="wordBitmapOrigin">Origin of wordBitmap</param>
        /// <param name="wordBitmap">Bitmap for this word</param>
        private void CopyPixelsFromWordBitmapToSourceBitmap(Bitmap aSourceBitmap, Point wordBitmapOrigin,
            Bitmap wordBitmap)
        {
            using (Graphics graphics = Graphics.FromImage(aSourceBitmap))
            {
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.DrawImage(wordBitmap, new Point(wordBitmapOrigin.X, wordBitmapOrigin.Y));
            }
        }

        /// <summary>
        /// Updates the PixelToEntryMap - notes that the 'collisions' for this WordCloundEntry belong to this word
        /// Used for selection
        /// </summary>
        /// <param name="wordCollisionList">The word collision list.</param>
        /// <param name="offsetPoint">The offset point.</param>
        /// <param name="cloudEntry">The cloud entry.</param>
        private void UpdatePixelToEntryMap(IEnumerable<Point> wordCollisionList, Point offsetPoint,
            WordCloudEntry cloudEntry)
        {
            foreach (var pixelPoint in wordCollisionList)
            {
                // Translate this point from local to global pixel coords
                var point = new Point(pixelPoint.X + offsetPoint.X, pixelPoint.Y + offsetPoint.Y);

                _pixelToEntryMap[point] = cloudEntry;
            }
        }

        /// <summary>
        /// Creates a bitmap out of text provided with transform specified
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="size">The size.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="color">The color.</param>
        /// <param name="aTheme">A theme.</param>
        /// <returns>
        /// Bitmap of text
        /// </returns>
        private Bitmap CreateImage(string text, double size, int angle, Color color, WordCloudTheme aTheme)
        {
            const int padding = 2;

            if (text == string.Empty)
            {
                return new Bitmap(0, 0, PixelFormat.Format32bppPArgb);
            }

            Font drawFont = new Font(aTheme.Font.Name, (float) size, aTheme.Font.Style);

            SizeF stringSize = TextRenderer.MeasureText(text, drawFont);
            SizeF stringSize2;

            Bitmap bitmap = new Bitmap(10, 10, PixelFormat.Format32bppPArgb);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                stringSize2 = graphics.MeasureString(text, drawFont);
            }

            if (stringSize2.Width > stringSize.Width)
            {
                stringSize = stringSize2;
            }

            if ((angle == 0) || (angle == 90) || (angle == -90))
            {
                // No rotation, or 90 degree rotation we can optimise the write
                bitmap = new Bitmap((int) stringSize.Width + 2 * padding, (int) stringSize.Height + 2 * padding, PixelFormat.Format32bppPArgb);

                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    SolidBrush brush = new SolidBrush(color);

                    graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                    graphics.DrawString(text, drawFont, brush, new PointF(padding, padding));

                    if (angle == 90)
                    {
                        bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    }
                    else if (angle == -90)
                    {
                        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    }
                }
            }
            else
            {
                // Create a square bitmap large enough to contain the (possibly) rotated word
                var bitmapSize = Math.Max((int) stringSize.Width, (int) stringSize.Height) + 2 * padding;
                bitmap = new Bitmap(bitmapSize, bitmapSize, PixelFormat.Format32bppPArgb);

                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // Offset the coordinate system so that point (0, 0) is at the center of the desired area
                    graphics.TranslateTransform((float) bitmapSize / 2, (float) bitmapSize / 2);

                    // Rotate the Graphics object.
                    graphics.RotateTransform(angle);

                    var brush = new SolidBrush(color);

                    graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

                    // Offset the Drawstring method so that the center of word aligns to the center of the bitmap (using the center origin set above)
                    graphics.DrawString(text, drawFont, brush, -stringSize.Width / 2, -stringSize.Height / 2);

                    // Reset the graphics object Transformations.
                    graphics.ResetTransform();
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Gets a hashset describing pixels that are not blank/empty for this bitmap.
        /// </summary>
        /// <param name="pixelHeight">Height of the pixel.</param>
        /// <param name="pixelWidth">Width of the pixel.</param>
        /// <param name="wordBitmapPixels">The word bitmap pixels.</param>
        /// <param name="aPixelFormat">A pixel format.</param>
        /// <returns></returns>
        private HashSet<Point> CreateCollisionList(int pixelHeight, int pixelWidth, byte[] wordBitmapPixels,
            PixelFormat aPixelFormat)
        {
            var nonEmptyPixels = new HashSet<Point>();

            // stride = width * bits per pixel / bits per byte
            int stride = pixelWidth * (Image.GetPixelFormatSize(aPixelFormat) / 8);

            // Goes through all individual bytes of the wordBitmapPixels array
            for (int y = 0; y < pixelHeight; y++)
            {
                for (int x = 0; x < stride; x++)
                {
                    // If this byte != 0, it means its part of a non-null pixel
                    if (wordBitmapPixels[(y * stride) + x] != 0)
                    {
                        var pixelPoint = new Point(x / 4, y);

                        if (!nonEmptyPixels.Contains(pixelPoint))
                        {
                            nonEmptyPixels.Add(pixelPoint);
                        }
                    }
                }
            }

            return nonEmptyPixels;
        }

        /// <summary>
        /// Master count collisions between the wordBitmap non empty pixels and sourceBitmap
        /// </summary>
        /// <param name="wordBitmapOrigin">Global offset for this word bitmap</param>
        /// <param name="nonEmptyWordPixelList">Hashet of collision points for this word bitmap</param>
        /// <param name="sourceBitmapPixels">Pixels as array of bytes of sourceBitmap</param>
        /// <param name="sourceBitmapWidth">WordBitmap width</param>
        /// <param name="sourceBitmapHeight">WordBitmap height</param>
        /// <returns>
        /// Number of collisions between this word non empty pixel array and sourceBitmap
        /// </returns>
        private int CountAllCollisions(Point wordBitmapOrigin, HashSet<Point> nonEmptyWordPixelList,
            byte[] sourceBitmapPixels, int sourceBitmapWidth, int sourceBitmapHeight)
        {
            int cols = CountCollisions(wordBitmapOrigin, nonEmptyWordPixelList, sourceBitmapPixels, sourceBitmapWidth,
                sourceBitmapHeight);

            cols += CountCollisions(new Point(wordBitmapOrigin.X - 2, wordBitmapOrigin.Y), nonEmptyWordPixelList,
                sourceBitmapPixels, sourceBitmapWidth, sourceBitmapHeight);
            cols += CountCollisions(new Point(wordBitmapOrigin.X + 2, wordBitmapOrigin.Y), nonEmptyWordPixelList,
                sourceBitmapPixels, sourceBitmapWidth, sourceBitmapHeight);
            cols += CountCollisions(new Point(wordBitmapOrigin.X, wordBitmapOrigin.Y - 2), nonEmptyWordPixelList,
                sourceBitmapPixels, sourceBitmapWidth, sourceBitmapHeight);
            cols += CountCollisions(new Point(wordBitmapOrigin.X, wordBitmapOrigin.Y + 2), nonEmptyWordPixelList,
                sourceBitmapPixels, sourceBitmapWidth, sourceBitmapHeight);

            return cols;
        }

        /// <summary>
        /// Count collisions between the wordBitmap non empty pixels and sourceBitmap
        /// </summary>
        /// <param name="wordBitmapOrigin">Global offset for this word bitmap</param>
        /// <param name="nonEmptyWordPixelList">Hashet of collision points for this word bitmap</param>
        /// <param name="sourceBitmapPixels">Pixels as array of bytes of sourceBitmap</param>
        /// <param name="sourceBitmapWidth">WordBitmap width</param>
        /// <param name="sourceBitmapHeight">WordBitmap height</param>
        /// <returns>
        /// Number of collisions between this word non empty pixel array and sourceBitmap
        /// </returns>
        private int CountCollisions(Point wordBitmapOrigin, IEnumerable<Point> nonEmptyWordPixelList,
            byte[] sourceBitmapPixels, int sourceBitmapWidth, int sourceBitmapHeight)
        {
            // Go through all the pixels for the wordBitmap and see if that location is occupied in the sourceBitmap
            foreach (var wordBitmapPoint in nonEmptyWordPixelList)
            {
                // wordBitmap pixel locations are local to its origin, make them global and look up in sourceBitmap
                int globalX = wordBitmapPoint.X + wordBitmapOrigin.X;
                int globalY = wordBitmapPoint.Y + wordBitmapOrigin.Y;

                if (globalX < 0 + Const.Margin || globalY < 0 + Const.Margin ||
                    globalX + Const.Margin >= sourceBitmapWidth || globalY + Const.Margin >= sourceBitmapHeight
                    || sourceBitmapPixels[(globalY * (sourceBitmapWidth * 4)) + (globalX * 4) + 3] != 0)
                {
                    return 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Gets an array of bytes, representing the pixels for the bitmap provided (number of bytes per pixel = PixelFormat / 8)
        /// </summary>
        /// <param name="aBitmapSource">Bitmap to get pixel array from</param>
        /// <returns>
        /// Byte array for this bitmap
        /// </returns>
        private byte[] GetWordBitmapPixels(Bitmap aBitmapSource)
        {
            int width = aBitmapSource.Width;
            int height = aBitmapSource.Height;

            PixelFormat pixelFormat = aBitmapSource.PixelFormat;
            Rectangle rect = new Rectangle(0, 0, width, height);

            // Stride = number of bytes per bitmap row
            int stride = width * (Image.GetPixelFormatSize(pixelFormat) / 8);

            // Create data array to hold bitmap pixel data
            var bitmapPixels = new byte[height * stride];

            BitmapData bitmapData = aBitmapSource.LockBits(rect, ImageLockMode.ReadWrite, pixelFormat);

            Marshal.Copy(bitmapData.Scan0, bitmapPixels, 0, height * stride);

            aBitmapSource.UnlockBits(bitmapData);

            return bitmapPixels;
        }

        /// <summary>
        /// Gets an array of bytes, representing the pixels for the source bitmap (number of bytes per pixel = PixelFormat / 8)
        /// </summary>
        /// <returns>
        /// Byte array for this bitmap
        /// </returns>
        private byte[] GetSourceBitmapPixels()
        {
            int width = MySourceBitmap.Width;
            int height = MySourceBitmap.Height;

            PixelFormat pixelFormat = MySourceBitmap.PixelFormat;
            Rectangle rect = new Rectangle(0, 0, width, height);

            // Stride = number of bytes per bitmap row
            int stride = width * (Image.GetPixelFormatSize(pixelFormat) / 8);

            // Create data array to hold bitmap pixel data
            if (MySourcePixels == null)
            {
                MySourcePixels = new byte[height * stride];
            }

            BitmapData bitmapData = MySourceBitmap.LockBits(rect, ImageLockMode.ReadWrite, pixelFormat);

            Marshal.Copy(bitmapData.Scan0, MySourcePixels, 0, height * stride);

            MySourceBitmap.UnlockBits(bitmapData);

            return MySourcePixels;
        }

        private Point GetSpiralPoint(double position)
        {
            double mult = position / Const.DoublePi * Const.SpiralRadius;
            double angle = position % Const.DoublePi;
            return new Point((int) (mult * Math.Sin(angle)), (int) (mult * Math.Cos(angle)));
        }

        #endregion Cloud Generation Methods

        private void SetWords()
        {
            var words = ViewModel.Words;
            var rowsCount = words.Rows.Count;
            if (rowsCount > Const.MaxTopwordsCount) rowsCount = Const.MaxTopwordsCount;

            var wordCloudEntries = new List<WordCloudEntry>(rowsCount);

            for (int i = 0; i < rowsCount; i++)
            {
                var row = words.Rows[i];

                var word = row.Item.Word;
                var wordSize = row.Count;

                var entry = PopulateWordCloudEntries(wordCloudEntries.Count == 0, RandomGen, word, wordSize);
                wordCloudEntries.Add(entry);
            }
            WordCloudEntries = wordCloudEntries;

            //Redraw
            InternalRegenerateCloud();
        }

        private WordCloudEntry PopulateWordCloudEntries(bool isFirstWord, Random aRandomGen, string word, int wordSize)
        {
            int colorIndex = 0;
            int angle = Const.NoRotation;

            // Theme's define a color list, randomly assign one by index
            if (CurrentTheme.ColorList.Count > 1)
            {
                // Color from theme list
                colorIndex = aRandomGen.Next(0, CurrentTheme.ColorList.Count);
            }

            if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Mixed)
            {
                // First word always horizontal
                // 70% Horizontal (default), 30% Vertical
                if (!isFirstWord && aRandomGen.Next(0, 10) >= 7)
                {
                    angle = Const.MixedRotationVertical;
                }
            }
            else if (CurrentTheme.WordRotation == WordCloudThemeWordRotation.Random)
            {
                // First word always horizontal
                if (!isFirstWord)
                {
                    // Random angle in range -RANDOM_MAX_ROTATION_ABS to RANDOM_MAX_ROTATION_ABS
                    angle = -Const.RandomMaxRotationAbs + aRandomGen.Next(0, Const.RandomRange);
                }
            }

            // At this stage, the word alpha value is set to be the same as the size value making the word color fade proportionaly with word size
            return new WordCloudEntry
            {
                Word = word,
                wordWeight = wordSize * Const.WeightedFrequencyMultiplier,
                Color = CurrentTheme.ColorList[colorIndex],
                AlphaValue = wordSize,
                Angle = angle
            };
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel?.Words != null)
            {
                WordCloudImage.Visibility = Visibility.Visible;
                SetWords();
            }
            else
            {
                WordCloudImage.Visibility = Visibility.Hidden;
            }
        }

        #endregion Private Methods

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

 public virtual void OnRefresh()
        {
            Refresh?.Invoke(this, EventArgs.Empty);
        }
    }
}*/