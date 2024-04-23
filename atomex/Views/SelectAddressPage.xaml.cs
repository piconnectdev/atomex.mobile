using atomex.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views
{
    public partial class SelectAddressPage : ContentPage
    {
        public SelectAddressPage(SelectAddressViewModel selectAddressViewModel)
        {
            BindingContext = selectAddressViewModel;
            InitializeComponent();
        }
    }
}
