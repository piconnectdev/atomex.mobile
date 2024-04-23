using atomex.ViewModels.CurrencyViewModels;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.Collectibles
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NftPage : ContentPage
    {
        public NftPage(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }
    }
}