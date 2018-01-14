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
        private StartPosition _defaultStartingPosition = StartPosition.Center;

        private readonly PixelFormat _pixelFormat = PixelFormats.Pbgra32;
        private readonly IRandomizer _randomizer;
        private readonly IPositioner _positioner;

        private const int RepositionAttempts = 4;
        private const int Pbgra32Bytes = 4;
        private const int Pbgra32Alpha = 0;
        public double Width { get; }
        public double Height { get; }
        public Point CloudCenter;

        private BitArray _collisionMap;
        private int _collisionMapWidth;

        private int _collisionMapHeight;
        private readonly Pen _pen;
        public int FailedPlacements { get; private set; }

        private const int Buffer = 2;

        public CloudSpace(double width, double height)
        {
            Width = width;
            Height = height;
            CloudCenter = new Point(width / 2, height / 2);
            _positioner = new SpiralPositioner(new Size(Width, Height));
            _pen = new Pen(Brushes.Purple, 1);
            _pen.Freeze();
        }

        public CloudSpace(double width, double height, IRandomizer randomizer = null) : this(width, height)
        {
            _randomizer = randomizer ?? new CryptoRandomizer();
            _positioner = new SpiralPositioner(new Size(Width, Height));
        }

        private bool CalculateNextStartingPoint(WordDrawing wordDrawing)
        {
            if (!_positioner.GetNextPoint(out var x, out var y)) return false;

            wordDrawing.X = x;
            wordDrawing.Y = y;

            return true;
        }


        private void AdjustFinalPosition(byte[] newBytes, WordDrawing wordDrawing)
        {
            var previousX = wordDrawing.X;
            while (!HasCollision(newBytes, wordDrawing) && Math.Abs(CloudCenter.X - wordDrawing.X) > Buffer + 3)
            {
                previousX = wordDrawing.X;
                wordDrawing.X += wordDrawing.X > CloudCenter.X ? -Buffer - 3 : Buffer + 3;
            }

            wordDrawing.X = previousX;

            var previousY = wordDrawing.Y;
            while (!HasCollision(newBytes, wordDrawing) && Math.Abs(CloudCenter.Y - wordDrawing.Y) > Buffer + 3)
            {
                previousY = wordDrawing.Y;
                wordDrawing.Y += wordDrawing.Y > CloudCenter.Y ? -Buffer - 3 : Buffer + 3;
            }

            wordDrawing.Y = previousY;
        }


        private void CreateCollisionMap(WordDrawing wordDrawing)
        {
            SetStartingPosition(wordDrawing, StartPosition.Center);
            _collisionMapWidth = (int) Width;
            _collisionMapHeight = (int) Height;

            var mainImageBytes = GetPixels(wordDrawing, _collisionMapWidth, _collisionMapHeight);
            var totalPixels = _collisionMapHeight * _collisionMapWidth;

            _collisionMap = new BitArray(totalPixels);

            for (var i = 0; i < totalPixels - Pbgra32Bytes; ++i)
            {
                if (mainImageBytes[i * Pbgra32Bytes + 3] > 0) AddNewCollisionPoint(i);
            }
        }

        public void SetStartingPosition(WordDrawing wordDrawing, StartPosition position)
        {
            _positioner.Delta = wordDrawing.Width / wordDrawing.Height;
            switch (position)
            {
                case StartPosition.Center:
                    _positioner.StartX = CloudCenter.X - wordDrawing.Width / 2;
                    _positioner.StartY = CloudCenter.Y - wordDrawing.Height / 2;
                    break;
                case StartPosition.Random:
                    var quad = _randomizer.RandomInt(4);
                    var xMod = 0.0;
                    var yMod = 0.0;
                    switch (quad)
                    {
                        case 1:
                            yMod = -CloudCenter.Y;
                            break;
                        case 2:
                            xMod = -CloudCenter.X;
                            yMod = -CloudCenter.Y;
                            break;
                        case 3:
                            xMod = -CloudCenter.X;
                            break;
                    }

                    _positioner.StartX = xMod + _randomizer.RandomInt((int) (CloudCenter.X * 1.5), (int) (_collisionMapWidth - wordDrawing.Width));
                    _positioner.StartY = yMod + _randomizer.RandomInt((int) (CloudCenter.Y * 1.5), (int) (_collisionMapHeight - wordDrawing.Height));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }

            CalculateNextStartingPoint(wordDrawing);
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


        private bool HasCollision(IReadOnlyList<byte> newWordBytes, WordDrawing newWord)
        {
            if (IsOutOfBounds(newWord)) return true;

            var srcWidth = _collisionMapWidth;

            var testX = newWord.IntX;
            var testY = newWord.IntY;
            var testWidth = newWord.IntWidth * Pbgra32Bytes;
            var testHeight = newWord.IntHeight;
            var mapPosition = testY * srcWidth + testX;
            var testOffset = 0;
            for (var line = 0; line < testHeight; ++line)
            {
                for (var i = 0; i < testWidth; i += Pbgra32Bytes)
                {
                    var mapIndex = mapPosition + i / Pbgra32Bytes;

                    var isCollisionPoint = newWordBytes[testOffset + i] != Pbgra32Alpha;
                    if (isCollisionPoint && _collisionMap[mapIndex]) return true;
                }

                mapPosition += srcWidth;
                testOffset += testWidth;
            }

            return false;
        }

        private byte[] GetPixels(WordDrawing wordDrawing, int width, int height)
        {
            var bm = new RenderTargetBitmap(width, height, 96, 96, _pixelFormat);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(Brushes.Purple, _pen, wordDrawing.Geo);
            }

            bm.Render(dv);

            var bitmapStride = (bm.PixelWidth * _pixelFormat.BitsPerPixel + 7) / 8;
            var newBytes = new byte[bm.PixelHeight * bitmapStride];
            bm.CopyPixels(newBytes, bitmapStride, 0);

            return newBytes;
        }

        private void UpdateCollisionMap(IReadOnlyList<byte> newBytes, WordDrawing newWord)
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
            var testWidth = newWord.IntWidth * Pbgra32Bytes;
            var testHeight = newWord.IntHeight;
            var mapPosition = testY * srcWidth + testX;
            var testOffset = 0;
            for (var line = 0; line < testHeight; ++line)
            {
                for (var i = 0; i < testWidth; i += Pbgra32Bytes)
                {
                    if (newBytes[testOffset + i] > 0) AddNewCollisionPoint(mapPosition + i / Pbgra32Bytes);
                }

                mapPosition += srcWidth;
                testOffset += testWidth;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOutOfBounds(WordDrawing wordDrawing)
        {
            return wordDrawing.IntRight >= _collisionMapWidth - Buffer - 2 || wordDrawing.IntBottom >= _collisionMapHeight - Buffer - 2 || wordDrawing.X < 0 || wordDrawing.Y < 0;
        }

        public bool AddWordGeometry(WordDrawing wordDrawing)
        {
            if (_collisionMap == null)
            {
                CreateCollisionMap(wordDrawing);
                return true;
            }

            var newBytes = GetPixels(wordDrawing, wordDrawing.IntWidth, wordDrawing.IntHeight);
            SetStartingPosition(wordDrawing, _defaultStartingPosition);

            var attempts = 0;
            while (HasCollision(newBytes, wordDrawing))
            {
                do
                {
                    var result = CalculateNextStartingPoint(wordDrawing);
                    if (!result && attempts > RepositionAttempts)
                    {
                        FailedPlacements++;
                        return false;
                    }

                    if (result) continue;
                    _defaultStartingPosition = StartPosition.Random;
                    SetStartingPosition(wordDrawing, StartPosition.Random);
                    attempts++;
                } while (IsOutOfBounds(wordDrawing));
            }

            if (_defaultStartingPosition == StartPosition.Random)
                AdjustFinalPosition(newBytes, wordDrawing);

            UpdateCollisionMap(newBytes, wordDrawing);

            return true;
        }
    }

    internal enum StartPosition
    {
        Center,
        Random,
    }
}