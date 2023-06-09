﻿using System;

namespace AnakinRaW.CommonUtilities.Wpf.Imaging;

public struct ImageKey : IEquatable<ImageKey>
{
    public Type CatalogType;
    public string Name;

    public override bool Equals(object? obj)
    {
        return obj is ImageKey other && Equals(other);
    }

    public bool Equals(ImageKey other)
    {
        return CatalogType == other.CatalogType && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CatalogType, Name);
    }

    public static bool operator ==(ImageKey key1, ImageKey key2)
    {
        return key1.Equals(key2);
    }

    public static bool operator !=(ImageKey key1, ImageKey key2)
    {
        return !key1.Equals(key2);
    }
}