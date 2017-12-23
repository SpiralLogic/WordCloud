using System.Collections.Generic;
using System.Windows.Media;

namespace WordCloudTest.WordClouds
{
    public class WordCloudTheme
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WordCloudTheme"/> class.
        /// </summary>
        public WordCloudTheme(Typeface typeFace,  WordCloudThemeWordRotation wordRotation, List<SolidColorBrush> colorList, SolidColorBrush backgroundColor)
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
        public List<SolidColorBrush> ColorList
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
        public SolidColorBrush BackgroundColor
        {
            get;
            set;
        }
    }
}