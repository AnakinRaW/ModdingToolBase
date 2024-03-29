using System;
using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;

[AttributeUsage(AttributeTargets.Property)]
public class NotifyChangedIsLinkedToPropertyAttribute(params string[] linkedProperties) : Attribute
{
    public ICollection<string> LinkedProperties { get; } = linkedProperties;
}