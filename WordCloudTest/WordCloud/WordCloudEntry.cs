
using System.Windows.Media;

namespace WordCloudTest.WordClouds
{
    public class WordCloudEntry
    {
        /// <summary>
        /// Gets or sets the angle.
        /// </summary>
        /// <value>
        /// The angle.
        /// </value>
        public int Angle { get; set; }

        /// <summary>
        /// Gets or sets the base color for the word.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets the alpha value
        /// Will be normalised to 0 to 1 range to set the color's alpha
        /// </summary>
        /// <value>
        /// The color value.
        /// </value>
        public double AlphaValue { get; set; }

        /// <summary>
        /// Gets or sets the size value.
        /// </summary>
        /// <value>
        /// The size value.
        /// </value>
        public int SizeValue { get; set; }

        /// <summary>
        /// Gets or sets the word.
        /// </summary>
        /// <value>
        /// The word.
        /// </value>
        public string Word { get; set; }
    }
}