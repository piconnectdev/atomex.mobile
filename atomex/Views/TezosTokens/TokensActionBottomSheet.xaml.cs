﻿using atomex.ViewModels.CurrencyViewModels;
using RGPopup.Maui.Pages;

namespace atomex.Views.TezosTokens
{
    public partial class TokensActionBottomSheet : PopupPage
    {
        public TokensActionBottomSheet(TezosTokensViewModel tezosTokensViewModel)
        {
            InitializeComponent();
            BindingContext = tezosTokensViewModel;
        }

        public void OnClose()
        {
            if (BindingContext is TezosTokensViewModel)
            {
                var tezosTokensViewModel = (TezosTokensViewModel)BindingContext;
                if (tezosTokensViewModel.CloseActionSheetCommand.CanExecute(null))
                    tezosTokensViewModel.CloseActionSheetCommand.Execute(null);
            }
        }
    }
}
