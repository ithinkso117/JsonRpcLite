using JsonRpcLite.Rpc;

namespace JsonRpcLite.InProcess
{
    public class JsonRpcInProcessServerEngine:JsonRpcServerEngine
    {
        public JsonRpcInProcessServerEngine()
        {
            Name = nameof(JsonRpcInProcessServerEngine);
        }
    }
}
