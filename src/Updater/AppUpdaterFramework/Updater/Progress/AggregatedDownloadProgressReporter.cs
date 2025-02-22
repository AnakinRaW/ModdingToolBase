﻿using System;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal class AggregatedDownloadProgressReporter(IComponentProgressReporter progressReporter, IEnumerable<IComponentStep> steps) 
    : ComponentAggregatedProgressReporter(progressReporter, steps)
{
    private const int MovingAverageCalculationWindow = 1000;

    private readonly object _syncLock = new();
    private readonly IDictionary<string, long> _progressTable = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

    private int _reportTimes = 1;
    private int _completedPackageCount;
    private long _completedSize;
    private long _completedSizeForSpeedCalculation;
    private long _previousCompletedSizeForSpeedCalculation;
    private long _byteRate;
    private DateTime _downloadTime = DateTime.Now;

    protected override ProgressType Type => ProgressTypes.Download;

    protected override double CalculateAggregatedProgress(IComponentStep step, double progress, out ComponentProgressInfo progressInfo)
    {
        var now = DateTime.Now;
        var key = step.Component.GetUniqueId();
        var totalTaskProgressSize = (long)(progress * step.Size);

        if (progress >= 1.0)
            Interlocked.Increment(ref _completedPackageCount);

        double currentProgress;

        progressInfo = new();

        lock (_syncLock)
        {
            if (!_progressTable.ContainsKey(key))
            {
                _progressTable.Add(key, totalTaskProgressSize);
                _completedSize += totalTaskProgressSize;
                _completedSizeForSpeedCalculation += totalTaskProgressSize;
            }
            else
            {
                var deltaSize = totalTaskProgressSize - _progressTable[key];
                _progressTable[key] = totalTaskProgressSize;
                _completedSize += deltaSize;
                _completedSizeForSpeedCalculation += deltaSize;
            }

            if (_completedSize < 0)
                _completedSize = 0;
            if (_completedSizeForSpeedCalculation < 0)
                _completedSizeForSpeedCalculation = 0;

            currentProgress = (double)_completedSize / TotalSize;
            currentProgress = Math.Min(currentProgress, 1.0);

            var deltaDownloadSpeed = _completedSizeForSpeedCalculation - _previousCompletedSizeForSpeedCalculation;
            var totalMilliseconds = (now - _downloadTime).TotalMilliseconds;

            if (totalMilliseconds > 10000.0)
            {
                _previousCompletedSizeForSpeedCalculation = _completedSizeForSpeedCalculation;
                _downloadTime = now;
            }
            if (deltaDownloadSpeed >= 0 && totalMilliseconds != 0.0)
            {
                var currentByteRate = (long)(deltaDownloadSpeed * MovingAverageCalculationWindow / totalMilliseconds);
                if (_reportTimes > MovingAverageCalculationWindow)
                    _reportTimes = 1;
                _byteRate = CalculateMovingAverage(currentByteRate, _byteRate, _reportTimes);
                ++_reportTimes;
            }
            progressInfo.DownloadedSize = _completedSize;
            progressInfo.DownloadSpeed = _byteRate;
            progressInfo.TotalSize = TotalSize;
        }

        if (_completedPackageCount >= TotalStepCount && progress >= 1.0)
        {
            currentProgress = 1.0;
            progressInfo.DownloadSpeed = 0;
        }
        else
            currentProgress *= 0.99;

        return currentProgress;
    }

    private static long CalculateMovingAverage(long currentByteRate, long previousByteRate, int reportTimes)
    {
        return previousByteRate + (currentByteRate - previousByteRate) / reportTimes;
    }
}