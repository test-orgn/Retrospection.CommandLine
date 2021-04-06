
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

            Console.WriteLine("\r\n\r\n -- More samples to follow soon\r\n");
            
            string[] commands = { "Simple", "Validation", "Dates", "HelpText", "Quit" };

            var prompter = new IntelliPrompt(commands);

            while (true)
            {
                var cmd = prompter.ReadLine("$ ")
                    .ToLower();
                
                if (cmd == "simple")
                {
                    // create a simple class and argify some params to send to it
                    var simpleArgs = IntelliPrompt.Argify("--s howmanytimes=3 --d name=Girtrude");
                    var simpleInvoker = new ActionInferer<Simple>(simpleArgs, new Simple());
                    simpleInvoker.Invoke();
                }
                else if(cmd == "validation")
                {
                    // class that shows various validation methods
                }
                else if (cmd == "dates")
                {
                    // class that shows date interpreter methods
                }
                else if (cmd == "helptext")
                {
                    // class that shows various methods with helptext and generates it
                }
                else if (cmd == "quit")
                {
                    return;
                }
            }
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
