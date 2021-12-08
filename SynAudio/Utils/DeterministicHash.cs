using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SynAudio.Utils
{
    /// <summary>
    /// Provides deterministic hash functions. The .NET Core and newer implementations of .NET do generate randomized hash values between applications runs.
    /// Default GetHashCode behaviour: The hash code itself is not guaranteed to be stable. Hash codes for identical strings can differ across .NET implementations, across .NET versions, and across .NET platforms (such as 32-bit and 64-bit) for a single version of .NET. In some cases, they can even differ by application domain. This implies that two subsequent runs of the same program may return different hash codes.
    /// https://docs.microsoft.com/en-us/dotnet/api/system.string.gethashcode?view=net-6.0
    /// </summary>
    public static class DeterministicHash
    {
        public static string HashByteArray(byte[] data)
        {
            byte[] hashValue;
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                hashValue = sha1.ComputeHash(data);
            }
            string hexFormat = string.Concat(hashValue.Select(x => x.ToString("X2")));
            return hexFormat;
        }

        public static string HashObject(object o)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o));
            return HashByteArray(bytes);
        }
    }
}
