using System.IO;
using JsonRpcLite.Network;
using Microsoft.AspNetCore.Http;

namespace JsonRpcLite.Kestrel
{
    internal class JsonRpcKestrelHttpContext:IJsonRpcHttpContext
    {
        private readonly HttpContext _context;

        public JsonRpcKestrelHttpContext(HttpContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Set the header into the response.
        /// </summary>
        /// <param name="name">The name of the header row.</param>
        /// <param name="value">The value of the header row.</param>
        public void SetResponseHeader(string name, string value)
        {
            _context.Response.Headers.Add(name, value);
        }


        /// <summary>
        /// Gets header value by header name.
        /// </summary>
        /// <param name="name">The name of the header row.</param>
        /// <returns>The header value.</returns>
        public string GetRequestHeader(string name)
        {
            return _context.Request.Headers[name];
        }

        /// <summary>
        /// Get the output stream from the response.
        /// </summary>
        /// <returns>The output stream.</returns>
        public Stream GetOutputStream()
        {
            return _context.Response.Body;
        }


        /// <summary>
        /// Get the input stream from the request.
        /// </summary>
        /// <returns>The input stream</returns>
        public Stream GetInputStream()
        {
            return _context.Request.Body;
        }


        /// <summary>
        /// Get the body length from the request.
        /// </summary>
        /// <returns>The request body length</returns>
        public long GetRequestContentLength()
        {
            return _context.Request.ContentLength?? 0;
        }

        /// <summary>
        /// Set the content length of the response.
        /// </summary>
        /// <param name="contentLength">The content length</param>
        public void SetResponseContentLength(long contentLength)
        {
            _context.Response.ContentLength = contentLength;
        }


        /// <summary>
        /// Set the status code of the response.
        /// </summary>
        /// <param name="statusCode">The status code to set.</param>
        public void SetResponseStatusCode(int statusCode)
        {
            _context.Response.StatusCode = statusCode;
        }


        /// <summary>
        /// Set the content type of the response.
        /// </summary>
        /// <param name="contentType">The content type to set.</param>
        public void SetResponseContentType(string contentType)
        {
            _context.Response.ContentType = contentType;
        }


        /// <summary>
        /// Get the http method from the request.
        /// </summary>
        /// <returns>The http method with lower case.</returns>
        public string GetRequestHttpMethod()
        {
            return _context.Request.Method.ToLower();
        }


        /// <summary>
        /// Get the request path from the request.
        /// </summary>
        /// <returns>The call path from the request.</returns>
        public string GetRequestPath()
        {
            return _context.Request.Path;
        }

        /// <summary>
        /// Close the context.
        /// </summary>
        public void Close()
        { 
            //No need close for Kestrel server;
        }
    }
}
