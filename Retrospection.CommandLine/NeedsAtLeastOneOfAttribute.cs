
using System;
using System.Collections.Generic;


namespace Retrospection.CommandLine
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NeedsAtLeastOneOfAttribute : Attribute
    {
        public IEnumerable<string> Switches { get; private set; }

        public NeedsAtLeastOneOfAttribute(params string[] switches)
        {
            Switches = switches;
        }
    }
}
