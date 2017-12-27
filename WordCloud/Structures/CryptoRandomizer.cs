using System;
using System.Security.Cryptography;

namespace WordCloud.Structures
{
    internal class CryptoRandomizer : IRandomizer
    {
        int IRandomizer.RandomInt(int max)
        {
            // Create a byte array to hold the random value.
            var byteArray = new byte[4];

            using (var gen = new RNGCryptoServiceProvider())
            {
                gen.GetBytes(byteArray);
                return Math.Abs(BitConverter.ToInt32(byteArray, 0) % max);
            }
        }

        int IRandomizer.RandomInt(int min, int max)
        {
            if (max == min) return 0;
            // Create a byte array to hold the random value.
            var byteArray = new byte[4];

            using (var gen = new RNGCryptoServiceProvider())
            {
                gen.GetBytes(byteArray);
                return Math.Abs(BitConverter.ToInt32(byteArray, 0) % (max - min) + min);
            }
        }
    }
}