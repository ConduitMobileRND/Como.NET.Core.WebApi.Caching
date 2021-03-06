using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Como.WebApi.Caching
{
    /// <summary>
    ///     A <see cref="ActionResult" /> that writes any given payload to the response body output stream
    ///     without using output formatters.
    /// </summary>
    public class RawActionResult : ActionResult
    {
        public const string JsonContentType = "application/json; charset=utf-8";
        private readonly string _contentType;

        private readonly IDictionary<string, string> _headers;
        private readonly byte[] _payload;
        private readonly int _statusCode;

        public RawActionResult(int statusCode, byte[] payload, string contentType,
            IDictionary<string, string> headers = null)
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

            if (_payload?.Length > 0)
            {
                response.StatusCode = _statusCode;
                await response.Body.WriteAsync(_payload, 0, _payload.Length);
            }
            else
            {
                response.StatusCode =
                    _statusCode == StatusCodes.Status200OK
                        ? StatusCodes.Status204NoContent
                        : _statusCode;
            }
        }
    }
}