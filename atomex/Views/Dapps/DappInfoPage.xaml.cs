using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using atomex.ViewModels.DappsViewModels;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.Dapps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DappInfoPage : ContentPage
    {
        public DappInfoPage(DappViewModel dapp)
        {
            InitializeComponent();
            BindingContext = dapp;
        }
    }
}