
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;


namespace Retrospection.CommandLine
{
    public sealed class ActionInferer<TModel> where TModel : class
    {
        // TODO: require -- prefix for props but not for commands


        private IEnumerable<string> _args;
        private readonly TModel _model;
        private readonly Type _modelType;
        private readonly Dictionary<string, string> _parameters;
        private readonly Dictionary<string, string> _aliases;
        private readonly IEnumerable<MethodInfo> _invokeList;
        private readonly IEnumerable<MethodInfo> _allMethods;
        private readonly IEnumerable<PropertyInfo> _allProps;
        private string _missingAtLeastOneOf;
        private Dictionary<string, IEnumerable<string>> _missingCombos;
        private Dictionary<string, IEnumerable<string>> _incorrectPairings;
        private Dictionary<string, IEnumerable<string>> _missingMethodParams;
        private StringComparer _stringComparer = StringComparer.OrdinalIgnoreCase;

        public Predicate<IEnumerable<string>> PreValidater { get; set; }
        public Predicate<IDictionary<string, string>> ParamsValidater { get; set; }
        public bool IsCaseSensitive { get; init; }
        public bool IsValid { get; private set; }

        public ActionInferer(IEnumerable<string> args) : this(args, null, false) { }
        public ActionInferer(IEnumerable<string> args, TModel model) : this(args, model, false) { }
        public ActionInferer(IEnumerable<string> args, TModel model, bool caseSensitive)
        {
            _stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            IsCaseSensitive = caseSensitive;

            if (!(PreValidater?.Invoke(args) ?? true)) return;

            _args = args;
            _model = model;
            _modelType = typeof(TModel);

            var bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            if (_model != null) bindingFlags |= BindingFlags.Instance;

            // Get a list of all possible aliases
            _aliases = GetAliases(_modelType);

            // Convert args to a dictionary
            _parameters = GetParameters(args);

            if (!(ParamsValidater?.Invoke(_parameters) ?? true)) return;

            var allMethods = _modelType.GetMethods(bindingFlags)
                .Where(method => method.GetCustomAttributes<CmdAttribute>().Any() && _parameters.ContainsKey(method.Name))
                .OrderBy(method => method.GetCustomAttribute<CmdAttribute>().Order);

            var allProps = _modelType.GetProperties(bindingFlags | BindingFlags.FlattenHierarchy)
                .Where(prop => prop.GetCustomAttributes<PrmAttribute>().Any());

            _allMethods = allMethods;
            _allProps = allProps;

            // Figure out what we have to call
            _invokeList = GetInvokeList(_parameters, allMethods);

            // Check to see if any props were incorrectly combined, missing etc
            PopulateValidationWarnings(allProps, allMethods);

            IsValid = !(_missingAtLeastOneOf != null || _missingCombos.Any() || _incorrectPairings.Any() || _missingMethodParams.Any());

            if (!IsValid) return;

            // Map values to the properties
            MapParametersToProperties(_parameters, allProps);
        }

