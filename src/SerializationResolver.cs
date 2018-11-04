using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Como.WebApi.Caching
{
    public class SerializationResolver : ISerializationResolver
    {
        private readonly JsonOutputFormatter _jsonOutputFormatter;

        public SerializationResolver(IOptions<MvcOptions> mvcOptions)
        {
            _jsonOutputFormatter = mvcOptions.Value.OutputFormatters.OfType<JsonOutputFormatter>().Single();
        }

        private byte[] SerializeObjectToJson(object obj)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(outputStream))
                {
                    _jsonOutputFormatter.WriteObject(streamWriter, obj);
                }

                return outputStream.ToArray();
            }
        }

        public byte[] Serialize(string outputContentType, object value)
        {
            var outputContentTypeLower = outputContentType.ToLower();
            switch (outputContentTypeLower)
            {
                case RawActionResult.JsonContentType:
                case "application/json":
                case "text/json":
                case "application/*+json":
                    return SerializeObjectToJson(value);
                case "text/html":
                case "text/plain":
                    return Encoding.UTF8.GetBytes(value.ToString());
                default:
                    throw new UnsupportedContentTypeException(outputContentType);
            }
        }

        public string ComputeHash(object target)
        {
            var serialized = SerializeObjectToJson(target);
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(serialized);
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