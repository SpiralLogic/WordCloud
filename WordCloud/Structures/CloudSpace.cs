using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WordCloud.Views;

namespace WordCloud.Structures
{
    class CloudSpace
    {
        private StartPosition DefaultStartingPosition = StartPosition.Center;

        private readonly PixelFormat _pixelFormat = PixelFormats.Pbgra32;
        private readonly IRandomizer _randomizer;
        private readonly IPositioner _positioner;

        private int RepositionAttempts = 4;
        private const int Pbgra32Bytes = 4;
        private const int Pbgra32Alpha = 0;
        public double Width { get; }
        public double Height { get; }
        public Point CloudCenter;

        public BitArray _collisionMap;
        public int _collisionMapWidth;

        public int _collisionMapHeight;
        public List<Rect> _collisionRects = new List<Rect>();
        private readonly Pen _pen;
        public int FailedPlacements { get; private set; } = 0;

        private const int Buffer = 0;

        public CloudSpace(double width, double height)
        {
            Width = width;
            Height = height;
            CloudCenter = new Point(width / 2, height / 2);
            _positioner = new SpiralPositioner(CloudCenter);
            _pen = new Pen(Brushes.Purple, 1);
            _pen.Freeze();
        }

        public CloudSpace(double width, double height, IRandomizer randomizer = null) : this(width, height)
        {
            _randomizer = randomizer ?? new CryptoRandomizer();
            _positioner = new SpiralPositioner(CloudCenter);
        }

        private bool CalculateNextStartingPoint(WordGeo wordGeo)
        {
            if (!_positioner.GetNextPoint(out var x, out var y)) return false;

            wordGeo.X = x;
            wordGeo.Y = y;

            return true;
        }


        private void AdjustFinalPosition(byte[] newBytes, WordGeo wordGeo)
        {
            var previousX = wordGeo.X;
            while (!HasCollision(newBytes, wordGeo) && Math.Abs(CloudCenter.X - wordGeo.X) > Buffer + 3)
            {
                previousX = wordGeo.X;
                wordGeo.X += wordGeo.X > CloudCenter.X ? -Buffer - 3 : Buffer + 3;
            }
            wordGeo.X = previousX;

            var previousY = wordGeo.Y;
            while (!HasCollision(newBytes, wordGeo) && Math.Abs(CloudCenter.Y - wordGeo.Y) > Buffer + 3)
            {
                previousY = wordGeo.Y;
                wordGeo.Y += wordGeo.Y > CloudCenter.Y ? -Buffer - 3 : Buffer + 3;
            }
            wordGeo.Y = previousY;
        }


        private void CreateCollisionMap(WordGeo wordGeo)
        {
            SetStartingPosition(wordGeo, StartPosition.Center);
            _collisionMapWidth = (int) Width;
            _collisionMapHeight = (int) Height;

            var mainImageBytes = GetPixels(wordGeo, _collisionMapWidth, _collisionMapHeight);
            var totalPixels = _collisionMapHeight * _collisionMapWidth;

            _collisionMap = new BitArray(totalPixels);

            for (var i = 0; i < totalPixels - Pbgra32Bytes; ++i)
            {
                if (mainImageBytes[i * Pbgra32Bytes + 3] > 0) AddNewCollisionPoint(i);
            }
        }

        public void SetStartingPosition(WordGeo wordGeo, StartPosition position)
        {
            _positioner.Delta = wordGeo.Width / wordGeo.Height;
            switch (position)
            {
                case StartPosition.Center:
                    _positioner.StartX = CloudCenter.X - wordGeo.Center.X;
                    _positioner.StartY = CloudCenter.Y - wordGeo.Center.Y;
                    break;
                case StartPosition.Random:
                    _positioner.StartX = wordGeo.Center.X + _randomizer.RandomInt((int) (_collisionMapWidth - wordGeo.Center.X));
                    _positioner.StartY = wordGeo.Center.Y + _randomizer.RandomInt((int) (_collisionMapHeight - wordGeo.Center.Y));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }

            CalculateNextStartingPoint(wordGeo);
        }

