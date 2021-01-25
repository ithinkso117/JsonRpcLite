﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JsonRpcLite.InProcess;
using JsonRpcLite.Log;
using JsonRpcLite.Network;

namespace TestServer
{
    class Program
    {
        private static readonly string[] TestData = {
            "{\"jsonrpc\": \"2.0\", \"method\":\"add\",\"params\":[1,2],\"id\":1}",
            "{\"jsonrpc\": \"2.0\", \"method\":\"addInt\",\"params\":[1,7],\"id\":2}",
            "{\"jsonrpc\": \"2.0\", \"method\":\"NullableFloatToNullableFloat\",\"params\":[1.23],\"id\":3}",
            "{\"jsonrpc\": \"2.0\", \"method\":\"Test2\",\"params\":[3.456],\"id\":4}",
            "{\"jsonrpc\": \"2.0\", \"method\":\"StringMe\",\"params\":[\"Foo\"],\"id\":5}"
        };
        private static JsonRpcInProcessServer _localServer;

        private static JsonRpcHttpServer _httpServer;

        static void Main(string[] args)
        {
            if(args.Contains("-debug"))
            {
                Logger.DebugMode = true;
                Logger.UseDefaultWriter();
            }

            if (args.Contains("-benchmark"))
            {
                
                for (var i = 0; i < 20; i++)
                {
                    _localServer = new JsonRpcInProcessServer();
                    Benchmark(TestData);
                    Console.WriteLine();
                }
            }
            else
            {
                _httpServer = new JsonRpcHttpServer();
                _httpServer.Start(8090);
                Console.WriteLine("JsonRpc HttpServer Started.");
            }
            Console.ReadLine();
        }


        public static Task Process(string requestStr)
        {
            return Task.Factory.StartNew(() =>
            {
                var task = _localServer.BenchmarkProcessAsync("test", "v1", requestStr);
                task.Wait();
                task.Dispose();
            });
        }

        private static void Benchmark(string[] testData)
        {
            Console.WriteLine("Starting benchmark");
            var count = 50;
            var iterations = 7;
            for (int iteration = 1; iteration <= iterations; iteration++)
            {
                count *= iteration;
                var tasks = new Task[count];
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < count; i += 5)
                {
                    tasks[i] = Process(testData[0]);
                    tasks[i + 1] = Process(testData[1]);
                    tasks[i + 2] = Process(testData[2]);
                    tasks[i + 3] = Process(testData[3]);
                    tasks[i + 4] = Process(testData[4]);
                }
                Task.WaitAll(tasks);
                foreach (var task in tasks)
                {
                    task.Dispose();
                }
                sw.Stop();
                Console.WriteLine("processed {0:N0} rpc in \t {1:N0}ms for \t {2:N} rpc/sec", count, sw.ElapsedMilliseconds, count * 1000d / sw.ElapsedMilliseconds);
            }


            Console.WriteLine("Finished benchmark...");
        }
    }
}
