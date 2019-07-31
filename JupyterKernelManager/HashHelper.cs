using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JupyterKernelManager
{
    public class HashHelper
    {
        private const int MIN_LENGTH = 16;
        private const int DEFAULT_LENGTH = 16;
        private const int MAX_LENGTH = 4096;

        /// <summary>
        /// Random number generator, so we aren't recreating one each time it's needed.
        /// </summary>
        private readonly RNGCryptoServiceProvider RandomGenerator = new RNGCryptoServiceProvider();

        /// <summary>
        /// Generate a new random id.
        /// </summary>
        /// <returns>id string (16 random bytes as hex-encoded text, chunks separated by '-')</returns>
        public string NewId(bool includeDelimiter = true, int length = DEFAULT_LENGTH)
        {
            if (length < MIN_LENGTH || length > MAX_LENGTH)
            {
                throw new ArgumentOutOfRangeException(string.Format("Key length was {0} - it must be between {1} and {2} bytes in length",
                    length, MIN_LENGTH, MAX_LENGTH));
            }

            var rand = new byte[length];
            RandomGenerator.GetBytes(rand);
            // Convert the bytes to a 2 character hex representation.
            var randString = BitConverter.ToString(rand).Replace("-", string.Empty);
            // This mimics the format Jupyter uses, instead of the built-in UUID generator
            return string.Format("{0}{1}{2}",
                randString.Substring(0, 8),
                (includeDelimiter ? "-" : ""),
                randString.Substring(8));
        }

        /// <summary>
        /// Return a new ID as ascii bytes
        /// </summary>
        /// <returns></returns>
        public byte[] NewIdBytes(bool includeDelimiter = true, int length = DEFAULT_LENGTH)
        {
            // We know that NewId creates a hex string which is 2x as long as the input, so because
            // of the conversions we do we need to halve the length when it goes in.
            return Encoding.ASCII.GetBytes(NewId(includeDelimiter, (length / 2)));
        }
    }
}
