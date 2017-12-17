using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            foreach (var kvp in _words.OrderByDescending(x => x.Value))
            {
                wordlist.Add(new FrequencyTableRow<WordGroup>(new WordGroup(kvp.Key), kvp.Value));
                count += kvp.Value;
                
            }


            Words = new FrequencyTable<WordGroup>(wordlist, count);

            _wordcloudControlDataContext = new WordCloudData(Words);

            WordcloudControl.DataContext = _wordcloudControlDataContext;
        }

        public FrequencyTable<WordGroup> Words;

        private void AddWord(object sender, RoutedEventArgs e)
        {
            WordcloudControl.DoStuff(_wordcloudControlDataContext);
        }

        private void DoWordCloud(object sender, RoutedEventArgs e)
        {
            WordcloudControl.RestartCloud(_wordcloudControlDataContext);
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
            {"wawa", 48},
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
            {"etc", 4},            {"coding2", 58},
            {"windows2", 52},
            {"wawa2", 48},
            {"mac2", 42},
            {"use2", 41},
            {"features2", 39},
            {"analysis2", 31},
            {"version2", 31},
            {"data2", 30},
            {"nodes2", 29},
            {"better2", 25},
            {"word2", 21},
            {"like2", 21},
            {"working2", 17},
            {"know2", 17},
            {"able2", 15},
            {"text2", 14},
            {"user2", 14},
            {"different2", 13},
            {"ability2", 13},
            {"files2", 13},
            {"sure2", 13},
            {"helpful2", 12},
            {"see2", 11},
            {"document2", 11},
            {"easier2", 11},
            {"format2", 11},
            {"maps2", 11},
            {"available2", 10},
            {"graphics2", 10},
            {"search2", 10},
            {"tools2", 10},
            {"improved2", 9},
            {"need2", 9},
            {"create2", 8},
            {"functionality2", 8},
            {"import2", 8},
            {"interface2", 8},
            {"just2", 8},
            {"make2", 8},
            {"new2", 8},
            {"one2", 8},
            {"option2", 8},
            {"pdf2", 8},
            {"really2", 8},
            {"view2", 8},
            {"visualization2", 8},
            {"way2", 8},
            {"queries2", 7},
            {"currently2", 7},
            {"friendly2", 7},
            {"good2", 7},
            {"social2", 7},
            {"still2", 7},
            {"time2", 7},
            {"click2", 7},
            {"page2", 7},
            {"print2", 7},
            {"tables2", 7},
            {"cloud2", 6},
            {"export2", 6},
            {"cases2", 6},
            {"far2", 6},
            {"now2", 6},
            {"often2", 6},
            {"software2", 6},
            {"themes2", 6},
            {"tried2", 6},
            {"visualise2", 6},
            {"also2", 5},
            {"compatibility2", 5},
            {"content2", 5},
            {"edit2", 5},
            {"get2", 5},
            {"great2", 5},
            {"highlighting2", 5},
            {"included2", 5},
            {"learning2", 5},
            {"list2", 5},
            {"looks2", 5},
            {"missing2", 5},
            {"much2", 5},
            {"network2", 5},
            {"present2", 5},
            {"product2", 5},
            {"project2", 5},
            {"stripe2", 5},
            {"think2", 5},
            {"title2", 5},
            {"video2", 5},
            {"without2", 5},
            {"area2", 4},
            {"audio2", 4},
            {"basic2", 4},
            {"button2", 4},
            {"comparison2", 4},
            {"convert2", 4},
            {"downloading2", 4},
            {"enough2", 4},
            {"etc2", 4},
                        {"coding3", 58},
            {"windows3", 52},
            {"wawa3", 48},
            {"mac3", 42},
            {"use3", 41},
            {"features3", 39},
            {"analysis3", 31},
            {"version3", 31},
            {"data3", 30},
            {"nodes3", 29},
            {"better3", 25},
            {"word3", 21},
            {"like3", 21},
            {"working3", 17},
            {"know3", 17},
            {"able3", 15},
            {"text3", 14},
            {"user3", 14},
            {"different3", 13},
            {"ability3", 13},
            {"files3", 13},
            {"sure3", 13},
            {"helpful3", 12},
            {"see3", 11},
            {"document3", 11},
            {"easier3", 11},
            {"format3", 11},
            {"maps3", 11},
            {"available3", 10},
            {"graphics3", 10},
            {"search3", 10},
            {"tools3", 10},
            {"improved3", 9},
            {"need3", 9},
            {"create3", 8},
            {"functionality3", 8},
            {"import3", 8},
            {"interface3", 8},
            {"just3", 8},
            {"make3", 8},
            {"new3", 8},
            {"one3", 8},
            {"option3", 8},
            {"pdf3", 8},
            {"really3", 8},
            {"view3", 8},
            {"visualization3", 8},
            {"way3", 8},
            {"queries3", 7},
            {"currently3", 7},
            {"friendly3", 7},
            {"good3", 7},
            {"social3", 7},
            {"still3", 7},
            {"time3", 7},
            {"click3", 7},
            {"page3", 7},
            {"print3", 7},
            {"tables3", 7},
            {"cloud3", 6},
            {"export3", 6},
            {"cases3", 6},
            {"far3", 6},
            {"now3", 6},
            {"often3", 6},
            {"software3", 6},
            {"themes3", 6},
            {"tried3", 6},
            {"visualise3", 6},
            {"also3", 5},
            {"compatibility3", 5},
            {"content3", 5},
            {"edit3", 5},
            {"get3", 5},
            {"great3", 5},
            {"highlighting3", 5},
            {"included3", 5},
            {"learning3", 5},
            {"list3", 5},
            {"looks3", 5},
            {"missing3", 5},
            {"much3", 5},
            {"network3", 5},
            {"present3", 5},
            {"product3", 5},
            {"project3", 5},
            {"stripe3", 5},
            {"think3", 5},
            {"title3", 5},
            {"video3", 5},
            {"without3", 5},
            {"area3", 4},
            {"audio3", 4},
            {"basic3", 4},
            {"button3", 4},
            {"comparison3", 4},
            {"convert3", 4},
            {"downloading3", 4},
            {"enough3", 4},
            {"etc3", 4},

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