using System.Threading.Tasks;

namespace TestServer
{
    public interface ITest2
    {
        int AddInt(int a, int b);

        Task<int> AddInt2Async(int a, int b);
    }

    public class InterfaceTest:ITest2
    {
        public int AddInt(int a, int b)
        {
            return a + b;
        }

        public async Task<int> AddInt2Async(int a, int b)
        {
            return await Task.FromResult(a + b);
        }
    }
}
