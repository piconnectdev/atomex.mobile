using atomex.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views
{
    public partial class DelegationInfoPage : ContentPage
    {
        public DelegationInfoPage(DelegationViewModel delegationViewModel)
        {
            InitializeComponent();
            BindingContext = delegationViewModel;
        }
    }
}