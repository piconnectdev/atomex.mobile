using atomex.ViewModels.ConversionViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.CreateSwap
{
    public partial class SelectCurrencyPage : ContentPage
    {
        public SelectCurrencyPage(SelectCurrencyViewModel selectCurrencyViewModel)
        {
            InitializeComponent();
            BindingContext = selectCurrencyViewModel;
        }
    }
}
