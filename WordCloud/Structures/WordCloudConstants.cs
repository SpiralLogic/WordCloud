using System;

namespace WordCloud.Structures
{
    public class WordCloudConstants
    {

        public const int MaxWords = 100;

        /// <summary>
        /// Minimum font size
        /// </summary>
        public const int MinFontSize = 11;

        /// <summary>
        /// Minimum largest value
        /// </summary>
        public const double MinimumLargestValue = 6;

        /// <summary>
        /// The ratio between the size of largest word and width of bitmap
        /// </summary>
        public const double LargestSizeWidthProportion = 0.38;

        /// <summary>
        /// Minimum length for font sizing calculations of the largest word
        /// </summary>
        public const int MinimumLargestWordLength = 4;

        /// <summary>
        /// Target width factor for adjusting bitmap size
        /// </summary>
        public const double TargetWidthFactor = 2.7;



        /// <summary>
        /// Non-rotated word angle
        /// </summary>
        public const int NoRotation = 0;

        /// <summary>
        /// Mixed rotation, rotated word angle
        /// </summary>
        public const int MixedRotationVertical = -90;

        /// <summary>
        /// Random rotation rotation (+ and - from 0)
        /// </summary>
        public const int RandomMaxRotationAbs = 80;

        /// <summary>
        /// The total range for random angles
        /// </summary>
        public const int RandomRange = RandomMaxRotationAbs * 2 + 1;

        /// <summary>
        /// Weighted frequency multiplier (WeightedFrequency for each word is multiplied by this value to ensure correct word sizing)
        /// </summary>
        public const int WeightedFrequencyMultiplier = 100;

    }
}