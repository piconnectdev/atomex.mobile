﻿using System;
using System.Threading.Tasks;
using atomex.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Views.CreateNewWallet
{
    public partial class WriteDerivedKeyPasswordPage : ContentPage
    {

        public WriteDerivedKeyPasswordPage()
        {
            InitializeComponent();
        }

        public WriteDerivedKeyPasswordPage(CreateNewWalletViewModel createNewWalletViewModel)
        {
            InitializeComponent();
            BindingContext = createNewWalletViewModel;
        }

        private void PasswordEntryFocused(object sender, FocusEventArgs args)
        {
            PasswordFrame.HasShadow = args.IsFocused;

            if (args.IsFocused)
            {
                // TODO Xamarin.Forms.Device.StartTimer is no longer supported. Use Microsoft.Maui.Dispatching.DispatcherExtensions.StartTimer instead. For more details see https://learn.microsoft.com/en-us/dotnet/maui/migration/forms-projects#device-changes
                Device.StartTimer(TimeSpan.FromSeconds(0.25), () =>
                {
                    Page.ScrollToAsync(0, PasswordEntry.Height, true);
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

        private async void OnPasswordTextChanged(object sender, TextChangedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.NewTextValue))
            {
                if (!PasswordHint.IsVisible)
                {
                    PasswordHint.IsVisible = true;
                    PasswordHint.Text = PasswordEntry.Placeholder;

                    _ = PasswordHint.FadeTo(1, 500, Easing.Linear);
                    _ = PasswordEntry.TranslateTo(0, 10, 500, Easing.CubicOut);
                    _ = PasswordHint.TranslateTo(0, -15, 500, Easing.CubicOut);
                }
            }
            else
            {
                await Task.WhenAll(
                    PasswordHint.FadeTo(0, 500, Easing.Linear),
                    PasswordEntry.TranslateTo(0, 0, 500, Easing.CubicOut),
                    PasswordHint.TranslateTo(0, -10, 500, Easing.CubicOut)
                );

                PasswordHint.IsVisible = false;
            }
        }
    }
}
