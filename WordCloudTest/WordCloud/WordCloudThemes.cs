using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WordCloudTest.WordClouds;

namespace WordCloudTest.WordCloud
{
    class WordCloudThemes
    {
        static WordCloudThemes()
        {
            SetupWordCloudThemes();
        }

        private static void SetupWordCloudThemes()
        {
            // Get named fonts, or DefaultFont on exception (font not found)
            Typeface georgia = null;
            Typeface impact = null;
            Typeface comicSans = null;
            Typeface segoeUi = null;

            try
            {
                georgia = new Typeface("Georgia");
            }
            catch
            {
                //georgia = DefaultFont;
            }

            try
            {
                impact = new Typeface("Impact");
            }
            catch
            {
                //impact = DefaultFont;
            }

            try
            {
                comicSans = new Typeface("Comic Sans MS");
            }
            catch
            {
                //comicSans = DefaultFont;
            }

            try
            {
                segoeUi = new Typeface("Segoe UI");
            }
            catch
            {
                //segoeUI = DefaultFont;

            }

            // Interpris
            Interpris = new WordCloudTheme(segoeUi,  WordCloudThemeWordRotation.Mixed, _colorPaletteInterpris, Brushes.White);

            // Horizontal
            Horizontal1 = new WordCloudTheme(impact,  WordCloudThemeWordRotation.HorizontalOnly, _colorPalette1, Brushes.Black);
  /*          Horizontal2 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.HorizontalOnly, _colorPalette2, Brushes.Black);
            Horizontal3 = new WordCloudTheme(comicSans, FontWeights.Normal, WordCloudThemeWordRotation.HorizontalOnly, _colorPalette3, Brushes.Black);
            Horizontal4 = new WordCloudTheme(georgia, FontWeights.Normal, WordCloudThemeWordRotation.HorizontalOnly, _colorPalette4, Brushes.Black);
            Horizontal5 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.HorizontalOnly, _colorPalette5, Brushes.White);
            Horizontal6 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.HorizontalOnly, _colorPalette6, Brushes.White);
            Horizontal7 = new WordCloudTheme(comicSans, FontWeights.Normal, WordCloudThemeWordRotation.HorizontalOnly, _colorPalette7, Brushes.White);
            Horizontal8 = new WordCloudTheme(georgia, FontWeights.Normal, WordCloudThemeWordRotation.HorizontalOnly, _colorPalette8, Brushes.White);
*/
            // Mixed (Horizontal & Vertical)
            Mixed1 = new WordCloudTheme(impact,WordCloudThemeWordRotation.Mixed, _colorPalette1, Brushes.Black);
/*
            Mixed2 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.Mixed, _colorPalette2, Brushes.Black);
            Mixed3 = new WordCloudTheme(comicSans, FontWeights.Normal, WordCloudThemeWordRotation.Mixed, _colorPalette3, Brushes.Black);
            Mixed4 = new WordCloudTheme(georgia, FontWeights.Normal, WordCloudThemeWordRotation.Mixed, _colorPalette4, Brushes.Black);
            Mixed5 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.Mixed, _colorPalette5, Brushes.White);
            Mixed6 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.Mixed, _colorPalette6, Brushes.White);
            Mixed7 = new WordCloudTheme(comicSans, FontWeights.Normal, WordCloudThemeWordRotation.Mixed, _colorPalette7, Brushes.White);
            Mixed8 = new WordCloudTheme(georgia, FontWeights.Normal, WordCloudThemeWordRotation.Mixed, _colorPalette8, Brushes.White);
*/

            // Random (Angled)
            Random1 = new WordCloudTheme(impact, WordCloudThemeWordRotation.Random, _colorPalette1, Brushes.Black);
/*            Random2 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.Random, _colorPalette2, Brushes.Black);
            Random3 = new WordCloudTheme(comicSans, FontWeights.Normal, WordCloudThemeWordRotation.Random, _colorPalette3, Brushes.Black);
            Random4 = new WordCloudTheme(georgia, FontWeights.Normal, WordCloudThemeWordRotation.Random, _colorPalette4, Brushes.Black);
            Random5 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.Random, _colorPalette5, Brushes.White);
            Random6 = new WordCloudTheme(impact, FontWeights.Normal, WordCloudThemeWordRotation.Random, _colorPalette6, Brushes.White);
            Random7 = new WordCloudTheme(comicSans, FontWeights.Normal, WordCloudThemeWordRotation.Random, _colorPalette7, Brushes.White);
            Random8 = new WordCloudTheme(georgia, FontWeights.Normal, WordCloudThemeWordRotation.Random, _colorPalette8, Brushes.White);*/
        }

