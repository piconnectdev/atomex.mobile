using atomex.ViewModels;
using RGPopup.Maui.Pages;

namespace atomex.Views.Popup
{
    public partial class TestNetWalletPopup : PopupPage
    {
        public TestNetWalletPopup(MainViewModel mainViewModel)
        {
            InitializeComponent();
            BindingContext = mainViewModel;
        }
    }
}
