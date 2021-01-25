using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JsonRpcLite.Services;

namespace JsonRpcLite.Network
{
    internal class SmdHandler:ISmdHandler
    {
        /// <summary>
        /// Dispatch request to smd .
        /// </summary>
        /// <param name="service">The service to handle the context</param>
        /// <param name="context">The context for communication</param>
        /// <returns>Void</returns>
        public async Task HandleAsync(JsonRpcService service, object context)
        {
            if (context is HttpListenerContext httpListenerContext)
            {
                httpListenerContext.Response.AddHeader("Server", "JsonRpcLite");
                httpListenerContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                httpListenerContext.Response.ContentEncoding = Encoding.UTF8;
                httpListenerContext.Response.ContentType = "application/json";
                httpListenerContext.Response.StatusCode = (int) HttpStatusCode.OK;
                httpListenerContext.Response.ContentLength64 = service.SmdData.Length;
                await httpListenerContext.Response.OutputStream.WriteAsync(service.SmdData).ConfigureAwait(false);
                httpListenerContext.Response.Close();
            }
            else
            {
                throw new InvalidOperationException("The context is not a HttpListenerContext");
            }
        }
    }
}
