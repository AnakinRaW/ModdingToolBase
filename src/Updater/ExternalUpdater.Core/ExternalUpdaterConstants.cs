﻿namespace AnakinRaW.ExternalUpdater;

public static class ExternalUpdaterConstants
{
    public const string ComponentIdentity = "AnakinRaW.ExternalUpdater";
    
    public static string GetExecutableFileName()
    {
        return $"{ComponentIdentity}.exe";
    }
}