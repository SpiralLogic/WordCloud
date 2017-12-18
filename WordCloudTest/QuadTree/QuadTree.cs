using System;
using System.Collections.Generic;
using System.Windows;

namespace WordCloudTest.QuadTree
{
    internal class QuadTreeBase<TK, T>
    {
        protected const int MaxItemsPerNode = 5;
        protected const int TopLeft = 0;
        protected const int TopRight = 1;
        protected const int BottomRight = 2;
        protected const int BottomLeft = 3;

        protected Rect Boundary;
        protected int Depth;
        protected readonly IDictionary<T, TK> Items = new Dictionary<T, TK>();
        protected QuadTreeBase<TK, T>[] Nodes;

        protected QuadTreeBase(Rect boundingBox, int depth = 5)
        {
            Depth = depth;
            Boundary = boundingBox;
        }

        public virtual bool Insert(T item, TK itemLocation)
        {
            if (!IsInRange(itemLocation))
                throw new ArgumentOutOfRangeException(nameof(itemLocation));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (Nodes != null)
            {
                return GetNode(itemLocation)?.Insert(item, itemLocation) ?? false;
            }

            Items.Add(item, itemLocation);

            if (Items.Count < MaxItemsPerNode || Depth == 0) return true;

            Divide();

            return true;
        }

        public virtual IEnumerable<T> QueryLocation(TK range)
        {
            var itemList = new List<T>(Items.Count);
            AddNodeQueryRanges(range, itemList);

            return itemList;
        }

        protected virtual void AddNodeQueryRanges(TK location, List<T> currentList)
        {
            if (Depth == 0 || !IsInRange(location)) return;

            currentList.AddRange(Items.Keys);

            if (Nodes != null)
            {
                GetNode(location)?.AddNodeQueryRanges(location, currentList);
            }
        }

        protected QuadTreeBase<TK, T> GetNode(TK location)
        {
            if (Nodes[TopLeft].IsInRange(location)) return Nodes[TopLeft];
            if (Nodes[TopRight].IsInRange(location)) return Nodes[TopRight];
            if (Nodes[BottomRight].IsInRange(location)) return Nodes[BottomRight];
            if (Nodes[BottomLeft].IsInRange(location)) return Nodes[BottomLeft];

            return null;
        }
        
        protected void Divide()
        {
            var boundarySize = new Size(Boundary.Width / 2, Boundary.Height / 2);

            Nodes = new QuadTreeBase<TK, T>[4];

            Nodes[TopLeft] = new QuadTreeBase<TK, T>(new Rect(Boundary.TopLeft, boundarySize), Depth - 1);
            Nodes[TopRight] = new QuadTreeBase<TK, T>(new Rect(new Point(Boundary.Left + boundarySize.Width, Boundary.Top), boundarySize), Depth - 1);
            Nodes[BottomRight] = new QuadTreeBase<TK, T>(new Rect(new Point(Boundary.Left + boundarySize.Width, Boundary.Top + boundarySize.Height), boundarySize), Depth - 1);
            Nodes[BottomLeft] = new QuadTreeBase<TK, T>(new Rect(new Point(Boundary.Left, Boundary.Top + boundarySize.Height), boundarySize), Depth - 1);

            foreach (var kvp in Items)
            {
                Insert(kvp.Key, kvp.Value);
            }

            Items.Clear();
        }

        protected virtual bool IsInRange(TK location)
        {
            return true;
        }
    }

    internal class PointQuadTree<T> : QuadTreeBase<Point, T>
    {
        public PointQuadTree(Rect boundingBox, int depth = 5) : base(boundingBox, depth)
        {
        }

        protected override bool IsInRange(Point location)
        {
            return Boundary.Contains(location);
        }
    }

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
    }
}