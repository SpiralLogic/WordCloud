using System;
using System.Collections.Generic;

namespace WordCloud.WordFrequencyQuery
{
    /// <summary>
    /// Frequency table generic class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrequencyTable<T>
    {
        public FrequencyTable(IList<FrequencyTableRow<T>> rows, int totalCount)
        {
            if (totalCount < 0)
                throw new ArgumentOutOfRangeException(nameof(totalCount));

            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
            TotalCount = totalCount;
        }

        /// <summary>
        /// Row elements of the table
        /// </summary>
        public IList<FrequencyTableRow<T>> Rows { get; }

        /// <summary>
        /// Total count, this may not be the same as the sum of the rows
        /// </summary>
        public int TotalCount { get; }
    }

    /// <summary>
    /// Row in frequency table
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrequencyTableRow<T>
    {
        public FrequencyTableRow(T item, int count)
        {
            Item = item;
            Count = count;
        }

        /// <summary>
        /// Item of frequency table
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Number of occurrences
        /// </summary>
        public int Count { get; }
    }
}
