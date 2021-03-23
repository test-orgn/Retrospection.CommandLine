
using System;
using System.Collections.Generic;


namespace Retrospection.CommandLine
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public class PrmAttribute : Attribute
    {
        public string Alias { get; init; }
        public string Description { get; init; }
        public string Category { get; init; }
        public bool IsRequired { get; init; }
        public IEnumerable<string> Needs { get; init; }
        public IEnumerable<string> Excludes { get; init; }

        public PrmAttribute(
            string Alias = "",
            string Description = "",
            string Category = "",
            bool Required = false,
            string[] Needs = null,
            string[] Excludes = null)
        {
            this.Alias = Alias;
            this.Description = Description;
            this.Category = Category;
            IsRequired = Required;
            this.Needs = Needs ?? Array.Empty<string>();
            this.Excludes = Excludes ?? Array.Empty<string>();
        }
    }
}
