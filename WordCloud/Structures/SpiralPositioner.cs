using System;
using System.Windows;

namespace WordCloud.Structures
{
    class SpiralPositioner : IPositioner
    {
        private readonly double _awayStep;
        private readonly Size _canvasSize;

        public SpiralPositioner(Size canvasSize)
        {
            _canvasSize = canvasSize;
            var coils = Math.Max(_canvasSize.Width / 2, _canvasSize.Height / 2) / Chord;
            _deltaMax = coils * 2 * Math.PI;
            _awayStep = Math.Max(_canvasSize.Width / 2, _canvasSize.Height / 2) / _deltaMax;
        }

        private readonly double _deltaMax;
        public double StartX { get; set; }
        public double StartY { get; set; }
        private double _delta = 1;

        public double Delta
        {
            get => _delta;
            set => _delta = value;
        }

        private const double Chord = 10;

        public bool GetNextPoint(out double x, out double y)
        {
            const double rotation = Math.PI / 30;

            var away = _awayStep * _delta;

            x = StartX + Math.Cos(_delta + rotation) * away;
            y = StartY + Math.Sin(_delta + rotation) * away;

            _delta += Chord / away;

            return _delta <= _deltaMax;
        }
    }
}