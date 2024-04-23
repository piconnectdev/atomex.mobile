using atomex.ViewModels.CurrencyViewModels;
using RGPopup.Maui.Pages;
using Microsoft.Maui.Controls.Xaml;

namespace atomex.Views.Collectibles
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ManageCollectiblesBottomSheet : PopupPage
    {
        public ManageCollectiblesBottomSheet(CollectiblesViewModel collectiblesViewModel)
        {
            InitializeComponent();
            BindingContext = collectiblesViewModel;
        }
        
        public void OnClose()
        {
            if (BindingContext is CollectiblesViewModel)
            {
                var tezosTokensViewModel = (CollectiblesViewModel)BindingContext;
                if (tezosTokensViewModel.CloseActionSheetCommand.CanExecute(null))
                    tezosTokensViewModel.CloseActionSheetCommand.Execute(null);
            }
        }
    }
}