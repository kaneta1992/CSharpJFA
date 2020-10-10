using System;

namespace JFA
{
    class Buffer<T>
    {
        public int Width { get; set; }
        public int Height { get; set; }

        private T[] _values;
        private T _outsideValue = default;

        public Buffer(int width, int height, T outsideValue = default)
        {
            Width = width;
            Height = height;
            _values = new T[Width * Height];
            _outsideValue = outsideValue;
        }

        public bool Contains(int x, int y)
        {
            return !(x < 0 || x >= Width || y < 0 || y >= Height);
        }

        public int GetIndex(int x, int y)
        {
            if (!Contains(x, y)) return -1;
            return y * Width + x;
        }

        public void Copy(T[] values)
        {
            Array.Copy(values, _values, _values.Length);
        }

        public T this [int x, int y]
        {
            get
            {
                if (!Contains(x, y)) return _outsideValue;
                return _values[GetIndex(x, y)];
            }
            set
            {
                _values[GetIndex(x, y)] = value;
            }
        }
    }
    class SwapBuffer<T>
    {
        public int Width => _buffers[0].Width;
        public int Height => _buffers[0].Height;

        private Buffer<T>[] _buffers;
        private int _activeIndex;

        public SwapBuffer(int width, int height, T outsideValue = default)
        {
            _buffers = new Buffer<T>[2];
            _buffers[0] = new Buffer<T>(width, height, outsideValue);
            _buffers[1] = new Buffer<T>(width, height, outsideValue);
            _activeIndex = 0;
        }
        public void Swap()
        {
            _activeIndex = (_activeIndex + 1) % 2;
        }
        public void Copy(T[] values)
        {
            _buffers[_activeIndex].Copy(values);
        }
        public bool Contains(int x, int y)
        {
            return _buffers[0].Contains(x, y);
        }

        public T this[int x, int y]
        {
            get
            {
                return _buffers[(_activeIndex + 1) % 2][x, y];
            }
            set
            {
                _buffers[_activeIndex][x, y] = value;
            }
        }
    }
    class JFA<T>
    {
        class NearestPointInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
        private SwapBuffer<NearestPointInfo> _nearestPoints;
        private Buffer<T> _buffer;

        public JFA(int width, int height, T[] buffer, T outsideValue = default)
        {
            _buffer = new Buffer<T>(width, height, outsideValue);
            _buffer.Copy(buffer);

            _nearestPoints = new SwapBuffer<NearestPointInfo>(width, height);
        }

        private float length(int x, int y)
        {
            return (float)Math.Sqrt(length2(x, y));
        }

        private float length2(int x, int y)
        {
            return x * x + y * y;
        }

        public void searchNearestPixelInLevel(int x, int y, int level, Predicate<T> insideFunction)
        {
            var step = (int)Math.Ceiling(Math.Pow(2, level));
            var minDistance = 999999.9f;
            var currentPixel = _buffer[x, y];
            for (var nx = -1; nx <= 1; nx++)
            {
                for (var ny = -1; ny <= 1; ny++)
                {
                    var sampleX = x + nx * step;
                    var sampleY = y + ny * step;
                    var samplePixel = _buffer[sampleX, sampleY];
                    var nearestPointOfSamplePixel = _nearestPoints[sampleX, sampleY];

                    int nearestPixelX, nearestPixelY;
                    if (insideFunction(currentPixel) != insideFunction(samplePixel))
                    {
                        nearestPixelX = sampleX;
                        nearestPixelY = sampleY;
                    }
                    else if (nearestPointOfSamplePixel != null)
                    {
                        nearestPixelX = nearestPointOfSamplePixel.X;
                        nearestPixelY = nearestPointOfSamplePixel.Y;
                    }
                    else
                    {
                        continue;
                    }
                    var sampleDistance = length2(nearestPixelX - x, nearestPixelY - y);
                    if (sampleDistance < minDistance)
                    {
                        minDistance = sampleDistance;
                        _nearestPoints[x, y] = new NearestPointInfo
                        {
                            X = nearestPixelX,
                            Y = nearestPixelY,
                        };
                    }
                }
            }
        }

        public T[] Execute(Predicate<T> insideFunction)
        {
            var maxLevel = Math.Max(_buffer.Width, _buffer.Height);
            maxLevel = (int)Math.Ceiling(Math.Log2(maxLevel));
            for (var i = maxLevel; i >= 0; i--)
            {
                for (var y = 0; y < _buffer.Height; y++)
                {
                    for (var x = 0; x < _buffer.Width; x++)
                    {
                        searchNearestPixelInLevel(x, y, i, insideFunction);
                    }
                }
                _nearestPoints.Swap();
            }

            var result = new T[_buffer.Width * _buffer.Height];
            for (var y = 0; y < _buffer.Height; y++)
            {
                for (var x = 0; x < _buffer.Width; x++)
                {
                    var currentPixel = _buffer[x, y];
                    var nearestPixelOfCurrentPixel = _nearestPoints[x, y];
                    if (nearestPixelOfCurrentPixel == null)
                    {
                        continue;
                    }
                    else if (insideFunction(currentPixel))
                    {
                        result[y * _buffer.Width + x] = currentPixel;
                    }
                    else
                    {
                        var nearestPixel = _buffer[nearestPixelOfCurrentPixel.X, nearestPixelOfCurrentPixel.Y];
                        result[y * _buffer.Width + x] = nearestPixel;
                    }
                }
            }
            return result;
        }
    }
    class Program
    {
        static int w = 32;
        static int h = 32;

        static int index(int x, int y)
        {
            return y * w + x;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var buffer = new int[w * h];
            buffer[index(0, 0)] = 6;
            buffer[index(0, h - 1)] = 2;
            buffer[index(w - 1, h - 1)] = 3;
            buffer[index(w - 1, 0)] = 4;
            buffer[index(0, 15)] = 5;
            var jfa = new JFA<int>(w, h, buffer, -1);
            var result = jfa.Execute(i =>
            {
                return i > 0 || i == -1;
            });

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Console.Write(Math.Abs(result[index(x, y)]) + " ");
                }
                Console.WriteLine("");
            }
        }
    }
}
