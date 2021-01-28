namespace TestServer
{
    interface ITest2
    {
        int AddInt(int a, int b);
    }

    class InterfaceTest:ITest2
    {
        public int AddInt(int a, int b)
        {
            return a + b;
        }
    }
}
