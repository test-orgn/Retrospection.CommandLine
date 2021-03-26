
using System;
using Retrospection.CommandLine;


namespace SampleUseCases
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instantiate an ActionInferer and pass in the args.
            ActionInferer<Program> ai = new ActionInferer<Program>(args);
            
            if (ai.IsValid)
            {
                ai.Invoke();
            }

            Console.WriteLine("\r\n\r\n -- More samples to follow soon");
        }

        [Cmd] 
        static void ShowHelp(string name)
        {
            Console.WriteLine($"Hello from {name}!");
            Console.WriteLine($"This method was invoked because the Debug properties (Right Click on the Project in Visual Studio and select Properties -> Debug, " +
                $"or check the launchSettings.json file) included a parameter to call this method, and also passed in an argument called 'name'.");
        }
    }
}
