# Retrospection.CommandLine

### Version 0.0.1.0002 Release Notes
- Added XML comments to source
- Created this documentation for NuGet page

# Purpose
To eliminate much of the boilerplate code that goes with writing command line applications, make validating command line arguments a lot easier, and provide auto-complete and menu services via the keyboard.

# Known Issues (v.Current)
- Documentation is sorely lacking
- Menu customization needs to be exposed for public consumption
- Various edge cases need testing
- Help Text Generator formatting is poor

# Getting Started
There are (currently) two main classes of interest that do all the heavy lifting.  **ActionInferer\<T\>** and **IntelliPrompt**.  The remaining public classes are attribute-derived types that support these.

## ActionInferer
This provides the ability to map string arguments directly to methods and properties on a class which have been advertised via the **Cmd** and **Prm** attributes respectively.  

### Simple Example:
```
using Retrospection.CommandLine;

class Program
{
    [Prm] internal static string Filename { get; set; }

    [Cmd] internal static void Run() => System.Diagnostics.Process.Start(Filename);
    
    static void Main(string[] args)
    {
        var ai = new ActionInferer<Program>(args);
        ai.Invoke();

        // Assuming the executable was launched like this:
        // myprogram.exe run --filename=notepad.exe
        // It will set the Filename property to "notepad.exe" and then invoke the Run method.
    }
}
```
*More detailed examples showing instance methods, validation and help text are coming soon.*


# IntelliPrompt
This provides a command line prompt which supports command history, customizable auto-complete, better cursor navigation, and a menu.

### Simple Example:
```
using Retrospection.CommandLine;

class Program
{
    static void Main(string[] args)
    {
        var opts = new[] { "Kirk", "Picard", "Sisko", "Janeway", "Archer", "Pike" };
        var prompt = new IntelliPrompt(null, opts, null);
        prompt.AutoCompleteMode = AutoCompleteMode.StartsWith;
        
        while (true)
        {
            var s = prompt.ReadLine("$");
            Console.WriteLine();
            Console.WriteLine($" You entered: {s}");
        }
    }
}
```
Run the above application and try typing "A" and then hitting the tab key.  You can also hit the context menu key on the keyboard (to the left of the right-most Ctrl key on most full-size keyboards) to get a menu of the available commands.  Hitting Escape will close the menu.

*More detailed examples demonstrating custom auto-complete and a structured command system are coming soon.*