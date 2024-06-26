﻿using System;
using atomex.ViewModels.SendViewModels;
using RGPopup.Maui.Pages;

namespace atomex.Views.Send
{
    public partial class SendingConfirmationBottomSheet : PopupPage
    {
        public SendingConfirmationBottomSheet(SendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        public SendingConfirmationBottomSheet(TezosTokensSendViewModel sendViewModel)
        {
            InitializeComponent();
            BindingContext = sendViewModel;
        }

        protected override void OnDisappearing()
        {
            if (BindingContext is IDisposable vm)
                vm.Dispose();
            
            if (BindingContext is SendViewModel)
            {
                var sendViewModel = (SendViewModel)BindingContext;
                if (sendViewModel.UndoConfirmStageCommand.CanExecute(null))
                    sendViewModel.UndoConfirmStageCommand.Execute(null);

                return;
            }

            var tezosTokenSendViewModel = (TezosTokensSendViewModel)BindingContext;
            if (tezosTokenSendViewModel.UndoConfirmStageCommand.CanExecute(null))
                tezosTokenSendViewModel.UndoConfirmStageCommand.Execute(null);
        }

        public void OnClose()
        {
            if (BindingContext is SendViewModel)
            {
                var sendViewModel = (SendViewModel)BindingContext;
                if (sendViewModel.CloseConfirmationCommand.CanExecute(null))
                    sendViewModel.CloseConfirmationCommand.Execute(null);

                return;
            }

            var tezosTokenSendViewModel = (TezosTokensSendViewModel)BindingContext;
            if (tezosTokenSendViewModel.CloseConfirmationCommand.CanExecute(null))
                tezosTokenSendViewModel.CloseConfirmationCommand.Execute(null);
        }
    }
}
