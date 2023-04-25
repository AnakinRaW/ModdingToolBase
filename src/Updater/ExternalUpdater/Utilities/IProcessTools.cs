﻿using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.ExternalUpdater.Utilities;

internal interface IProcessTools
{
    void StartApplication(IFileInfo application, ExternalUpdaterResultOptions appStartOptions, IReadOnlyList<string> originalArguments, bool elevate = false);

    Task<bool> WaitForExitAsync(int? pid, CancellationToken token);
}