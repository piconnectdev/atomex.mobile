﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Core;
using Atomex.Wallets.Abstract;
using RGPopup.Maui.Pages;
using static atomex.Models.SnackbarMessage;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex
{
    public enum TabNavigation
    {
        None,
        Portfolio,
        Exchange,
        Buy,
        Settings
    }

    public interface INavigationService
    {
        void GoToBuy(CurrencyConfig currencyConfig);
        void GoToExchange(CurrencyConfig currencyConfig);
        void ShowPage(Page page, TabNavigation tab = TabNavigation.None);
        void ClosePage(TabNavigation tab = TabNavigation.None);
        void RemovePreviousPage(TabNavigation tab);
        void ShowPopup(PopupPage popup, bool removePrevious = true);
        void ClosePopup();
        bool HasMultipleBottomSheets();
        void DisplaySnackBar(MessageType messageType, string text, string btnTxt = "OK", int duration = 3000, Func<Task> action = null, CancellationTokenSource cts = null);
        Task ShowAlert(string title, string text, string cancel);
        Task<bool> ShowAlert(string title, string text, string accept, string cancel);
        Task<string> DisplayActionSheet(string cancel, string[] actions, string title = null);
        void SetInitiatedPage(TabNavigation tab);
        Task ReturnToInitiatedPage(TabNavigation tab);
        Task ConnectDappByDeepLink(string qrCode);
        void AllowCamera();
    }
}
