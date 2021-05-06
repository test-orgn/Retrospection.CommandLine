
using Retrospection.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SampleUseCases
{
    [NeedsAtLeastOneOf("Age", "DOB")]
    class ValidationExample
    {
        [Prm(Excludes: new[] { "DOB" })]
        int? Age { get; set; }

        [Prm(Excludes: new[] { "Age" })]
        DateTime DOB { get; set; }

        [Prm(Required: true)]
        string Name { get; set; }

        [Cmd] void PrintGreeting()
        {
            //var personAge = Age ?? (int)(DateTime.Now.Subtract(DOB).TotalDays / 365);



            Console.WriteLine($"Hello, {Name}! You are {Age} years old!");
        }
    }
}
