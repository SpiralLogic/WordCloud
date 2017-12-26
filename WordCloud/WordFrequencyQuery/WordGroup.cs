using System.Collections.Generic;

namespace WordCloud.WordFrequencyQuery
{
    public class WordGroup
    {
        public WordGroup(string word)
            :this(word, new List<string>())
        { }

        public WordGroup(string word, IList<string> groupedWords)
        {
            Word = word;
            GroupedWords = groupedWords;
        }

        public string Word { get; set; }
        public IList<string> GroupedWords { get; }

        public override string ToString()
        {
            return string.Join(", ", GroupedWords);
        }
    }
}
