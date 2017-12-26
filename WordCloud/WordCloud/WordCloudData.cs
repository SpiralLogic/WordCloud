using System.ComponentModel;
using System.Runtime.CompilerServices;
using WordCloud.WordFrequencyQuery;

namespace WordCloud.WordCloud
{
    public class WordCloudData : INotifyPropertyChanged
    {
        public FrequencyTable<WordGroup> Words;

        public WordCloudData(FrequencyTable<WordGroup> words)
        {
            Words = words;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
