using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace WordCloudTest.WordClouds
{
    public class WordCloudTheme
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WordCloudTheme"/> class.
        /// </summary>
        public WordCloudTheme(Typeface typeFace, FontWeight fontWeight, WordCloudThemeWordRotation wordRotation, List<Color> colorList, Color backgroundColor)
        {
            Typeface = typeFace;
            WordRotation = wordRotation;
            ColorList = colorList;
            BackgroundColor = backgroundColor;
        }

        /// <summary>
        /// Gets or sets the font.
        /// </summary>
        /// <value>
        /// The font.
        /// </value>
        public Typeface Typeface
        {
            get;
            set;
        }

       public FontWeight FontWeight { get; set; }

        /// <summary>
        /// Gets or sets the word rotation.
        /// </summary>
        /// <value>
        /// The text alignment.
        /// </value>
        public WordCloudThemeWordRotation WordRotation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the color list.
        /// </summary>
        /// <value>
        /// The color list.
        /// </value>
        public List<Color> ColorList
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the color of the background.
        /// </summary>
        /// <value>
        /// The color of the background.
        /// </value>
        public Color BackgroundColor
        {
            get;
            set;
        }
    }
}