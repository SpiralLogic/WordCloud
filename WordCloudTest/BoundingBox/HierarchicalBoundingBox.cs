using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.Windows.Media;

namespace WordCloudTest.BoundingBox
{
    class HierarchicalBoundingBox<T> where T : Geometry
    {
        private const double MinimumNodeSize = 16;
        private const int LeafExpansion = 2;
        protected const int TopLeft = 0;
        protected const int TopRight = 1;
        protected const int BottomRight = 2;
        protected const int BottomLeft = 3;

        private Rect _localBounds;
        private Rect _globalBounds;
        private readonly T _geo;
        private readonly double _size;
        private HierarchicalBoundingBox<T>[] _regions;
        private bool _isHit;

        public Rect GlobalBounds
        {
            get => new Rect(new Point(_globalBounds.X + _localBounds.X, _globalBounds.Y + _localBounds.Y), _localBounds.Size);
            set
            {
                _globalBounds = value;
                if (_regions != null)
                    foreach (var node in _regions)
                        node.GlobalBounds = value;
            }
        }

        public HierarchicalBoundingBox(T geo, Rect localBounds, Rect globalBounds)
        {
            _localBounds = localBounds;

            var size = _localBounds.Size.Width * _localBounds.Size.Height;
            if (size < MinimumNodeSize)
            {
                _localBounds.Width += LeafExpansion;
                _localBounds.Height += LeafExpansion;
            }

            _globalBounds = globalBounds;
            _geo = geo;
            _size = _localBounds.Size.Width * _localBounds.Size.Height;
        }

        public bool IsRegionHit(Rect globalTestRegion)
        {
            if (_isHit) return _isHit;

            if (_regions == null)
            {
                Divide();
            }

            if (_regions == null)
            {
                var testRectangle = new RectangleGeometry(globalTestRegion);

                var result = testRectangle.FillContainsWithDetail(_geo) != IntersectionDetail.Empty;
                    using (var c = Bbdg.Append())
                    {
                        c.DrawGeometry(null, new Pen(Brushes.DeepPink, 0.5), testRectangle);
                    }

                _isHit = result;

                return _isHit;
            }

            foreach (var node in _regions)
            {
                if (node.IsRegionHit(globalTestRegion))
                    return true;
            }

            return false;
        }

        public List<HierarchicalBoundingBox<T>> GetLeafNodesInRegion(Rect globalRegion)
        {
            var newList = new List<HierarchicalBoundingBox<T>>();
            GetLeafNodesInternal(globalRegion, newList);

            return newList;
        }

        private void GetLeafNodesInternal(Rect globalRegion, List<HierarchicalBoundingBox<T>> newList)
        {
            if (!GlobalBounds.Contains(globalRegion) && !GlobalBounds.IntersectsWith(globalRegion)) return;

            Divide();

            if (_regions == null)
            {
                newList.Add(this);
                return;
            }

            foreach (var node in _regions)
            {
                node.GetLeafNodesInternal(globalRegion, newList);
            }
        }

        public void Divide()
        {
            if (_regions != null) return;

            var nodeSize = new Size(_localBounds.Width / 2, _localBounds.Height / 2);
            bool half = false;
            if (nodeSize.Width < MinimumNodeSize / 4 && nodeSize.Height < MinimumNodeSize / 4) return;
            if (nodeSize.Width < MinimumNodeSize / 4)
            {
                nodeSize.Height = nodeSize.Height / 2;
                half = true;
            }
            else if (nodeSize.Height < MinimumNodeSize / 4)
            {
                half = true;

                nodeSize.Width /= 2;
            }

            _regions = new HierarchicalBoundingBox<T>[half ? 2 : 4];

            _regions[TopLeft] = new HierarchicalBoundingBox<T>(_geo, new Rect(_localBounds.TopLeft, nodeSize), _globalBounds);
            _regions[TopRight] = new HierarchicalBoundingBox<T>(_geo, new Rect(new Point(_localBounds.Left + nodeSize.Width, _localBounds.Top), nodeSize), _globalBounds);
            if (!half) _regions[BottomRight] = new HierarchicalBoundingBox<T>(_geo, new Rect(new Point(_localBounds.Left + nodeSize.Width, _localBounds.Top - nodeSize.Height), nodeSize), _globalBounds);
            if (!half) _regions[BottomLeft] = new HierarchicalBoundingBox<T>(_geo, new Rect(new Point(_localBounds.Left, _localBounds.Top + nodeSize.Height), nodeSize), _globalBounds);
        }

        public static DrawingGroup Bbdg = new DrawingGroup();

        public bool DoBoxesCollide(HierarchicalBoundingBox<T> toTestBoundingBox, DrawingGroup dg)
        {
            var testRegion = Rect.Intersect(GlobalBounds, toTestBoundingBox.GlobalBounds);
            var remoteNodes = toTestBoundingBox.GetLeafNodesInRegion(testRegion);

            if (Bbdg != null)
            {
                using (var c = Bbdg.Open())
                {
                    var localNodes = GetLeafNodesInRegion(testRegion);

                    foreach (var n in localNodes)
                    {
                        c.DrawRectangle(null, new Pen(Brushes.Black, 1), n.GlobalBounds);
                    }
                    foreach (var m in remoteNodes)
                    {
                        c.DrawRectangle(null, new Pen(Brushes.Blue, 0.5), m.GlobalBounds);
                    }

                    c.DrawRectangle(null, new Pen(Brushes.OrangeRed, 0.5), testRegion);
                //    c.DrawRectangle(null, new Pen(Brushes.DarkTurquoise, 0.5), GlobalBounds);
            //        c.DrawRectangle(null, new Pen(Brushes.Sienna, 0.5), toTestBoundingBox.GlobalBounds);
                }

                if (!dg.Children.Contains(Bbdg)) dg.Children.Add(Bbdg);
            }


            foreach (var remoteNode in remoteNodes)
            {
                if (IsRegionHit(remoteNode.GlobalBounds) && remoteNode.IsRegionHit(remoteNode.GlobalBounds)) return true;
            }

            return false;
        }
    }
}