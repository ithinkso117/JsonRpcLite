﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Network;
using JsonRpcLite.Rpc;

namespace TestClient
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

        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(65535, 65535);
            var remoteUrl = "http://127.0.0.1:8090/";
            if (args.Length != 0 && (args[0].StartsWith("http") || args[0].StartsWith("ws")))
            {
                remoteUrl = args[0];
            }
            var client = new JsonRpcClient();
            IJsonRpcClientEngine clientEngine = remoteUrl.StartsWith("http")? 
                new JsonRpcHttpClientEngine(remoteUrl):
                new JsonRpcWebSocketClientEngine(remoteUrl);

            bool ws = clientEngine is JsonRpcWebSocketClientEngine;

            client.UseEngine(clientEngine);

            var testCount = 3;
            if (args.Contains("-benchmark"))
            {
                testCount = 100;
            }
            var statisticsList = new List<int>();
            for (var i = 0; i < testCount; i++)
            {
                statisticsList.Add(Benchmark(client, TestData, ws));
                Console.WriteLine();
            }
            Console.WriteLine();
            Console.WriteLine($"Best: {statisticsList.Max()} rpc/sec, \t Average: {(int)statisticsList.Average()} rpc/sec, \t Worst: {statisticsList.Min()} rpc/sec");
            Console.ReadLine();
        }

        public static Task Process(JsonRpcClient client, string requestStr)
        {
            return Task.Factory.StartNew(() =>
            {
                var task = client.BenchmarkAsync("test", requestStr);
                task.Wait();
                task.Dispose();
            });
        }

        private static int Benchmark(JsonRpcClient client, string[] testData, bool ws)
        {
            var statisticsList = new List<double>();
            Console.WriteLine("Starting benchmark");
            var count = 50;
            var iterations = 7;
            for (int iteration = 1; iteration <= iterations; iteration++)
            {
                count *= iteration;
                var tasks = new Task[count];
                var sw = Stopwatch.StartNew();

                if (!ws)
                {
                    for (int i = 0; i < count; i += 5)
                    {
                        tasks[i] = Process(client, testData[0]);
                        tasks[i + 1] = Process(client, testData[1]);
                        tasks[i + 2] = Process(client, testData[2]);
                        tasks[i + 3] = Process(client, testData[3]);
                        tasks[i + 4] = Process(client, testData[4]);
                    }

                    Task.WaitAll(tasks);
                    foreach (var task in tasks)
                    {
                        task.Dispose();
                    }
                }
                else
                {
                    count = 10;
                    tasks = new Task[count];
                    for (int i = 0; i < count; i += 5)
                    {
                       tasks[i] = Process(client, testData[0]);
                       tasks[i+1] = Process(client, testData[1]);
                       tasks[i+2] = Process(client, testData[2]);
                       tasks[i+3] = Process(client, testData[3]);
                       tasks[i+4] = Process(client, testData[4]);
                    }
                    Task.WaitAll(tasks);
                    foreach (var task in tasks)
                    {
                        task.Dispose();
                    }
                }

                sw.Stop();
                if (sw.ElapsedMilliseconds != 0)
                {
                    var statistics = count * 1000d / sw.ElapsedMilliseconds;
                    statisticsList.Add(statistics);
                }
                Console.WriteLine("processed {0:N0} rpc in \t {1:N0}ms for \t {2:N} rpc/sec", count, sw.ElapsedMilliseconds, count * 1000d / sw.ElapsedMilliseconds);
            }
            Console.WriteLine("Finished benchmark...");
            return (int)statisticsList.Average();
        }
    }
}
