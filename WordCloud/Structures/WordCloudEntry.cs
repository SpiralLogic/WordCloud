using System.Windows.Media;

namespace WordCloud.Structures
{
    public class WordCloudEntry
    {
        public int Angle { get; set; }

        public SolidColorBrush Brush { get; set; }

        public int Weight { get; set; }

        public string Word { get; set; }
    }
}