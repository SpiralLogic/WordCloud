using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WordCloudTest.Annotations;

namespace WordCloudTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();


            var wordlist = new List<FrequencyTableRow<WordGroup>>();
            var count = 0;
            foreach (var kvp in _words)
            {
                wordlist.Add(new FrequencyTableRow<WordGroup>(new WordGroup(kvp.Key), kvp.Value));
                count += kvp.Value;
            }


            Words = new FrequencyTable<WordGroup>(wordlist, count);

            _wordcloudControlDataContext = new WordCloudData(Words);

            WordcloudControl.DataContext = _wordcloudControlDataContext;
        }

        public FrequencyTable<WordGroup> Words;

        private void DoWordCloud(object sender, RoutedEventArgs e)
        {
            WordcloudControl.DoStuff();
        }

        public WordCloudData WordCloudData
        {
            get { return _wordCloudData; }
            set
            {
                _wordCloudData = value;
                OnPropertyChanged(nameof(WordCloudData));
            }
        }

        private readonly Dictionary<string, int> _words = new Dictionary<string, int>
        {
            {"coding", 58},
            {"windows", 52},
            {"nvivo", 48},
            {"mac", 42},
            {"use", 41},
            {"features", 39},
            {"analysis", 31},
            {"version", 31},
            {"data", 30},
            {"nodes", 29},
            {"better", 25},
            {"word", 21},
            {"like", 21},
            {"working", 17},
            {"know", 17},
            {"able", 15},
            {"text", 14},
            {"user", 14},
            {"different", 13},
            {"ability", 13},
            {"files", 13},
            {"sure", 13},
            {"helpful", 12},
            {"see", 11},
            {"document", 11},
            {"easier", 11},
            {"format", 11},
            {"maps", 11},
            {"available", 10},
            {"graphics", 10},
            {"search", 10},
            {"tools", 10},
            {"improved", 9},
            {"need", 9},
            {"create", 8},
            {"functionality", 8},
            {"import", 8},
            {"interface", 8},
            {"just", 8},
            {"make", 8},
            {"new", 8},
            {"one", 8},
            {"option", 8},
            {"pdf", 8},
            {"really", 8},
            {"view", 8},
            {"visualization", 8},
            {"way", 8},
            {"queries", 7},
            {"currently", 7},
            {"friendly", 7},
            {"good", 7},
            {"social", 7},
            {"still", 7},
            {"time", 7},
            {"click", 7},
            {"page", 7},
            {"print", 7},
            {"tables", 7},
            {"cloud", 6},
            {"export", 6},
            {"cases", 6},
            {"far", 6},
            {"now", 6},
            {"often", 6},
            {"software", 6},
            {"themes", 6},
            {"tried", 6},
            {"visualise", 6},
            {"also", 5},
            {"compatibility", 5},
            {"content", 5},
            {"edit", 5},
            {"get", 5},
            {"great", 5},
            {"highlighting", 5},
            {"included", 5},
            {"learning", 5},
            {"list", 5},
            {"looks", 5},
            {"missing", 5},
            {"much", 5},
            {"network", 5},
            {"present", 5},
            {"product", 5},
            {"project", 5},
            {"stripe", 5},
            {"think", 5},
            {"title", 5},
            {"video", 5},
            {"without", 5},
            {"area", 4},
            {"audio", 4},
            {"basic", 4},
            {"button", 4},
            {"comparison", 4},
            {"convert", 4},
            {"downloading", 4},
            {"enough", 4},
            {"etc", 4},
        };

        private WordCloudData _wordcloudControlDataContext;
        private WordCloudData _wordCloudData;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}