        public string GetFormattedValidationText()
        {
            var missing = _missingCombos
                .Select(c => $"When specifying {c.Key}, {string.Join(", ", c.Value)} must also be specified");

            var missingMethParams = _missingMethodParams
                .Select(c => $"When specifying {c.Key}, {string.Join(", ", c.Value)} must also be specified");

            var badCombos = _incorrectPairings
                .Select(c => $"{string.Join(", ", c.Value)} cannot be combined with {c.Key}");

            var allErrors =
                missing
                .Union(missingMethParams)
                .Union(badCombos);

            var text= string.Join("\r\n", allErrors);
            
            if(_missingAtLeastOneOf != null)
            {
                text = _missingAtLeastOneOf + "\r\n" + text;
            }

            return text;
        }
        public string GetHelpText()
        {
            // Available Properties
            var propHelp = _allProps
                .Select(p => GetPropertyHelpText(p));

            // Available Commands
            var commandHelp = _allMethods
                .Select(p => GetMethodHelpText(p));

            return string.Join("\r\n", commandHelp.Union(propHelp));
        }
        private static string GetPropertyHelpText(PropertyInfo prop)
        {
            var attr = prop.GetCustomAttribute<PrmAttribute>();
            string name;
            string ret;

            if (attr.Alias != "" && prop.Name.ToLower().StartsWith(attr.Alias.ToLower()) && prop.Name.Length > attr.Alias.Length)
            {
                name = $"--{attr.Alias}({prop.Name.Substring(attr.Alias.Length)})";
            }
            else if (attr.Alias != "")
            {
                name = $"--{prop.Name} | --{attr.Alias}";
            }
            else
            {
                name = "--" + prop.Name;
            }

            if (prop.PropertyType != typeof(bool)) name += $"=<{prop.Name}>";
            if (!attr.IsRequired) name = $"[{name}]";

            ret = $"  {name}\t\t{attr.Description}";

            if (attr.Needs.Any())
            {
                if (attr.Description != "") ret += "\t";
                ret += $"\t(Requires {string.Join(", ", attr.Needs)})";
            }
            if (attr.Excludes.Any())
            {
                if (attr.Description != "") ret += "\t";
                ret += $"\t(Cannot be used with {string.Join(", ", attr.Excludes)})";
            }

            return ret;
        }
        private string GetMethodHelpText(MethodInfo method)
        {
            var attr = method.GetCustomAttribute<CmdAttribute>();
            string name;
            string ret;

            if (attr.Alias != "" && method.Name.ToLower().StartsWith(attr.Alias.ToLower()) && method.Name.Length > attr.Alias.Length)
            {
                name = $"{attr.Alias}({method.Name.Substring(attr.Alias.Length)})";
            }
            else if (attr.Alias != "")
            {
                name = $"{method.Name} | {attr.Alias}";
            }
            else
            {
                name = method.Name;
            }

            ret = $"  {name}\t\t{attr.Description}";

            //if (attr.Needs.Any())
            //{
            //    if (attr.Description != "") ret += "\t";
            //    ret += $"\t(Requires {string.Join(", ", attr.Needs)})";
            //}
            //if (attr.Excludes.Any())
            //{
            //    if (attr.Description != "") ret += "\t";
            //    ret += $"\t(Cannot be used with {string.Join(", ", attr.Excludes)})";
            //}

            return ret;
        }
        private Dictionary<string, string> GetAliases(Type t)
        {
            var allMethods = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => !string.IsNullOrWhiteSpace(method.GetCustomAttribute<CmdAttribute>()?.Alias));

            var allProps = t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic)
                .Where(prop => !string.IsNullOrWhiteSpace(prop.GetCustomAttribute<PrmAttribute>()?.Alias));

            var allParameters = allMethods
                .SelectMany(m => m.GetParameters())
                .Where(prm => !string.IsNullOrWhiteSpace(prm.GetCustomAttribute<PrmAttribute>()?.Alias));

            var aliases = allMethods.ToDictionary(m => m.GetCustomAttribute<CmdAttribute>().Alias, m => m.Name);
            aliases.AddRange(allProps.ToDictionary(m => m.GetCustomAttribute<PrmAttribute>().Alias, m => m.Name));
            aliases.AddRange(allParameters.ToDictionary(m => m.GetCustomAttribute<PrmAttribute>().Alias, m => m.Name));

