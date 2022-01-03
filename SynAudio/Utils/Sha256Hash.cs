using System;

namespace SynAudio.Utils
{
    public static class Sha256Hash
    {
        /// <summary>
        /// Computes SHA256 hash from object using System.Text.Json.JsonSerializer.
        /// </summary>
        /// <param name="o">The object to compute the hash from.</param>
        /// <returns>64 characters</returns>
        public static string FromObject(object o)
        {
            byte[] serialized = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(o);
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(serialized)).Replace("-", string.Empty);
        }

        /// <summary>
        /// Computes SHA256 hash from byte array.
        /// </summary>
        /// <param name="bytes">The bytes to compute the hash from.</param>
        /// <returns>64 characters</returns>
        public static string FromByteArray(byte[] bytes)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty);
        }
    }
}
