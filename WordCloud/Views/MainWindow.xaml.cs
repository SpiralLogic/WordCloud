using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using WordCloud.Annotations;
using WordCloud.Structures;
using WordCloud.WordFrequencyQuery;

namespace WordCloud.Views
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
                //      wordlist.Add(new FrequencyTableRow<WordGroup>(new WordGroup(kvp.Key + "z"), kvp.Value));
                count += kvp.Value;
            }

            Words = new FrequencyTable<WordGroup>(wordlist, count);

            _wordcloudControlDataContext = new WordCloudData(Words);

            WordCloudControl.DataContext = _wordcloudControlDataContext;
        }

        public FrequencyTable<WordGroup> Words;
        
        private async void DoWordCloud(object sender, RoutedEventArgs e)
        {
            var s = new Stopwatch();
            s.Start();

            await WordCloudControl.AddWords(_wordcloudControlDataContext);
            s.Stop();
            Time.Text = s.ElapsedMilliseconds.ToString();
            Failures.Text = WordCloudControl.Failures.ToString();
            Debug.WriteLine(s.ElapsedMilliseconds);
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
            {"coding", 35000},
            {"windows", 1000},
            {"wawa", 9980},
            {"mac", 9970},
            {"use", 9960},
            {"features", 9950},
            {"analysis", 9940},
            {"version", 9930},
            {"data", 9920},
            {"nodes", 9910},
            {"better", 9900},
            {"word", 9890},
            {"like", 9880},
            {"working", 9870},
            {"know", 9860},
            {"able", 9850},
            {"text", 9840},
            {"user", 9830},
            {"different", 9820},
            {"ability", 9810},
            {"files", 9800},
            {"sure", 9790},
            {"helpful", 9780},
            {"see", 9770},
            {"document", 9760},
            {"easier", 9750},
            {"format", 9740},
            {"maps", 9730},
            {"available", 9720},
            {"graphics", 9710},
            {"search", 9700},
            {"tools", 9690},
            {"improved", 9680},
            {"need", 9670},
            {"create", 9660},
            {"functionality", 9650},
            {"import", 9640},
            {"interface", 9630},
            {"just", 9620},
            {"make", 9610},
            {"new", 9600},
            {"one", 9590},
            {"option", 9580},
            {"pdf", 9570},
            {"really", 9560},
            {"view", 9550},
            {"visualization", 9540},
            {"way", 9530},
            {"queries", 9520},
            {"curdfgdfgren", 9510},
            {"friendly", 9500},
            {"good", 9490},
            {"social", 9480},
            {"still", 9470},
            {"time", 9460},
            {"click", 9450},
            {"page", 9440},
            {"prindfgdfgt", 9430},
            {"tables", 9420},
            {"cloud", 9410},
            {"export", 9400},
            {"cases", 9390},
            {"far", 9380},
            {"now", 9370},
            {"often", 9360},
            {"software", 9350},
            {"themes", 9340},
            {"tried", 9330},
            {"visualise", 9320},
            {"also", 9310},
            {"compatibility", 9300},
            {"content", 9290},
            {"edit", 9280},
            {"gedgfdfgt", 9270},
            {"great", 9260},
            {"highlighting", 9250},
            {"included", 9240},
            {"learning", 9230},
            {"list", 9220},
            {"looks", 9210},
            {"missing", 9200},
            {"much", 9190},
            {"network", 9180},
            {"present", 9170},
            {"product", 9160},
            {"project", 9150},
            {"stripe", 9140},
            {"think", 9130},
            {"title", 9120},
            {"video", 9110},
            {"without", 9100},
            {"area", 9090},
            {"audio", 9080},
            {"basic", 9070},
            {"bufdgdfgtton", 9060},
            {"comparison", 9050},
            {"convert", 9040},
            {"downloading", 9030},
            {"enough", 9020},
            {"etc", 9010},
            {"windows2", 9000},
            {"wawa2", 8990},
            {"mac2", 8980},
            {"use2", 8970},
            {"features2", 8960},
            {"analysis2", 8950},
            {"version2", 8940},
            {"data2", 8930},
            {"nodes2", 8920},
            {"better2", 8910},
            {"word2", 8900},
            {"like2", 8890},
            {"working2", 8880},
            {"know2", 8870},
            {"able2", 8860},
            {"text2", 8850},
            {"user2", 8840},
            {"different2", 8830},
            {"ability2", 8820},
            {"files2", 8810},
            {"sure2", 8800},
            {"helpful2", 8790},
            {"see2", 8780},
            {"document2", 8770},
            {"easier2", 8760},
            {"format2", 8750},
            {"maps2", 8740},
            {"available2", 8730},
            {"graphics2", 8720},
            {"search2", 8710},
            {"tools2", 8700},
            {"improved2", 8690},
            {"need2", 8680},
            {"create2", 8670},
            {"functionality2", 8660},
            {"import2", 8650},
            {"interface2", 8640},
            {"just2", 8630},
            {"make2", 8620},
            {"new2", 8610},
            {"one2", 8600},
            {"option2", 8590},
            {"pdf2", 8580},
            {"really2", 8570},
            {"view2", 8560},
            {"visualization2", 8550},
            {"way2", 8540},
            {"queries2", 8530},
            {"currently2", 8520},
            {"friendly2", 8510},
            {"good2", 8500},
            {"social2", 8490},
            {"still2", 8480},
            {"time2", 8470},
            {"click2", 8460},
            {"page2", 8450},
            {"print2", 8440},
            {"tables2", 8430},
            {"cloud2", 8420},
            {"export2", 8410},
            {"cases2", 8400},
            {"far2", 8390},
            {"now2", 8380},
            {"often2", 8370},
            {"software2", 8360},
            {"themes2", 8350},
            {"tried2", 8340},
            {"visualise2", 8330},
            {"also2", 8320},
            {"compatibility2", 8310},
            {"content2", 8300},
            {"edit2", 8290},
            {"get2", 8280},
            {"great2", 8270},
            {"highlighting2", 8260},
            {"included2", 8250},
            {"learning2", 8240},
            {"list2", 8230},
            {"looks2", 8220},
            {"missing2", 8210},
            {"much2", 8200},
            {"network2", 8190},
            {"present2", 8180},
            {"product2", 8170},
            {"project2", 8160},
            {"stripe2", 8150},
            {"think2", 8140},
            {"title2", 8130},
            {"video2", 8120},
            {"without2", 8110},
            {"area2", 8100},
            {"audio2", 8090},
            {"basic2", 8080},
            {"button2", 8070},
            {"comparison2", 8060},
            {"convert2", 8050},
            {"downloading2", 8040},
            {"enough2", 8030},
            {"etc2", 8020},
            {"coding3", 8010},
            {"windows3", 8000},
            {"wawa3", 7990},
            {"mac3", 7980},
            {"use3", 7970},
            {"features3", 7960},
            {"analysis3", 7950},
            {"version3", 7940},
            {"data3", 7930},
            {"nodes3", 7920},
            {"better3", 7910},
            {"word3", 7900},
            {"like3", 7890},
            {"working3", 7880},
            {"know3", 7870},
            {"able3", 7860},
            {"text3", 7850},
            {"user3", 7840},
            {"different3", 7830},
            {"ability3", 7820},
            {"files3", 7810},
            {"sure3", 7800},
            {"helpful3", 7790},
            {"see3", 7780},
            {"document3", 7770},
            {"easier3", 7760},
            {"format3", 7750},
            {"maps3", 7740},
            {"available3", 7730},
            {"graphics3", 7720},
            {"search3", 7710},
            {"tools3", 7700},
            {"improved3", 7690},
            {"need3", 7680},
            {"create3", 7670},
            {"functionality3", 7660},
            {"import3", 7650},
            {"interface3", 7640},
            {"just3", 7630},
            {"make3", 7620},
            {"new3", 7610},
            {"one3", 7600},
            {"option3", 7590},
            {"pdf3", 7580},
            {"really3", 7570},
            {"view3", 7560},
            {"visualization3", 7550},
            {"way3", 7540},
            {"queries3", 7530},
            {"currently3", 7520},
            {"friendly3", 7510},
            {"good3", 7500},
            {"social3", 7490},
            {"still3", 7480},
            {"time3", 7470},
            {"click3", 7460},
            {"page3", 7450},
            {"print3", 7440},
            {"tables3", 7430},
            {"cloud3", 7420},
            {"export3", 7410},
            {"cases3", 7400},
            {"far3", 7390},
            {"now3", 7380},
            {"often3", 7370},
            {"software3", 7360},
            {"themes3", 7350},
            {"tried3", 7340},
            {"visualise3", 7330},
            {"also3", 7320},
            {"compatibility3", 7310},
            {"content3", 7300},
            {"edit3", 7290},
            {"get3", 7280},
            {"great3", 7270},
            {"highlighting3", 7260},
            {"included3", 7250},
            {"learning3", 7240},
            {"list3", 7230},
            {"looks3", 7220},
            {"missing3", 7210},
            {"much3", 7200},
            {"network3", 7190},
            {"present3", 7180},
            {"product3", 7170},
            {"project3", 7160},
            {"stripe3", 7150},
            {"think3", 7140},
            {"title3", 7130},
            {"video3", 7120},
            {"without3", 7110},
            {"area3", 7100},
            {"audio3", 7090},
            {"basic3", 7080},
            {"button3", 7070},
            {"comparison3", 7060},
            {"convert3", 7050},
            {"downloading3", 7040},
            {"enough3", 7030},
            {"etc3", 7020},
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