using System.Collections.Generic;

namespace JsonRpcLite.Utilities
{
    internal class JsonRpcRequestData
    {
        private readonly Dictionary<string, object> _data = new() { { "jsonrpc", "2.0" } };

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


        /// <summary>
        /// Gets or sets the name of the method.
        /// </summary>
        public string Method
        {
            get
            {
                if (_data.ContainsKey("method"))
                {
                    return (string)_data["method"];
                }

                return null;
            }
            set
            {
                if (value != null)
                {
                    _data["method"] = value;
                }
                else
                {
                    _data.Remove("method");
                }
            }
        }


        /// <summary>
        /// Gets or sets the param of the method.
        /// </summary>
        public object Params
        {
            get
            {
                if (_data.ContainsKey("params"))
                {
                    return _data["params"];
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    _data.Remove("params");
                }
                else
                {
                    _data["params"] = value;
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
