using System.Collections.Generic;
using System.Windows.Media;

namespace WordCloud.Structures
{
    internal class WordCloudThemes
    {
        static WordCloudThemes()
        {
            SetupWordCloudThemes();
        }

        private static void SetupWordCloudThemes()
        {
            var segoeUi = new Typeface("Segoe UI");

            Default = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Mixed, ColorPalette1, Brushes.Black);

            Horizontal1 = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Horizontal, ColorPalette1, Brushes.Black);

            Mixed1 = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Mixed, ColorPalette1, Brushes.Black);

            Random1 = new WordCloudTheme(segoeUi, WordCloudThemeWordRotation.Random, ColorPalette1, Brushes.Black);
        }

        public static WordCloudTheme Horizontal1 { get; private set; }

        public static WordCloudTheme Mixed1 { get; private set; }

        public static WordCloudTheme Random1 { get; private set; }

        public static WordCloudTheme Default { get; private set; }

        private static readonly List<SolidColorBrush> ColorPalette1 = new List<SolidColorBrush>
        {
           Brushes.BlueViolet,
           Brushes.LawnGreen,
           Brushes.Yellow,
           Brushes.CornflowerBlue
        };
    }
}