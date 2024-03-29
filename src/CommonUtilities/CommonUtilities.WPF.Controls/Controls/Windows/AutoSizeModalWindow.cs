using System.Windows;

namespace AnakinRaW.CommonUtilities.Wpf.Controls;

public class AutoSizeModalWindow(IModalWindowViewModel viewModel) : ModalWindow(viewModel)
{
    static AutoSizeModalWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoSizeModalWindow), new FrameworkPropertyMetadata(typeof(AutoSizeModalWindow)));
    }
}