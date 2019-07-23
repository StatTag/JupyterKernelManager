// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Linq;
using NetMQ;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;

namespace JupyterKernelManager
{
    public static partial class Extensions
    {

        /// <summary>
        ///      Converts a string containing hexadecimal digits to an array of
        ///      bytes representing the same data.
        /// </summary>
        /// <param name="hex">
        ///     A string containing an even number of hexadecimal characters
        ///     (0-f).
        /// </param>
        /// <returns>An array of bytes representing the same data.</returns>
        public static byte[] HexToBytes(this string hex)
        {
            var bytes = new byte[hex.Length / 2];
            foreach (var idxHexPair in Enumerable.Range(0, hex.Length / 2))
            {
                bytes[idxHexPair] = Convert.ToByte(hex.Substring(2 * idxHexPair, 2), 16);
            }
            return bytes;
        }
    }
}