            return aliases;
        }
        private void PopulateValidationWarnings(IEnumerable<PropertyInfo> allProps, IEnumerable<MethodInfo> allMethods)
        {
            var failAtLeastOneOf = !_modelType.GetCustomAttribute<NeedsAtLeastOneOfAttribute>()?
                .Switches
                .Any(s => _parameters.ContainsKey(s))
                ?? false;

            if (failAtLeastOneOf)
            {
                _missingAtLeastOneOf = "At least one of the following must be specified: " +
                    string.Join(',', _modelType
                    .GetCustomAttribute<NeedsAtLeastOneOfAttribute>()
                    .Switches);
            }

            _missingCombos = GetParamValidationFails(
                allProps,
                _parameters,
                (p, name) => p.GetCustomAttribute<PrmAttribute>().Needs.Contains(name, _stringComparer),
                n => !_parameters.ContainsKey(n));

            _incorrectPairings = GetParamValidationFails(
                allProps,
                _parameters,
                (p, name) => p.GetCustomAttribute<PrmAttribute>().Excludes.Contains(name, _stringComparer),
                n => _parameters.ContainsKey(n));

            PopulateMissingMethodParamsValidations(allMethods, _parameters);
            PopulateMethodParamComboValidations(allMethods, _parameters);
            PopulateMethodComboValidations(_allMethods, _parameters);
        }
        private static Dictionary<string, IEnumerable<string>> GetParamValidationFails(
            IEnumerable<MemberInfo> members,
            Dictionary<string, string> parameters,
            Func<MemberInfo, string, bool> selector,
            Func<string, bool> validationTrigger
            )
        {
            var filtered = members
                .Select(prop => (prop, members.Where(p => p != prop && selector(p, prop.Name))))
                .Where(pair => pair.Item2.Any() && parameters.ContainsKey(pair.prop.Name))
                .ToDictionary(p => p.prop, p => p.Item2.Select(k => k.Name));

            return filtered
                .Select(combo => (combo.Key.Name, combo.Value.Where(validationTrigger)))
                .Where(x => x.Item2.Any())
                .ToDictionary(x => x.Name, x => x.Item2);
        }
        private void PopulateMissingMethodParamsValidations(IEnumerable<MethodInfo> allMethods, Dictionary<string, string> parameters)
        {
            var missing = allMethods
                .Select(m => (m.Name, m.GetParameters().Where(prm => (prm.GetCustomAttribute<PrmAttribute>()?.IsRequired ?? false) && !parameters.ContainsKey(prm.Name)).Select(r => r.Name)))
                .Where(t => t.Item2.Any())
                .ToDictionary(k => k.Name, k => k.Item2);

            _missingMethodParams = missing;
        }
        private void PopulateMethodParamComboValidations(IEnumerable<MethodInfo> allMethods, Dictionary<string, string> parameters)
        {
            foreach (var method in allMethods)
            {
                var combos = method.GetParameters().Where(p => (p.GetCustomAttribute<PrmAttribute>()?.Needs.Any() ?? false) && parameters.ContainsKey(p.Name));
                var mutexes = method.GetParameters().Where(p => (p.GetCustomAttribute<PrmAttribute>()?.Excludes.Any() ?? false) && parameters.ContainsKey(p.Name));

                var missingCombos = combos.Select(combo => (combo, combo.GetCustomAttribute<PrmAttribute>().Needs.Where(must => !parameters.ContainsKey(must))))
                    .ToDictionary(c => c.combo.Name, c => c.Item2);

                var badCombos = mutexes.Select(mutex => (mutex, mutex.GetCustomAttribute<PrmAttribute>().Excludes.Where(mustnt => parameters.ContainsKey(mustnt))))
                    .ToDictionary(c => c.mutex.Name, c => c.Item2);

                _missingCombos.AddRange(missingCombos);
                _incorrectPairings.AddRange(badCombos);
            }
        }
        private void PopulateMethodComboValidations(IEnumerable<MethodInfo> allMethods, Dictionary<string, string> parameters)
        {
            var combos = allMethods.Where(m => (m.GetCustomAttribute<CmdAttribute>()?.Needs.Any() ?? false) && parameters.ContainsKey(m.Name));
            var mutexes = allMethods.Where(m => (m.GetCustomAttribute<CmdAttribute>()?.Excludes.Any() ?? false) && parameters.ContainsKey(m.Name));

            var missingCombos = combos.Select(combo => (combo, combo.GetCustomAttribute<PrmAttribute>().Needs.Where(must => !parameters.ContainsKey(must))))
                .ToDictionary(c => c.combo.Name, c => c.Item2);

            var badCombos = mutexes.Select(mutex => (mutex, mutex.GetCustomAttribute<PrmAttribute>().Excludes.Where(mustnt => parameters.ContainsKey(mustnt))))
                .ToDictionary(c => c.mutex.Name, c => c.Item2);

            _missingCombos.AddRange(missingCombos);
            _incorrectPairings.AddRange(badCombos);
        }
        private IEnumerable<string> GetParameterValidationText()
        {
            var missing = _missingCombos
                .Select(kvp => kvp.Value.Select(v => $"Specifying '{kvp.Key}' requires that {v} is also specified"))
                .SelectMany(g => g);

            var exclusive = _incorrectPairings
                .Select(kvp => kvp.Value.Select(v => $"'{kvp.Key}' cannot be combined with {v}"))
                .SelectMany(g => g);

            return missing.Union(exclusive);
        }
        public void Invoke()
        {
            if (!IsValid) throw new ApplicationException("Invoke cannot be called when the IsValid property is false.");

            foreach (var method in _invokeList)
            {
                var prms = method.GetParameters()
                    .Select<ParameterInfo, object>(prm => _parameters.ContainsKey(prm.Name)
                    ? ConvertParameterVal(_parameters[prm.Name], prm.ParameterType)
                    : prm.IsOptional ? Type.Missing : null
                    )
                    .ToArray();

                method.Invoke(_model, prms);
            }
        }
        private Dictionary<string, string> GetParameters(IEnumerable<string> args)
        {
            var mapped = args
                .Select(arg => arg.Split('='))
                .ToDictionary(
                    parts => parts[0].StartsWith("--") ? parts[0][2..] : parts[0],
                    parts => parts.Length > 1 ? string.Join('=', parts.Skip(1)) : string.Empty,
                    _stringComparer);

            // Convert aliases to real
            foreach (var alias in _aliases)
            {
                if (mapped.ContainsKey(alias.Key))
                {
                    var val = mapped[alias.Key];
                    mapped.Remove(alias.Key);
                    mapped.Add(alias.Value, val);
                }
            }

            return mapped;
        }
        private void MapParametersToProperties(Dictionary<string, string> parameters, IEnumerable<PropertyInfo> allProps)
        {
            foreach (var prop in allProps)
            {
                if (parameters.ContainsKey(prop.Name))
                {
                    var setter = prop.GetSetMethod(true);
                    setter.Invoke(_model, new[] { ConvertParameterVal(parameters[prop.Name], prop.PropertyType) });
                }
            }
        }
        private object ConvertParameterVal(string parameter, Type toType)
        {
            if (toType == typeof(bool))
            {
                return parameter.Trim().ToLower() switch
                {
                    "false" or "f" or "0" or "no" or "n" => false,  // TODO: "False" aliases / ("True" also?)
                    _ => true
                };
            }
            else if (toType == typeof(DateTime))
            {
                return parameter.Trim() switch
                {
                    "now" or "t" => DateTime.Now,
                    "yesterday" => DateTime.Now.Date.AddDays(-1),
                    "today" => DateTime.Now.Date,
                    "tomorrow" => DateTime.Now.Date.AddDays(1),
                    string dt when Regex.IsMatch(dt, @"^(\-|\+)[0-9]+[ymwdhs]$", RegexOptions.IgnoreCase) => GetRelativeDate(parameter.Trim()),    // passing parameter because case sensitivity is now required
                    string dt => DateTime.Parse(dt),
                    _ => DateTime.MinValue,
                };
            }
            else
            {
                return Convert.ChangeType(parameter, toType);
            }
        }
        public static DateTime GetRelativeDate(string expression)
        {
            // +20d or -15h for example

            var multi = expression[0] == '-' ? -1 : 1;
            var qty = int.Parse(expression[1..^1]);
            var measure = expression[^1];

            return measure switch
            {
                'y' or 'Y' => DateTime.Now.Date.AddYears(qty * multi),
                'M' => DateTime.Now.Date.AddMonths(qty * multi),
                'w' or 'W' => DateTime.Now.Date.AddDays(qty * multi * 7),
                'd' or 'D' => DateTime.Now.Date.AddDays(qty * multi),
                'h' or 'H' => DateTime.Now.AddHours(qty * multi),            // For time measurements we include current time, date measurements we just go with date
                'm' => DateTime.Now.AddMinutes(qty * multi),
                's' => DateTime.Now.AddSeconds(qty * multi),
                _ => DateTime.Now,
            };
        }
        private IEnumerable<MethodInfo> GetInvokeList(Dictionary<string, string> parameters, IEnumerable<MethodInfo> allMethods)
        {
            return allMethods
                .Where(method => parameters.ContainsKey(method.Name) && parameters[method.Name] == "");
        }
    }
}
