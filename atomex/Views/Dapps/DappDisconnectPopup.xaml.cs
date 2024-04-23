using atomex.ViewModels.DappsViewModels;
using Rg.Plugins.Popup.Pages;
using Microsoft.Maui.Controls.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DappDisconnectPopup : PopupPage
    {
        public DappDisconnectPopup(DappViewModel dappViewModel)
        {
            InitializeComponent();
            BindingContext = dappViewModel;
        }
    }
}