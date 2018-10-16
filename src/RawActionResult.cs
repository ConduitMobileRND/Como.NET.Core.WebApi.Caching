using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Como.WebApi.Caching
{
    /// <summary>
    ///     A special <see cref="ActionResult" /> that writes a pre-serialized payload to the response output stream
    ///     in order to avoid unnecessary object deserialization &amp; serialization in cases we have a JSON string in hand 
    /// </summary>
    public class RawActionResult : ActionResult
    {
        public const string JsonContentType = "application/json; charset=utf-8";
        
        private readonly IDictionary<string, string> _headers;
        private readonly byte[] _payload;
        private readonly string _contentType;
        private readonly int _statusCode;

        public RawActionResult(int statusCode, byte[] payload, string contentType, IDictionary<string, string> headers = null)
        {
            _statusCode = statusCode;
            _payload = payload;
            _contentType = contentType;
            _headers = headers;
        }

        /// <inheritdoc />
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var response = context.HttpContext.Response;
            response.ContentType = _contentType;
            if (_headers != null)
            {
                foreach (var header in _headers)
                {
                    response.Headers[header.Key] = header.Value;
                }
            }

            response.StatusCode = _statusCode;
            if (_payload?.Length > 0)
            {
                await response.Body.WriteAsync(_payload, 0, _payload.Length);
            }
        }
    }
}