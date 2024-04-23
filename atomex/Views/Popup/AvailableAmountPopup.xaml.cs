using atomex.ViewModels;
using atomex.ViewModels.CurrencyViewModels;
using RGPopup.Maui.Pages;

namespace atomex.Views.Popup
{
    public partial class AvailableAmountPopup : PopupPage
    {
        public AvailableAmountPopup(PortfolioViewModel portfolioViewModel)
        {
            InitializeComponent();
            BindingContext = portfolioViewModel;
        }

        public AvailableAmountPopup(CurrencyViewModel currencyViewModel)
        {
            InitializeComponent();
            BindingContext = currencyViewModel;
        }
    }
}
