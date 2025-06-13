using System;
using System.Threading.Tasks;
using WebAppTester.Services;

namespace WebAppTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Web API Tester");
            Console.WriteLine("==============");
            
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a path to a YAML test configuration file.");
                Console.WriteLine("Usage: WebAppTester <path-to-yaml-file>");
                return;
            }
            
            string yamlFilePath = args[0];
            
            try
            {
                var testRunner = new TestRunnerService();
                await testRunner.RunTestsAsync(yamlFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