        private void AddNewCollisionPoint(int index)
        {
            var y = index / _collisionMapWidth;
            var x = index % _collisionMapWidth;
            _collisionMap[index] = true;

            for (var i = 1; i <= Buffer; i++)
            {
                if (x < _collisionMapWidth - i) _collisionMap[index + i] = true;
                if (y < _collisionMapHeight - i) _collisionMap[index + _collisionMapWidth * i] = true;
                if (x > i - 1) _collisionMap[index - i] = true;
                if (y > i - 1) _collisionMap[index - _collisionMapWidth * i] = true;
            }
        }


        private bool HasCollision(IReadOnlyList<byte> newWordBytes, WordGeo newWord)
        {
            if (IsOutOfBounds(newWord)) return true;

            var srcWidth = _collisionMapWidth;

            var testX = newWord.IntX;
            var testY = newWord.IntY;
            var testWidth = newWord.IntWidth;
            var testHeight = newWord.IntHeight;
            for (var line = 0; line < testHeight; ++line)
            {
                var mapPosition = (testY + line) * srcWidth + testX;
                var testOffset = line * testWidth * Pbgra32Bytes;
                for (var i = 0; i < testWidth * Pbgra32Bytes; i += Pbgra32Bytes)
                {
                    var mapIndex = mapPosition + i / Pbgra32Bytes;
                    
                    var isCollisionPoint = newWordBytes[testOffset + i] != Pbgra32Alpha;
                    if (isCollisionPoint && _collisionMap[mapIndex]) return true;

                }
            }
            //_collisionRects.Add(new Rect(testX, testY, testWidth, testHeight));
            return false;
        }

        private byte[] GetPixels(WordGeo wordGeo, int width, int height)
        {
            var bm = new RenderTargetBitmap(width, height, 96, 96, _pixelFormat);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Purple, _pen, wordGeo.Geo);
            }
            bm.Render(dv);

            var bitmap = new WriteableBitmap(bm);
            var bitmapStride = (bitmap.PixelWidth * _pixelFormat.BitsPerPixel + 7) / 8;
            var newBytes = new byte[bitmap.PixelHeight * bitmapStride];
            bitmap.CopyPixels(newBytes, bitmapStride, 0);

            return newBytes;
        }

        private void UpdateCollisionMap(IReadOnlyList<byte> newBytes, WordGeo newWord)
        {
            if (newWord.IntBottom > _collisionMapHeight ||
                newWord.IntRight > _collisionMapWidth ||
                newWord.IntRight < 0 ||
                newWord.IntBottom < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newBytes));
            }

            var srcWidth = _collisionMapWidth;
            var testX = newWord.IntX;
            var testY = newWord.IntY;
            var testWidth = newWord.IntWidth;
            var testHeight = newWord.IntHeight;

            for (var line = 0; line < testHeight; ++line)
            {
                var mapPosition = (testY + line) * srcWidth + testX;
                var testOffset = line * testWidth * Pbgra32Bytes;

                for (var i = 0; i < testWidth * Pbgra32Bytes; i += Pbgra32Bytes)
                {
                    if (newBytes[testOffset + i] > 0) AddNewCollisionPoint(mapPosition + i / Pbgra32Bytes);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOutOfBounds(WordGeo wordGeo)
        {
            return wordGeo.IntRight > _collisionMapWidth || wordGeo.IntBottom > _collisionMapHeight || wordGeo.X < 0 || wordGeo.Y < 0;
        }

        public bool AddWordGeometry(WordGeo wordGeo)
        {
            if (_collisionMap == null)
            {
                CreateCollisionMap(wordGeo);
                return true;
            }

            var newBytes = GetPixels(wordGeo, wordGeo.IntWidth, wordGeo.IntHeight);
            SetStartingPosition(wordGeo, DefaultStartingPosition);

            var attempts = 0;
            while (HasCollision(newBytes, wordGeo))
            {
                if (!CalculateNextStartingPoint(wordGeo) || IsOutOfBounds(wordGeo))
                {
                    if (attempts > RepositionAttempts)
                    {
                        FailedPlacements++;
                        return false;
                    }
                    DefaultStartingPosition = StartPosition.Random;
                    SetStartingPosition(wordGeo, StartPosition.Random);
                    attempts++;
                }
            }

            if (DefaultStartingPosition == StartPosition.Random)
                AdjustFinalPosition(newBytes, wordGeo);

            UpdateCollisionMap(newBytes, wordGeo);

            return true;
        }
    }
}