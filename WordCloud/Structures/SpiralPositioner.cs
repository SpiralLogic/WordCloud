using System;
using System.Windows;

namespace WordCloud.Structures
{
    class SpiralPositioner : IPositioner
    {
        private readonly double _awayStep;

        public SpiralPositioner(Point centerPoint)
        {
            var coils = Math.Max(centerPoint.X, centerPoint.Y) / Chord;
            DeltaMax = coils * 2 * Math.PI;

            _awayStep = Math.Max(centerPoint.X, centerPoint.Y) / DeltaMax;
        }

        public double DeltaMax { get; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double Delta { get; set; } = 1;
        public double Chord { get; set; } = 10;

        public bool GetNextPoint(out double x, out double y)
        {
            const double rotation = Math.PI / 30;

            var away = _awayStep * Delta;

            x = StartX + Math.Cos(Delta + rotation) * away;
            y = StartY + Math.Sin(Delta + rotation) * away;

            Delta += Chord / away;

            return Delta <= DeltaMax;
        }
    }
}