using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Como.WebApi.Caching
{
    public static class HashExtensionMethods
    {
        private static readonly JsonSerializer HashSerializer = new JsonSerializer();

        public static string ComputeHash(this object target)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        HashSerializer.Serialize(jsonWriter, target);
                    }
                }
                return stream.ToArray().ComputeSha1Hash();
            }
        }

        private static string ComputeSha1Hash(this byte[] input)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(input);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}