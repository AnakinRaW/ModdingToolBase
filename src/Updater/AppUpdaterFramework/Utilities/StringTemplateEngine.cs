using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

public static class StringTemplateEngine
{
    private const char VariableStart = '[';
    private const char VariableEnd = ']';
    
    public static string ToVariable(string value)
    {
        value = value.TrimStart('[');
        value = value.TrimEnd(']');
        return $"[{value}]";
    }

    public static string ResolveVariables(string value, IDictionary<string, string> variables, bool recursive = false)
    {
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));
        return ResolveVariables(value, (IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(variables), recursive);
    }
    
    public static string ResolveVariables(string value, IReadOnlyDictionary<string, string> properties, bool recursive = false)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var expandedString = value;
        string currentValue;

        do
        {
            currentValue = expandedString;
            var stringBuilder = new StringBuilder();
            var startIndex = 0;
            int? varStartIndex = null;
            for (var i = 0; i < expandedString.Length; ++i)
            {
                var ch = expandedString[i];
                if (!varStartIndex.HasValue)
                {
                    varStartIndex = ch switch
                    {
                        VariableStart => i,
                        _ => varStartIndex
                    };
                    continue;
                }
                if (ch == VariableEnd)
                {
                    var varName = expandedString.Substring(varStartIndex.Value + 1, i - varStartIndex.Value - 1);
                    if (TryResolveVariable(varName, properties, out var resolvedVariable))
                    {
                        stringBuilder.Append(expandedString, startIndex, varStartIndex.Value - startIndex);
                        stringBuilder.Append(resolvedVariable);
                        startIndex = i + 1;
                    }
                    varStartIndex = null;
                }
            }

            if (startIndex == 0)
                return value;
            if (startIndex < expandedString.Length)
                stringBuilder.Append(expandedString, startIndex, expandedString.Length - startIndex);
            expandedString = stringBuilder.ToString();

        } while (recursive && !currentValue.Equals(expandedString, StringComparison.OrdinalIgnoreCase));

        return expandedString;
    }

    private static bool TryResolveVariable(
        string name, IReadOnlyDictionary<string, string> properties, out string? value)
    {
        value = null;

        if (string.IsNullOrEmpty(name))
            return false;

        if (properties.TryGetValue(name, out value)) 
            return true;
        
        value = Environment.GetEnvironmentVariable(name);
        if (value is not null)
            return true;
       
        value = string.Empty;
        return true;
    }
}