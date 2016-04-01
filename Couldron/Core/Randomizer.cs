﻿using System;
using System.Security.Cryptography;

namespace Couldron.Core
{
    /// <summary>
    /// Provides a randomizer that is cryptographicly secure
    /// </summary>
    public static partial class Randomizer
    {
        private static RNGCryptoServiceProvider cryptoGlobal = new RNGCryptoServiceProvider();

        private static int GetCryptographicSeed()
        {
            byte[] buffer = new byte[4];
            // Fills an array of bytes with a cryptographically strong sequence of random values
            cryptoGlobal.GetBytes(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }
    }
}