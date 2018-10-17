using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Como.WebApi.Caching
{
    public static class JsonOutputFormatterExtensionMethods
    {
        public static string ComputeHash(this JsonOutputFormatter formatter, object target)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    formatter.WriteObject(writer, target);
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