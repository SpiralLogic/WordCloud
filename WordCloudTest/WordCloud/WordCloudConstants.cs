using System;

namespace WordCloudTest.WordClouds
{
    public class WordCloudConstants
    {
        // Minimum alpha
        public const byte FromAlpha = 192;

        // Maximum alpha
        public const byte ToAlpha = 255;

        /// <summary>
        /// Multiplier for spiral calculations
        /// </summary>
        public const int SpiralRadius = 7;

        /// <summary>
        /// Double Pi
        /// </summary>
        public const double DoublePi = 2.0 * Math.PI;

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
        /// Source bitmap margin to avoid clipping
        /// </summary>
        public const int Margin = 20;

        /// <summary>
        /// Margin for vertical cropping - increase this to increase the amount of top & bottom whitespace
        /// around word cloud
        /// </summary>
        public const int VerticalCroppingMargin = 10;

        /// <summary>
        /// Target width factor for adjusting bitmap size
        /// </summary>
        public const double TargetWidthFactor = 2.7;

        /// <summary>
        /// Length to travel along the spiral before ending search
        /// </summary>
        public const double MaxSprialLength = DoublePi * 5800;

        /// <summary>
        /// The fixed limit of top words for WordCloud
        /// </summary>
        public const int MaxTopwordsCount = 100;

        /// <summary>
        /// The offset start location for print preview
        /// </summary>
        public const int LocationOffset = 30;

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

        /// <summary>
        /// The margin to be used for exporting the Tree Map to PDF
        /// </summary>
        public const int PdfExportMargin = 40;
    }
}