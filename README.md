# JsonRpcLite

![JsonRpcLite](https://github.com/ithinkso117/JsonRpcLite/workflows/JsonRpcLite/badge.svg)

This is a simple JsonRpc server implementation written in C#, it's simple, but fast!

## Features
- [x] High performance, faster than JSON-RPC.NET.
- [x] Lightweight design, only one dll file.
- [x] Build-in http server, support http and in-process.
- [x] Attributes support, support customize service name and method name.
- [x] SMD support.
- [ ] Code generator for generate client code(C#, dart, javascript...).
- [ ] Interface based mode for C# development.

## Performance

Here we have a benchmark compare with JSON-RPC.NET.
##### i7-7700 @ 3.6GHz 64.0 GB

##### JsonRpcLite
```dos
Starting benchmark
processed 50 rpc in      0ms for         ∞ rpc/sec
processed 100 rpc in     0ms for         ∞ rpc/sec
processed 300 rpc in     0ms for         ∞ rpc/sec
processed 1,200 rpc in   2ms for         600,000.00 rpc/sec
processed 6,000 rpc in   12ms for        500,000.00 rpc/sec
processed 36,000 rpc in          77ms for        467,532.47 rpc/sec
processed 252,000 rpc in         581ms for       433,734.94 rpc/sec
Finished benchmark...
```

##### JSON-RPC.NET
```dos
Starting benchmark
processed 50 rpc in      0ms for         ∞ rpc/sec
processed 100 rpc in     0ms for         ∞ rpc/sec
processed 300 rpc in     0ms for         ∞ rpc/sec
processed 1,200 rpc in   2ms for         600,000.00 rpc/sec
processed 6,000 rpc in   28ms for        214,285.71 rpc/sec
processed 36,000 rpc in          88ms for        409,090.91 rpc/sec
processed 252,000 rpc in         640ms for       393,750.00 rpc/sec
Finished benchmark...
```

#### Usage
## For server
```csharp
    public interface ITestService
    {
        public string Hello(string name);

        public int Add(int a, int b);
    }
    
    public class TestService:ITestService
    {
        public string Hello(string name)
        { 
          ...
        }

        public int Add(int a, int b)
        {
          ...
        }
    }

   //In server
   var server = new JsonRpcServer();
   server.RegisterService<ITestService>(new TestService());
   var serverEngine = new JsonRpcHttpServerEngine("http://127.0.0.1:8090/");
   server.UseEngine(serverEngine);
   server.Start();
```

## For clinet
```csharp
  //In client
  var client = new JsonRpcClient();
  var clientEngine = new JsonRpcHttpClientEngine("http://127.0.0.1:8090/");
  client.UseEngine(clientEngine);
  var proxy = client.CreateProxy<ITestService>("TestService");
  Console.WriteLine(proxy.Hello("World"));
```


