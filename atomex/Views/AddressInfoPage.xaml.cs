using atomex.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views
{
    public partial class AddressInfoPage : ContentPage
    {
        public AddressInfoPage(AddressViewModel addressInfoViewModel)
        {
            InitializeComponent();
            BindingContext = addressInfoViewModel;
        }
    }
}
