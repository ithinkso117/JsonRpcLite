using System;
using System.Collections.Generic;
using JsonRpcLite.Services;

namespace JsonRpcLite.Utilities
{
    internal class JsonRpcResultData
    {
        private readonly Dictionary<string, object> _data = new(){ { "jsonrpc", "2.0" } };

        /// <summary>
        /// Gets or sets the id of the data.
        /// </summary>
        public object Id
        {
            get
            {
                if (_data.ContainsKey("id"))
                {
                    return _data["id"];
                }

                return null;
            }
            set
            {
                if (value != null)
                {
                    _data["id"] = value;
                }
                else
                {
                    _data.Remove("id");
                }
            }
        }


        public object Data
        {
            get
            {
                if (_data.ContainsKey("result"))
                {
                    return _data["result"];
                }

                return _data["error"];
            }
            set
            {
                if (value is Exception exception)
                {
                    _data.Remove("result");
                    if (value is RpcException rpcException)
                    {
                        _data["error"] = new Dictionary<string, object> {{"code", rpcException.ErrorCode}, {"message", rpcException.Message}};
                    }
                    else
                    {
                        _data["error"] = new Dictionary<string, object> { { "code", ServerErrorException.DefaultServerErrorCode }, { "message", exception.Message } };
                    }
                }
                else
                {
                    _data.Remove("error");
                    if (value == null)
                    {
                        _data.Remove("result");
                    }
                    else
                    {
                        _data["result"] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Get the internal data of the result.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetData()
        {
            return _data;
        }
    }
}
