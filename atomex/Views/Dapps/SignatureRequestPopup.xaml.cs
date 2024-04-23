using atomex.ViewModels.DappsViewModels;
using RGPopup.Maui.Pages;
using Microsoft.Maui.Controls.Xaml;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SignatureRequestPopup : PopupPage
    {
        public SignatureRequestPopup(SignatureRequestViewModel signatureRequestViewModel)
        {
            InitializeComponent();
            BindingContext = signatureRequestViewModel;
        }
        
        protected override bool OnBackgroundClicked()
        {
            if (BindingContext is SignatureRequestViewModel)
            {
                var vm = (SignatureRequestViewModel)BindingContext;
                vm?.OnRejectCommand.Execute();
            }
            
            return true;
        }   
    }
}