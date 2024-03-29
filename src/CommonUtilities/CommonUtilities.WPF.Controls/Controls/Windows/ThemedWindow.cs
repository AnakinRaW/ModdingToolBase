using System.Windows;

namespace AnakinRaW.CommonUtilities.Wpf.Controls;

public class ThemedWindow(IWindowViewModel dataContext) : ShadowChromeWindow(dataContext)
{
    static ThemedWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedWindow), new FrameworkPropertyMetadata(typeof(ThemedWindow)));
    }
}