using atomex.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.CreateNewWallet
{
    public partial class WalletTypePage : ContentPage
    {

        public WalletTypePage()
        {
            InitializeComponent();
        }

        public WalletTypePage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            BindingContext = createNewWalletViewModel;
        }
    }
}
