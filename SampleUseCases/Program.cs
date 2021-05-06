
using System;
using System.Linq;
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
                    var simpleInvoker = new ActionInferer<SimpleExample>(simpleArgs, new SimpleExample());
                    simpleInvoker.Invoke();
                }
                else if (cmd == "validation")
                {
                    Console.WriteLine("Input your name and EITHER your age or DOB.");
                    Console.WriteLine("e.g. --name=William --age=42");
                    Console.WriteLine(" OR: --name=\"Jean Luc\" --DOB=1/2/1982");

                    var inp = new IntelliPrompt().ReadLine(" : ");
                    var vprms = IntelliPrompt.Argify(inp).ToList();
                    vprms.Add("--printgreeting");

                    var validateSample = new ActionInferer<ValidationExample>(vprms, new ValidationExample());
                    
                    if (!validateSample.IsValid)
                    {
                        Console.WriteLine();
                        Console.WriteLine(validateSample.GetFormattedValidationText());
                    }
                    else
                    {
                        validateSample.Invoke();
                    }

                }
                else if (cmd == "dates")
                {
                    Console.WriteLine("Try entering a standard date, or a relative date.\r\n" +
                        "Example relative dates are:\r\n" +
                        "  +3d  : today plus 3 days\r\n" +
                        "  -2w  : today minus 2 weeks\r\n" +
                        "The following constants are supported in relative format:\r\n" +
                        "  y or Y  : Year\r\n" +
                        "  M       : Month\r\n" +
                        "  w or W  : Week\r\n" +
                        "  d or D  : Day\r\n" +
                        "  h or H  : Hour\r\n" +
                        "  m       : Minute\r\n" +
                        "  s       : Second"
                        );
                    
                    var dateInput = Console.ReadLine();
                    Console.WriteLine();

                    if (DateTime.TryParse(dateInput, out var dt))
                    {
                        Console.WriteLine(dt.ToString("G"));
                    }
                    else
                    {
                        var dtRelative = IntelliPrompt.GetRelativeDate(dateInput);
                        Console.WriteLine(dtRelative.ToString("G"));
                    }
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
