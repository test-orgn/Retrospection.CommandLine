
using System;


namespace Retrospection.CommandLine
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CmdAttribute : PrmAttribute
    {
        public int Order { get; init; }

        public CmdAttribute(
                string Alias = "",
                string Description = "",
                string Category = "",
                bool Required = false,
                string[] Needs = null,
                string[] Excludes = null,
                int Order = int.MaxValue) : base(Alias, Description, Category, Required, Needs, Excludes)
        {
            this.Order = Order;
        }
    }
}
