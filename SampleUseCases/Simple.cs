
using Retrospection.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SampleUseCases
{
    class Simple
    {
        [Cmd(Alias = "s", Order = 2)]
        static void SomethingToDo(int howManyTimes)
        {
            Console.WriteLine($"SomethingToDo was executed 2nd, with a howManyTimes parameter of: {howManyTimes}");
        }

        [Cmd(Alias = "d", Order = 1)]
        void DoThisFirst(string name)
        {
            Console.WriteLine($"DoThisFirst is an instance method.\r\nIt was executed 1st, with a name parameter of: {name}");
        }
    }
}
