using System.Windows;

namespace WordCloudTest.QuadTree
{
    internal class PointQuadTree<T> : QuadTreeBase<Point, T>
    {
        public PointQuadTree(Rect boundingBox, int depth = 5) : base(boundingBox, depth)
        {
        }

        protected override bool IsInRange(Point location)
        {
            return Boundary.Contains(location);
        }

        protected override QuadTreeBase<Point, T> CreateNode(Rect region, int depth)
        {
            return new PointQuadTree<T>(region, depth);
        }
    }
}