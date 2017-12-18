using System;
using System.Collections.Generic;
using System.Windows;

namespace WordCloudTest.QuadTree
{
    internal class RectQuadTree<T> : QuadTreeBase<Rect, T>
    {
        private readonly IDictionary<T, Rect> _borderItems = new Dictionary<T, Rect>();

        public RectQuadTree(Rect boundingBox, int depth = 5) : base(boundingBox, depth)
        {
        }

        public override bool Insert(T item, Rect itemLocation)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (!Boundary.Contains(itemLocation))
                return false;

            if (Nodes != null)
            {
                var node = GetNode(itemLocation);

                if (node == null)
                {
                    _borderItems.Add(item, itemLocation);

                    return true;
                }

                return node.Insert(item, itemLocation);
            }

            Items.Add(item, itemLocation);

            if (Items.Count < MaxItemsPerNode || Depth == 0) return true;

            Divide();

            return true;
        }

        public override IEnumerable<T> QueryLocation(Rect range)
        {
            var itemList = new List<T>(Items.Count + _borderItems.Count);
            AddNodeQueryRanges(range, itemList);

            return itemList;
        }

        protected override bool IsInRange(Rect location)
        {
            return Boundary.Contains(location);
        }

        protected override QuadTreeBase<Rect, T> CreateNode(Rect region, int depth)
        {
            return new RectQuadTree<T>(region, depth);
        }
    }
}