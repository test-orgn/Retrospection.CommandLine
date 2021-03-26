
using System;
using System.Collections.Generic;


namespace Retrospection.CommandLine
{
    /// <summary>
    /// Indicates that a property can be set by an instance of the ActionInferer class.  Can also be used on parameters for methods decorated with the Cmd attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public class PrmAttribute : Attribute
    {
        /// <summary>
        /// If a parameter matches this alias it will be mapped to this.
        /// </summary>
        public string Alias { get; init; }

        /// <summary>
        /// The description used when generating help text.
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// The category used when generating help text.
        /// </summary>
        public string Category { get; init; }

        /// <summary>
        /// Used for validation. Indicates if this is mandatory.
        /// </summary>
        public bool IsRequired { get; init; }

        /// <summary>
        /// Used for validation.  An array of strings containing names of other parameters or commands that must be present for this parameter to be set.
        /// </summary>
        public IEnumerable<string> Needs { get; init; }

        /// <summary>
        /// Used for validation.  An array of strings containing names of other parameters or commands that must not be present for this parameter to be set.
        /// </summary>
        public IEnumerable<string> Excludes { get; init; }

        /// <summary>
        /// Create a Prm attribute for use with an instance of ActionInvoker.
        /// </summary>
        /// <param name="Alias">If a parameter matches this alias it will be mapped to this command.</param>
        /// <param name="Description">The description used when generating help text.</param>
        /// <param name="Category">The category used when generating help text.</param>
        /// <param name="Required">Used for validation. Indicates if this parameter is mandatory.</param>
        /// <param name="Needs">Used for validation.  An array of strings containing names of other parameters or commands that must be present for this parameter to be set.</param>
        /// <param name="Excludes">Used for validation.  An array of strings containing names of other parameters or commands that must not be present for this parameter to be set.</param>
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
