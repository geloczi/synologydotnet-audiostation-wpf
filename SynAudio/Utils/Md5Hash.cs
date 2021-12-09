using System;

namespace SynAudio.Utils
{
    public static class Md5Hash
    {
        /// <summary>
        /// Computes MD5 hash from object using System.Text.Json.JsonSerializer.
        /// </summary>
        /// <param name="o">The object to compute the hash from.</param>
        /// <returns>32 character long MD5 string</returns>
        public static string FromObject(object o)
        {
            byte[] serialized = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(o);
            using (var md5 = System.Security.Cryptography.MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(serialized)).Replace("-", string.Empty);
        }

        /// <summary>
        /// Computes MD5 hash from byte array.
        /// </summary>
        /// <param name="bytes">The bytes to compute the hash from.</param>
        /// <returns>32 character long MD5 string</returns>
        public static string FromByteArray(byte[] bytes)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", string.Empty);
        }
    }
}
