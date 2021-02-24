using System;

namespace embeddedAFL
{
    internal static class StaticHelper
    {
        public static RandomBufferGenerator Generator = new RandomBufferGenerator(65537);

        public static int ConvertLittleEndian(byte[] array)
        {
            int pos = 0;
            int result = 0;
            foreach (byte by in array)
            {
                result |= @by << pos;
                pos += 8;
            }
            return result;
        }

        public class RandomBufferGenerator
        {
            private readonly Random _random = new Random();
            private readonly byte[] _seedBuffer;

            public RandomBufferGenerator(int maxBufferSize)
            {
                _seedBuffer = new byte[maxBufferSize];

                _random.NextBytes(_seedBuffer);
            }

            public byte[] GenerateBufferFromSeed(int size)
            {
                int randomWindow = _random.Next(0, size);

                byte[] buffer = new byte[size];

                Buffer.BlockCopy(_seedBuffer, randomWindow, buffer, 0, size - randomWindow);
                Buffer.BlockCopy(_seedBuffer, 0, buffer, size - randomWindow, randomWindow);

                return buffer;
            }
        }
    }
}
