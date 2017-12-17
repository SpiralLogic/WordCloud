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

        protected Rect _boundary;
        protected int _depth;
        protected readonly IDictionary<T, TK> _items = new Dictionary<T, TK>();
        protected QuadTreeBase<TK, T>[] _nodes;

        protected QuadTreeBase(Rect boundingBox, int depth = 5)
        {
            _depth = depth;
            _boundary = boundingBox;
        }


        public virtual bool Insert(T item, TK itemLocation)
        {
            if (!IsInRange(itemLocation))
                throw new ArgumentOutOfRangeException(nameof(itemLocation));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_nodes != null)
            {
                return GetNode(itemLocation)?.Insert(item, itemLocation) ?? false;
            }

            _items.Add(item, itemLocation);

            if (_items.Count < MaxItemsPerNode || _depth == 0) return true;

            Divide();

            return true;
        }

        public virtual IEnumerable<T> QueryLocation(TK range)
        {
            var itemList = new List<T>(_items.Count);
            AddNodeQueryRanges(range, itemList);

            return itemList;
        }

        protected virtual void AddNodeQueryRanges(TK location, List<T> currentList)
        {
            if (_depth == 0 || !IsInRange(location)) return;

            currentList.AddRange(_items.Keys);

            if (_nodes != null)
            {
                GetNode(location)?.AddNodeQueryRanges(location, currentList);
            }
        }

        protected QuadTreeBase<TK, T> GetNode(TK location)
        {
            if (_nodes[TopLeft].IsInRange(location)) return _nodes[TopLeft];
            if (_nodes[TopRight].IsInRange(location)) return _nodes[TopRight];
            if (_nodes[BottomRight].IsInRange(location)) return _nodes[BottomRight];
            if (_nodes[BottomLeft].IsInRange(location)) return _nodes[BottomLeft];

            return null;
        }

        protected void Divide()
        {
            var boundarySize = new Size(_boundary.Width / 2, _boundary.Height / 2);

            _nodes = new QuadTreeBase<TK, T>[4];

            _nodes[TopLeft] = new QuadTreeBase<TK, T>(new Rect(_boundary.TopLeft, boundarySize), _depth - 1);
            _nodes[TopRight] = new QuadTreeBase<TK, T>(new Rect(new Point(_boundary.Left + boundarySize.Width, _boundary.Top), boundarySize), _depth - 1);
            _nodes[BottomRight] = new QuadTreeBase<TK, T>(new Rect(new Point(_boundary.Left + boundarySize.Width, _boundary.Top + boundarySize.Height), boundarySize), _depth - 1);
            _nodes[BottomLeft] = new QuadTreeBase<TK, T>(new Rect(new Point(_boundary.Left, _boundary.Top + boundarySize.Height), boundarySize), _depth - 1);

            foreach (var kvp in _items)
            {
                Insert(kvp.Key, kvp.Value);
            }

            _items.Clear();
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
            return _boundary.Contains(location);
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

            if (!_boundary.Contains(itemLocation))
                return false;

            if (_nodes != null)
            {
                var node = GetNode(itemLocation);

                if (node == null)
                {
                    _borderItems.Add(item, itemLocation);

                    return true;
                }

                return node.Insert(item, itemLocation);
            }

            _items.Add(item, itemLocation);

            if (_items.Count < MaxItemsPerNode || _depth == 0) return true;

            Divide();

            return true;
        }

        public override IEnumerable<T> QueryLocation(Rect range)
        {
            var itemList = new List<T>(_items.Count + _borderItems.Count);
            AddNodeQueryRanges(range, itemList);

            return itemList;
        }

        protected override bool IsInRange(Rect location)
        {
            return _boundary.Contains(location);
        }
    }
}