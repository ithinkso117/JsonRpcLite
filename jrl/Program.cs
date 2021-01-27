using System;
using System.Linq;

namespace jrl
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("*********************************************************************");
            Console.WriteLine("*                   Code generator for JsonRpcLite                  *");
            Console.WriteLine("*********************************************************************");
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: jrl -i [SMD-URL] -c [CODE] -o [OUTPUT]");
                Console.WriteLine("       [SMD-URL] The url contains the service mapping description.");
                Console.WriteLine("       [CODE] The code to generate, could be csharp, dart, javascript.");
                Console.WriteLine("       [OUTPUT] The output folder to store the generated files.");
            }
            else
            {
                if (args.Contains("-i"))
                {

                }
                if (args.Contains("-c"))
                {

                }
                if (args.Contains("-o"))
                {

                }
            }
        }
    }
}
