using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

internal interface IVariableResolver
{
    public string ResolveVariables(
        string value,
        IDictionary<string, string?>? variables,
        ISet<string>? variablesToIgnore = null,
        bool recursive = false);
}