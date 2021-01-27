using System;
using JsonRpcLite.Services;

namespace TestServer
{
    public class TestClass1
    {
        public int Id { get; set; }

        public string Name { get; set; }


    }

    public class TestClass2
    {
        public byte[] Data { get; set; }

        public DateTime Time { get; set; }

        public float Id { get; set; }

        public TestClass1 Child { get; set; }

    }

    public class ComplexObject
    {
        public TestClass1 Test1 { get; set; }

        public TestClass2 Test2 { get; set; }
    }


    [RpcService("test")]
    public class CalculatorService : JsonRpcService
    {
        [RpcMethod]
        public ComplexObject[] testIsAllowed(ComplexObject[] cb)
        {
            return cb;
        }

        [RpcMethod]
        public ComplexObject testComplex(ComplexObject cb)
        {
            return cb;
        }


        [RpcMethod]
        public double add(double l, double r)
        {
            return l + r;
        }

        [RpcMethod]
        public int addInt(int l, int r)
        {
            return l + r;
        }

        [RpcMethod]
        public float NullableFloatToNullableFloat(float a)
        {
            return a;
        }

        [RpcMethod]
        public decimal Test2(decimal x)
        {
            return x;
        }

        [RpcMethod]
        public string StringMe(string x)
        {
            return x;
        }

        [RpcMethod]
        public double add_1(double l, double r)
        {
            return l + r;
        }

        [RpcMethod]
        public int addInt_1(int l, int r)
        {
            return l + r;
        }

        [RpcMethod]
        public float NullableFloatToNullableFloat_1(float a)
        {
            return a;
        }

        [RpcMethod]
        public decimal Test2_1(decimal x)
        {
            return x;
        }

        [RpcMethod]
        public string StringMe_1(string x)
        {
            return x;
        }

        [RpcMethod]
        public double add_2(double l, double r)
        {
            return l + r;
        }

        [RpcMethod]
        public int addInt_2(int l, int r)
        {
            return l + r;
        }

        [RpcMethod]
        public float NullableFloatToNullableFloat_2(float a)
        {
            return a;
        }

        [RpcMethod]
        public decimal Test2_2(decimal x)
        {
            return x;
        }

        [RpcMethod]
        public string StringMe_2(string x)
        {
            return x;
        }

        [RpcMethod]
        public double add_3(double l, double r)
        {
            return l + r;
        }

        [RpcMethod]
        public int addInt_3(int l, int r)
        {
            return l + r;
        }

        [RpcMethod]
        public float NullableFloatToNullableFloat_3(float a)
        {
            return a;
        }

        [RpcMethod]
        public decimal Test2_3(decimal x)
        {
            return x;
        }

        [RpcMethod]
        public void StringMe_3(string x)
        {
            
        }
    }
}
