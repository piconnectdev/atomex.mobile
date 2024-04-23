using atomex.ViewModels.ConversionViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.CreateSwap
{
    public partial class ExchangePage : ContentPage
    {
        public ExchangePage(ConversionViewModel conversionViewModel)
        {
            InitializeComponent();
            BindingContext = conversionViewModel;
        }
    }
}
