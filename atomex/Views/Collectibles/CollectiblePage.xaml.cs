using atomex.ViewModels.CurrencyViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.Collectibles
{
    public partial class CollectiblePage : ContentPage
    {
        public CollectiblePage(CollectibleViewModel collectibleViewModel)
        {
            InitializeComponent();
            BindingContext = collectibleViewModel;
        }
    }
}