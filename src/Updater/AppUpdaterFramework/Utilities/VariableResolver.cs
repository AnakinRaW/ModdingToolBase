﻿using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

internal class VariableResolver : IVariableResolver
{
    public static readonly IVariableResolver Default = new VariableResolver();

    public string ResolveVariables(string value, IDictionary<string, string?>? variables)
    {
        return value.ReplaceVariables(variables);
    }
}