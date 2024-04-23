using atomex.ViewModels.TransactionViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views
{
    public partial class TransactionInfoPage : ContentPage
    {
        public TransactionInfoPage(TransactionViewModel transactionViewModel)
        {
            InitializeComponent();
            BindingContext = transactionViewModel;
        }
    }
}