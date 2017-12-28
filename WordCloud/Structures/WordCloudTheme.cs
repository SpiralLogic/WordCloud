using System.Collections.Generic;
using System.Windows.Media;

namespace WordCloud.Structures
{
    public class WordCloudTheme
    {
        public WordCloudTheme(Typeface typeFace,  WordCloudThemeWordRotation wordRotation, IList<SolidColorBrush> brushList, SolidColorBrush backgroundBrush)
        {
            Typeface = typeFace;
            WordRotation = wordRotation;
            BrushList = brushList;
            BackgroundBrush = backgroundBrush;

            foreach (var color in BrushList)
            {
                color.Freeze();
            }
        }

        public Typeface Typeface
        {
            get;
        }

        public WordCloudThemeWordRotation WordRotation
        {
            get;
        }
        
        public IList<SolidColorBrush> BrushList
        {
            get;
        }

        public SolidColorBrush BackgroundBrush
        {
            get;
        }
    }
}