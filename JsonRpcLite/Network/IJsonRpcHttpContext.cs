using System.IO;

namespace JsonRpcLite.Network
{
    public interface IJsonRpcHttpContext
    {
        /// <summary>
        /// Set the header into the response.
        /// </summary>
        /// <param name="name">The name of the header row.</param>
        /// <param name="value">The value of the header row.</param>
        void SetResponseHeader(string name, string value);


        /// <summary>
        /// Gets header value by header name.
        /// </summary>
        /// <param name="name">The name of the header row.</param>
        /// <returns>The header value.</returns>
        string GetRequestHeader(string name);

        /// <summary>
        /// Get the output stream from the response.
        /// </summary>
        /// <returns>The output stream.</returns>
         Stream GetOutputStream();

        /// <summary>
        /// Get the input stream from the request.
        /// </summary>
        /// <returns>The input stream</returns>
         Stream GetInputStream();

        /// <summary>
        /// Get the body length from the request.
        /// </summary>
        /// <returns>The request body length</returns>
        long GetRequestContentLength();


        /// <summary>
        /// Set the content length of the response.
        /// </summary>
        /// <param name="contentLength">The content length</param>
        void SetResponseContentLength(long contentLength);


        /// <summary>
        /// Set the status code of the response.
        /// </summary>
        /// <param name="statusCode">The status code to set.</param>
        void SetResponseStatusCode(int statusCode);

        /// <summary>
        /// Set the content type of the response.
        /// </summary>
        /// <param name="contentType">The content type to set.</param>
        void SetResponseContentType(string contentType);

        /// <summary>
        /// Get the http method from the request.
        /// </summary>
        /// <returns>The http method with lower case.</returns>
        string GetRequestHttpMethod();

        /// <summary>
        /// Get the request path from the request.
        /// </summary>
        /// <returns>The call path from the request.</returns>
        string GetRequestPath();

        /// <summary>
        /// Close the context.
        /// </summary>
        void Close();
    }
}
