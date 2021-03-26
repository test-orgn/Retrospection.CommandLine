
using System;


namespace Retrospection.CommandLine
{
    /// <summary>
    /// Indicates that a method can be invoked by an instance of the ActionInferer class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CmdAttribute : PrmAttribute
    {
        /// <summary>
        /// Indicates what order a method should be invoked.  Methods are invoked in order from lowest to highest.
        /// </summary>
        public int Order { get; init; }

        /// <summary>
        /// Create a Cmd attribute for use with an instance of ActionInvoker.
        /// </summary>
        /// <param name="Alias">If a parameter matches this alias it will be mapped to this command.</param>
        /// <param name="Description">The description used when generating help text.</param>
        /// <param name="Category">The category used when generating help text.</param>
        /// <param name="Required">Used for validation. Indicates if this command is mandatory.</param>
        /// <param name="Needs">Used for validation.  An array of strings containing names of other parameters that must be present for this command to be called.</param>
        /// <param name="Excludes">Used for validation.  An array of strings containing names of other parameters that must not be present for this command to be called.</param>
        /// <param name="Order">Indicates what order a method should be invoked.  Methods are invoked in order from lowest to highest.</param>
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
