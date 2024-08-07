﻿using System;
using System.Windows;
using AnakinRaW.CommonUtilities.Wpf.Utilities;
using Vanara.PInvoke;

namespace AnakinRaW.CommonUtilities.Wpf.DPI;

public class DisplayInfo : IComparable<DisplayInfo>, IEquatable<DisplayInfo>
{
    public double DpiX { get; }

    public double DpiY { get; }

    public bool IsDpiFaulted { get; }

    public bool IsPrimary => MonitorInfo.IsPrimary();

    public IntPtr MonitorHandle { get; }

    public Size Size => MonitorInfo.rcMonitor.ToSize();

    internal User32.MONITORINFO MonitorInfo { get; }

    internal Point Position => MonitorInfo.rcMonitor.GetPosition();

    internal RECT Rect => MonitorInfo.rcMonitor;

    private PolarVector Vector { get; }

    internal DisplayInfo(IntPtr hMonitor, User32.MONITORINFO monitorInfo)
    {
        MonitorHandle = hMonitor;
        MonitorInfo = monitorInfo;
        try
        {
            hMonitor.GetMonitorDpi(out var dpiX, out var dpiY);
            DpiX = dpiX;
            DpiY = dpiY;
        }
        catch
        {
            DpiX = 96.0;
            DpiY = 96.0;
            IsDpiFaulted = true;
        }
        Vector = new PolarVector(Position);
    }

    public bool Equals(DisplayInfo? other)
    {
        if (other is null)
            return false;
        return ReferenceEquals(this, other) || MonitorHandle.Equals(other.MonitorHandle);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;
        return obj is DisplayInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return MonitorHandle.GetHashCode();
    }

    public int CompareTo(DisplayInfo? other)
    {
        if (other is null)
            return 1;
        if (Vector.IsOrigin && !other.Vector.IsOrigin)
            return -1;
        if (!Vector.IsOrigin && other.Vector.IsOrigin)
            return 1;
        if (Vector.Angle < other.Vector.Angle)
            return -1;
        if (Vector.Angle > other.Vector.Angle)
            return 1;
        if (Vector.Length < other.Vector.Length)
            return -1;
        return Vector.Length > other.Vector.Length ? 1 : 0;
    }

    private class PolarVector(Point topLeft)
    {
        public bool IsOrigin => Length == 0.0 && Angle == 0.0;

        public double Angle { get; } = Math.Atan2(topLeft.Y, topLeft.X) * (180.0 / Math.PI);

        public double Length { get; } = new Vector(topLeft.X, topLeft.Y).Length;
    }
}