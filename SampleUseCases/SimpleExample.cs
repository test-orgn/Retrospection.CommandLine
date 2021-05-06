
using Retrospection.CommandLine;
using System;


namespace SampleUseCases
{
    class SimpleExample
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
