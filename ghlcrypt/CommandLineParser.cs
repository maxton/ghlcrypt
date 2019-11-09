using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHLCrypt
{
  public class Verb
  {
    public string Name;
    public string HelpText;
    public List<ArgDef> Args;

    /// <summary>
    /// The body of the verb. The first param is a map of switch name -> switch present,
    /// the second is a map of optional value name -> value,
    /// the third is a list of positional arguments.
    /// </summary>
    public Action<Dictionary<string, bool>, Dictionary<string, string>, string[]> Body;

    /// <summary>
    /// Creates a verb that uses only positional arguments.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static Verb Create(string name, string helpText, List<ArgDef> args, Action<string[]> action)
    {
      return new Verb { Name = name, HelpText = helpText, Args = args, Body = (_, _2, a) => action(a) };
    }

    /// <summary>
    /// Creates a verb that uses boolean switches and positional arguments.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static Verb Create(string name, string helpText, List<ArgDef> args, Action<Dictionary<string, bool>, string[]> action)
    {
      return new Verb { Name = name, HelpText = helpText, Args = args, Body = (b, _, n) => action(b, n) };
    }


    /// <summary>
    /// Creates a verb that uses boolean switches, optional parameters, and positional arguments.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="args"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static Verb Create(string name, string helpText, List<ArgDef> args, Action<Dictionary<string, bool>, Dictionary<string, string>, string[]> action)
    {
      return new Verb { Name = name, HelpText = helpText, Args = args, Body = action };
    }
    public override string ToString()
    {
      var options = Args
        .Select(x =>
          x.Type == ArgType.Boolean ? $"[--{x.Name}]" :
          x.Type == ArgType.Optional ? $"[--{x.Name} <...>]" :
          /* ArgType.Positional */ $"<{x.Name}>")
        .Aggregate((x, y) => $"{x} {y}");
      return Name + " " + options;
    }

    public static bool Run(Verb[] verbs, string[] args, string name)
    {
      if (args.Length > 0 && verbs.Where(x => x.Name == args[0]).FirstOrDefault() is Verb v)
      {
        // Parse the command line into separate containers of switches, optional arguments, and positional arguments
        var switches = new Dictionary<string, bool>();
        var optionals = new Dictionary<string, string>();
        var positionalArgs = new List<string>() { args[0] };

        // Keep track of the arguments that still need to be matched in the command line
        var remainingArgs = v.Args.ToList();

        for (int i = 1; i < args.Length; i++)
        {
          if (args[i].StartsWith("--"))
          {
            if (remainingArgs.FirstOrDefault(x => x.Type == ArgType.Boolean && x.Name == args[i].Substring(2)) is ArgDef boolArg)
            {
              remainingArgs.Remove(boolArg);
              switches[boolArg.Name] = true;
            }
            else if (remainingArgs.FirstOrDefault(x => x.Type == ArgType.Optional && x.Name == args[i].Substring(2)) is ArgDef optArg)
            {
              remainingArgs.Remove(optArg);
              ++i;
              if (i >= args.Length)
              {
                Console.WriteLine($"Command line error: No value provided for optional param {args[i - 1]}");
                Console.WriteLine($"Usage: {name} {v}");
                return true;
              }
              optionals[optArg.Name] = args[i];
            }
            else
            {
              Console.WriteLine($"Command line error: Unknown optional parameter \"{args[i]}\"");
              Console.WriteLine($"Usage: {name} {v}");
              return true;
            }
          }
          else // arg doesn't start with --
          {
            if (remainingArgs.FirstOrDefault(x => x.Type == ArgType.Positional) is ArgDef posArg)
            {
              positionalArgs.Add(args[i]);
              remainingArgs.Remove(posArg);
            }
            else
            {
              Console.WriteLine($"Command line error: Too many arguments");
              Console.WriteLine($"Usage: {name} {v}");
              return true;
            }
          }
        }

        // Fill out the unset optional args with the default values, and catch missing required arguments.
        foreach (var arg in remainingArgs)
        {
          switch (arg.Type)
          {
            case ArgType.Boolean:
              switches[arg.Name] = false;
              break;
            case ArgType.Optional:
              optionals[arg.Name] = null;
              break;
            case ArgType.Positional:
              Console.WriteLine("Command line error: not enough arguments");
              Console.WriteLine($"Usage: {name} {v}");
              return true;
          }
        }

        // At this point it is safe to call the body.
        v.Body(switches, optionals, positionalArgs.ToArray());
        return true;
      }

      // In this case, the verb wasn't found, so show the full usage and list of verbs.
      Console.WriteLine($"Usage: {name} <verb> [options ...]");
      Console.WriteLine("");
      Console.WriteLine("Verbs:");
      var verb_list = (args.Length > 0
          && verbs.Where(verb => verb.Name.StartsWith(args[0])).ToArray() is Verb[] prefixList
          && prefixList.Length > 0) ? prefixList : verbs;
      foreach (var verb in verb_list.OrderBy(z => z.Name))
      {
        Console.WriteLine($"  {verb}");
        Console.WriteLine($"    {verb.HelpText}");
        Console.WriteLine();
      }
      return false;
    }
  }

  public enum ArgType
  {
    Boolean, Optional, Positional
  }
  public class ArgDef
  {
    public string Name;
    public ArgType Type = ArgType.Positional;
    public static List<ArgDef> Multi(List<ArgDef> optionalArgs, params string[] names)
    {
      return optionalArgs.Concat(Required(names)).ToList();
    }
    public static List<ArgDef> Multi(List<ArgDef> optionalArgs, List<ArgDef> moreOptionalArgs, params string[] names)
    {
      return optionalArgs.Concat(moreOptionalArgs).Concat(Required(names)).ToList();
    }
    public static List<ArgDef> Required(params string[] names)
    {
      return names.Select(x => new ArgDef { Name = x }).ToList();
    }
    public static List<ArgDef> Bool(params string[] names)
    {
      return names.Select(x => new ArgDef { Name = x, Type = ArgType.Boolean }).ToList();
    }
    public static List<ArgDef> Option(params string[] names)
    {
      return names.Select(x => new ArgDef { Name = x, Type = ArgType.Optional }).ToList();
    }
  }
}
