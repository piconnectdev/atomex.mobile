using atomex.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.BuyCurrency
{
    public partial class BuyPage : ContentPage
    {
        public BuyPage()
        {
            InitializeComponent();
        }

        public BuyPage(BuyViewModel buyViewModel)
        {
            InitializeComponent();
            BindingContext = buyViewModel;
        }
    }
}
