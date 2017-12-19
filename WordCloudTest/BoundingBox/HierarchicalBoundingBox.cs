using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace WordCloudTest.BoundingBox
{
    class HierarchicalBoundingBox<T> where T : Geometry
    {
        private readonly Size _minNodeSize = new Size(4, 4);
        private const int LeafExpansion = 2;
        protected const int TopLeft = 0;
        protected const int TopRight = 1;
        protected const int BottomRight = 2;
        protected const int BottomLeft = 3;

        private Rect _localBounds;
        private Rect _globalBounds;
        private Point _globalLocation;

        public readonly T _geo;
        public HierarchicalBoundingBox<T>[] _nodes;
        private bool? _isHit;
        private bool _isLeaf;
        private bool _isFrozen = false;

        public Point GlobalLocation
        {
            get
            {
                return _globalLocation;
            }
            set
            {
                if (!_isFrozen)
                {
                    _globalLocation = value;
                    if (_nodes != null)
                    foreach (var node in _nodes)
                    {
                        node.GlobalLocation = value;
                    }
                }
            }
        }

        public Rect GlobalBounds
        {
            get => _isFrozen ? _globalBounds : new Rect(new Point(GlobalLocation.X + _localBounds.X, GlobalLocation.Y + _localBounds.Y), _localBounds.Size);
        }

        protected HierarchicalBoundingBox(T geo, Rect localBounds, bool isLeaf = false)
        {
            _localBounds = localBounds;
            _isLeaf = isLeaf || localBounds.Width <= _minNodeSize.Width && localBounds.Height <= _minNodeSize.Height;

            if (_isLeaf)
            {
                _localBounds.Width += LeafExpansion;
                _localBounds.X -= LeafExpansion / 2.0;
                _localBounds.Height += LeafExpansion;
                _localBounds.Y -= LeafExpansion / 2.0;
            }

            _geo = geo;
        }

        public HierarchicalBoundingBox(T geo) : this(geo, new Rect(geo.Bounds.Size))
        {
            _globalLocation = geo.Bounds.Location;
        }

        public bool IsRegionHit(Rect testRegion)
        {
            if (!GlobalBounds.Contains(testRegion) && !GlobalBounds.IntersectsWith(testRegion)) return false;

            if (_isLeaf && _isHit.HasValue) return _isHit.Value;

            if (_nodes == null) Divide();

            if (_isLeaf)
            {
                var testRectangle = new RectangleGeometry(testRegion);
                _isHit = testRectangle.FillContainsWithDetail(_geo, .01, ToleranceType.Absolute) != IntersectionDetail.Empty;
                //     var cc= CombinedGeometry.Combine(testRectangle, _geo, GeometryCombineMode.Exclude, null);
                //  _isHit = cc.GetArea() < testRegion.Width * testRegion.Height;
                return _isHit.Value;
            }

            foreach (var node in _nodes)
            {
                if (node.IsRegionHit(testRegion))
                    return true;
            }

            return false;
        }

        public List<HierarchicalBoundingBox<T>> GetLeafNodesInRegion(Rect resultRegion)
        {
            var newList = new List<HierarchicalBoundingBox<T>>();
            GetLeafNodesInternal(ref resultRegion, newList);

            return newList;
        }

        private void GetLeafNodesInternal(ref Rect resultRegion, ICollection<HierarchicalBoundingBox<T>> newList)
        {
            if (!GlobalBounds.Contains(resultRegion) && !GlobalBounds.IntersectsWith(resultRegion)) return;

            if (_isLeaf)
            {
                newList.Add(this);
                return;
            }

            if (_nodes == null) Divide();

            if (_isLeaf)
            {
                newList.Add(this);
                return;
            }

            foreach (var node in _nodes)
            {
                node.GetLeafNodesInternal(ref resultRegion, newList);
            }
        }

        private void Divide()
        {
            if (_isLeaf || _isHit.HasValue) return;

            int hDiv = 2, vDiv = 2;
            var ratio = _localBounds.Width / _localBounds.Height;

            if (ratio > 1)
            {
                hDiv = Math.Min((int) Math.Floor(ratio * hDiv), 4);
            }
            else
            {
                vDiv = Math.Min((int) Math.Floor(1 / ratio) * vDiv, 4);
            }
            var newNodeSize = new Size(_localBounds.Width / hDiv, _localBounds.Height / vDiv);

            var areLeaves = _localBounds.Width < _minNodeSize.Width && _localBounds.Height < _minNodeSize.Height;
            _nodes = new HierarchicalBoundingBox<T>[hDiv * vDiv];
            for (var j = 0; j < vDiv; j++)
            {
                for (var i = 0; i < hDiv; i++)
                {
                    var newBounds = new Rect(new Point(_localBounds.X + i * newNodeSize.Width, _localBounds.Y + j * newNodeSize.Height), newNodeSize);

                    _nodes[i + hDiv * j] = new HierarchicalBoundingBox<T>(_geo, newBounds, areLeaves);
                    _nodes[i + hDiv * j]._globalLocation = GlobalLocation;
                }
            }
            if (_nodes.Length > 0)
            {
                _isLeaf = false;
                _isHit = null;
            }
        }

        public void Freeze()
        {
            _globalBounds = GlobalBounds;
            _globalLocation = GlobalLocation;
            if (_nodes != null)
            {
                foreach (var node in _nodes)
                {
                    node.Freeze();
                }
            }

            _isFrozen = true;
        }

        public bool DoBoxesCollide(HierarchicalBoundingBox<T> toTest)
        {
            var testRegion = Rect.Intersect(GlobalBounds, toTest.GlobalBounds);

            if (testRegion.IsEmpty) return false;
            
            var remoteNodes = toTest.GetLeafNodesInRegion(testRegion);
            foreach (var remoteNode in remoteNodes)
            {
                if (IsRegionHit(testRegion) && remoteNode.IsRegionHit(testRegion))
                    return true;
            }
            return false;
        }
    }
}