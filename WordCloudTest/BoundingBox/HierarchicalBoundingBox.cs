using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WordCloudTest.Annotations;

namespace WordCloudTest.BoundingBox
{
    class HierarchicalBoundingBox
    {
        private Rect _bounds;
        private readonly int _numberOfNodes;
        private readonly int _nodesVertical;
        private readonly int _nodesHorizontal;
        private Size _nodeSize;
        private HierarchicalBoundingBox[] _nodes;
        private bool _isHit;

        private HierarchicalBoundingBox(Rect bounds, int numberOfNodes = 4)
        {
            _bounds = bounds;
            _numberOfNodes = numberOfNodes;
            _nodesVertical = numberOfNodes / 2;
            _nodesHorizontal = numberOfNodes / 2;
        }

        public HierarchicalBoundingBox(Rect bounds)
        {
            _bounds = new Rect(new Size(bounds.Width, bounds.Height));
            if (_bounds.Height > _bounds.Width)
            {
                _numberOfNodes = 2 * (int) Math.Ceiling(_bounds.Height / _bounds.Width);
                _nodesVertical = _numberOfNodes / 2;
                _nodesHorizontal = 2;
            }
            else
            {
                _numberOfNodes = 2 * (int) Math.Ceiling(_bounds.Width / _bounds.Height);
                _nodesHorizontal = _numberOfNodes / 2;
                _nodesVertical = 2;
            }
        }

        public void Divide(Predicate<Rect> hitTest, int iterations = 1)
        {
            _isHit = hitTest(_bounds);
            if (iterations == 0 || !_isHit) return;


            var nodeSize = new Size(_bounds.Width / _nodesHorizontal, _bounds.Height / _nodesVertical);
            if (nodeSize.Width < 1 || nodeSize.Height < 1) return;
            _nodes = new HierarchicalBoundingBox[_numberOfNodes];
            for (var i = 0; i < _nodesVertical; ++i)
            {
                for (var j = 0; j < _nodesHorizontal; ++j)
                {
                    var boundingBox = new Rect(new Point(_bounds.X + j * nodeSize.Width, _bounds.Y + i * nodeSize.Height), nodeSize);
                    _nodes[i * _nodesHorizontal + j] = new HierarchicalBoundingBox(boundingBox, 4);
                    _nodes[i * _nodesHorizontal + j].Divide(hitTest, iterations - 1);
                }
            }
        }

        public IEnumerable<Rect> GetBoxesHit(Rect inter)
        {
            inter.X = 0;
            inter.Y = 0;
            var boxes = new List<Rect>();
            GetBoxesHitAdd(boxes, inter);
            return boxes;
        }

        private void GetBoxesHitAdd(IList<Rect> boxList, Rect inter)
        {
            if (!_bounds.IntersectsWith(inter) && !_bounds.Contains(inter) &&!_isHit) return;
            
            if (_nodes != null && _nodes.Any())
            {
                foreach (var node in _nodes)
                {
                    node.GetBoxesHitAdd(boxList, inter);
                }
                
                return;
            }
            if (_nodes == null && _isHit)
            {
                boxList.Add(_bounds);
            }
        }

        public IEnumerable<Rect> GetBoxesMissed()
        {
            var boxes = new List<Rect>();
            GetBoxesMissedAdd(boxes);
            return boxes;
        }

        private void GetBoxesMissedAdd(IList<Rect> boxList)
        {
            if (_nodes != null && _nodes.Any())
            {
                foreach (var node in _nodes)
                {
                    node.GetBoxesMissedAdd(boxList);
                }
                return;
            }
            if (_nodes == null && !_isHit)
            {
                boxList.Add(_bounds);
            }
        }

        public bool IsHit(Point startPoint1, Point startPoint2, HierarchicalBoundingBox toTest)
        {
            if (_nodes == null && !_bounds.Contains(startPoint2)) return false;
            var inter = new Rect(startPoint1, _bounds.Size);

            inter.Intersect(new Rect(startPoint2, toTest._bounds.Size));

            foreach (var s in GetBoxesHit(inter))
            {
                var ownBounds = new Rect(new Point(s.X + startPoint1.X, s.Y + startPoint1.Y), s.Size);

                foreach (var t in toTest.GetBoxesHit(inter))
                {
                    var toTextBounds = new Rect(new Point(t.X + startPoint2.X, t.Y + startPoint2.Y), t.Size);

                    if (ownBounds.Contains(toTextBounds) || ownBounds.IntersectsWith(toTextBounds)) return true;
                }
            }
            return false;
        }
    }
}