        public static WordCloudTheme Horizontal1 { get; set; }
        public static WordCloudTheme Horizontal2 { get; set; }
        public static WordCloudTheme Horizontal3 { get; set; }
        public static WordCloudTheme Horizontal4 { get; set; }
        public static WordCloudTheme Horizontal5 { get; set; }
        public static WordCloudTheme Horizontal6 { get; set; }
        public static WordCloudTheme Horizontal7 { get; set; }
        public static WordCloudTheme Horizontal8 { get; set; }

        public static WordCloudTheme Mixed1 { get; set; }
        public static WordCloudTheme Mixed2 { get; set; }
        public static WordCloudTheme Mixed3 { get; set; }
        public static WordCloudTheme Mixed4 { get; set; }
        public static WordCloudTheme Mixed5 { get; set; }
        public static WordCloudTheme Mixed6 { get; set; }
        public static WordCloudTheme Mixed7 { get; set; }
        public static WordCloudTheme Mixed8 { get; set; }

        public static WordCloudTheme Random1 { get; set; }
        public static WordCloudTheme Random2 { get; set; }
        public static WordCloudTheme Random3 { get; set; }
        public static WordCloudTheme Random4 { get; set; }
        public static WordCloudTheme Random5 { get; set; }
        public static WordCloudTheme Random6 { get; set; }
        public static WordCloudTheme Random7 { get; set; }
        public static WordCloudTheme Random8 { get; set; }

        public static WordCloudTheme Interpris { get; set; }

        private static readonly List<SolidColorBrush> _colorPaletteInterpris = new List<SolidColorBrush>
        {
            new SolidColorBrush(Color.FromArgb(255,231, 0, 149)), // #E70095
                new SolidColorBrush(Color.FromArgb(255,83, 98, 112)), // #536270
                    new SolidColorBrush(Color.FromArgb(255,156, 203, 62)), // #9CCB3E
                        new SolidColorBrush(Color.FromArgb(255,61, 173, 180)) // #3DADB4
        };

        private static List<SolidColorBrush> _colorPalette1 = new List<SolidColorBrush>
        {
            new SolidColorBrush(Color.FromArgb(255,141, 195, 242)),
                new SolidColorBrush(Color.FromArgb(255,203, 228, 248)),
                    new SolidColorBrush(Color.FromArgb(255,140, 191, 31)),
                        new SolidColorBrush(Color.FromArgb(255,255, 255, 255))
        };
/*

        private static List<SolidColorBrush> _colorPalette2 = new List<SolidColorBrush>
        {
            Color.FromArgb(255,182, 79, 133),
            Color.FromArgb(255,218, 151, 186),
            Color.FromArgb(255,165, 207, 90)
        };

        private static List<Color> _colorPalette3 = new List<Color>
        {
            Color.FromArgb(255,70, 137, 102),
            Color.FromArgb(255,255, 176, 59),
            Color.FromArgb(255,182, 73, 38),
            Color.FromArgb(255,255, 244, 165)
        };

        private static List<Color> _colorPalette4 = new List<Color>
            {Colors.White};

        /* Color palette's 5-8 are for a white background
		 * 5 - RGB 99, 110, 0		RGB 247, 177, 0		RGB 107, 0, 25		RGB 235, 66, 0
		 * 6 - RGB 48, 84, 81		RGB 181, 154, 75	RGB 99, 43, 37		RGB 120, 76, 46
		 * 7 - RGB 172, 102, 1		RGB 57, 121, 183	RGB 9, 62, 112
		 * 8 - RGB 0, 0, 0
		 #1#

        private static List<Color> _colorPalette5 = new List<Color>
        {
            Color.FromArgb(255,172, 102, 1),
            Color.FromArgb(255,57, 121, 183),
            Color.FromArgb(255,9, 62, 112),
        };

        private static List<Color> _colorPalette6 = new List<Color>
        {
            Color.FromArgb(255,48, 84, 81),
            Color.FromArgb(255,181, 154, 75),
            Color.FromArgb(255,99, 43, 37),
            Color.FromArgb(255,120, 76, 46)
        };

        private static List<Color> _colorPalette7 = new List<Color>
        {
            Color.FromArgb(255,99, 110, 0),
            Color.FromArgb(255,247, 177, 0),
            Color.FromArgb(255,107, 0, 25),
            Color.FromArgb(255,235, 66, 0)
        };

        private static List<Color> _colorPalette8 = new List<Color>
            {Colors.Black};
*/

    }
}