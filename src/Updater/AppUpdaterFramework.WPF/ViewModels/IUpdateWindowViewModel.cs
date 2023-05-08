using System.Collections.ObjectModel;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.ViewModels;

public interface IUpdateWindowViewModel : IModalWindowViewModel, IViewModel
{
    IProductViewModel ProductViewModel { get; set; }

    IUpdateInfoBarViewModel InfoBarViewModel { get; }

    ObservableCollection<ProductBranch> Branches { get; }

    ProductBranch CurrentBranch { get; set; }

    bool CanSwitchBranches { get; }
}