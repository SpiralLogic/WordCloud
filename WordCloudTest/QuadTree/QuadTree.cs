using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace WordCloudTest.QuadTree
{
    class BoundsQuadTree<T>
    {
        private const int MaxItemsPerNode = 5;
        
        private const int TopLeft = 0;
        private const int TopRight = 1;
        private const int BottomRight = 2;
        private const int BottomLeft = 3;


        private readonly int _depth;
        private Rect _boundary;



        private BoundsQuadTree<T>[] _regions;


        private readonly IDictionary<T, Rect> _items = new Dictionary<T, Rect>();
        private readonly IDictionary<T, Rect> _borderItems = new Dictionary<T, Rect>();

        public BoundsQuadTree(Rect boundingBox, int depth = 5)
        {
            _depth = depth;
            _boundary = boundingBox;
        }

        public bool Insert(T item, Rect itemBounds)
        {
            if (!_boundary.Contains(itemBounds)) return false;

            if (_regions != null)
            {
                var node = GetNode(itemBounds);

                if (node != null) return node.Insert(item, itemBounds);

                _borderItems.Add(item, itemBounds);
                
                return true;
            }

            _items.Add(item, itemBounds);

            if (_items.Count >= MaxItemsPerNode && _depth != 0)
            {
                Divide();

                foreach (var kvp in _items)
                {
                    Insert(kvp.Key, kvp.Value);
                }
                
                _items.Clear();
            }

            return true;
        }

        private void Divide()
        {
            var regionSize = new Size(_boundary.Width / 2, _boundary.Height / 2);

            _regions = new BoundsQuadTree<T>[4];
            
            _regions[TopLeft] = new BoundsQuadTree<T>(new Rect(_boundary.TopLeft, regionSize), _depth - 1);
            _regions[TopRight] = new BoundsQuadTree<T>(new Rect(new Point(_boundary.Left + regionSize.Width, _boundary.Top), regionSize), _depth - 1);
            _regions[BottomRight] = new BoundsQuadTree<T>(new Rect(new Point(_boundary.Left + regionSize.Width, _boundary.Top + regionSize.Height), regionSize), _depth - 1);
            _regions[BottomLeft] = new BoundsQuadTree<T>(new Rect(new Point(_boundary.Left, _boundary.Top + regionSize.Height), regionSize), _depth - 1);
        }

        public Rect GetQuad(T item, Rect bounds)
        {
            var node = this;
            if (_regions != null)
            {
                node = GetNode(bounds);
                if (node != null)
                {
                    return node._boundary;
                }
            }
            
            if (_boundary.Contains(bounds)) return this._boundary;

            throw new AbandonedMutexException();
        }

        public IEnumerable<T> QueryRange(Rect range)
        {
            var itemList = new List<T>(_items.Count + _borderItems.Count);
            AddQueryRange(range, itemList);

            return itemList;
        }

        private void AddQueryRange(Rect range, List<T> currentList)
        {
            if (_depth == 0 || !_boundary.Contains(range)) return;

            currentList.AddRange(_items.Keys);
            currentList.AddRange(_borderItems.Keys);

            if (_regions != null)
            {
                GetNode(range)?.AddQueryRange(range, currentList);
            }
        }

        private BoundsQuadTree<T> GetNode(Rect bounds)
        {
            if (_regions[TopLeft]._boundary.Contains(bounds)) return _regions[TopLeft];
            if (_regions[TopRight]._boundary.Contains(bounds)) return _regions[TopRight];
            if (_regions[BottomRight]._boundary.Contains(bounds)) return _regions[BottomRight];
            if (_regions[BottomLeft]._boundary.Contains(bounds)) return _regions[BottomLeft];

            return null;
        }
    }
}