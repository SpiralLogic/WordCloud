using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WordCloudTest
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
