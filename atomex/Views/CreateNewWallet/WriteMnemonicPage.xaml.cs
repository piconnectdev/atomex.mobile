using System;
using atomex.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.CreateNewWallet
{
    public partial class WriteMnemonicPage : ContentPage
    {
        public WriteMnemonicPage()
        {
            InitializeComponent();
        }
        public WriteMnemonicPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            BindingContext = createNewWalletViewModel;
        }

        private void OnPickerFocused(object sender, FocusEventArgs args)
        {
            PickerFrame.HasShadow = args.IsFocused;
        }

        private void OnPickerClicked(object sender, EventArgs args)
        {
            LanguagesPicker.Focus();
        }

        private void OnEditorFocused(object sender, FocusEventArgs args)
        {
            EditorFrame.HasShadow = args.IsFocused;

            if (args.IsFocused)
            {
                // TODO Xamarin.Forms.Device.StartTimer is no longer supported. Use Microsoft.Maui.Dispatching.DispatcherExtensions.StartTimer instead. For more details see https://learn.microsoft.com/en-us/dotnet/maui/migration/forms-projects#device-changes
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, Editor.Height, true);
                    return false;
                });
            }
            else
            {
                // TODO Xamarin.Forms.Device.StartTimer is no longer supported. Use Microsoft.Maui.Dispatching.DispatcherExtensions.StartTimer instead. For more details see https://learn.microsoft.com/en-us/dotnet/maui/migration/forms-projects#device-changes
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, 0, true);
                    return false;
                });
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var editor = sender as Editor;
            editor.Text = args.NewTextValue.ToLower();
        }

        protected override void OnDisappearing()
        {
            var vm = (CreateNewWalletViewModel)BindingContext;
            if (vm.ClearWarningCommand.CanExecute(null))
            {
                vm.ClearWarningCommand.Execute(null);
            }
            base.OnDisappearing();
        }
    }
}
