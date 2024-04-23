using atomex.ViewModels.CurrencyViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.TezosTokens
{
    public partial class TokenPage : ContentPage
    {
        public TokenPage(TezosTokenViewModel tezosTokenViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokenViewModel;
        }
    }
}
