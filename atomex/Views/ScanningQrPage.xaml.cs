using atomex.ViewModels;
using atomex.ViewModels.ConversionViewModels;
using atomex.ViewModels.DappsViewModels;
using atomex.ViewModels.SendViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views
{
    public partial class ScanningQrPage : ContentPage
    {
        public ScanningQrPage(SelectAddressViewModel selectAddressViewModel)
        {
            InitializeComponent();
            BindingContext = selectAddressViewModel;
        }

        public ScanningQrPage(SelectOutputsViewModel selectAddressViewModel)
        {
            InitializeComponent();
            BindingContext = selectAddressViewModel;
        }
    }
}
