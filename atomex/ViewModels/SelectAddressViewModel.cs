﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Models;
using atomex.Resources;
using atomex.Views;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.ViewModels;
using atomex.ViewModels.Abstract;
using Atomex.Wallet.Abstract;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using ZXing;
using static atomex.Models.Message;
using Atomex.Wallets.Abstract;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.ViewModels
{
    public class SelectAddressViewModel : BaseViewModel
    {
        private INavigationService _navigationService;
        private CurrencyConfig _currency;

        public Action<SelectAddressViewModel, WalletAddressViewModel> ConfirmAction { get; set; }
        public SelectAddressMode SelectAddressMode { get; set; }
        public SelectAddressFrom SelectAddressFrom { get; set; }
        public TabNavigation TabNavigation { get; set; }
        private ObservableCollection<WalletAddressViewModel> _initialMyAddresses;
        [Reactive] public ObservableCollection<WalletAddressViewModel> MyAddresses { get; set; }
        [Reactive] public string SearchPattern { get; set; }
        [Reactive] public string ToAddress { get; set; }
        [Reactive] public bool SortIsAscending { get; set; }
        [Reactive] public bool SortByBalance { get; set; }
        [Reactive] public string SortButtonName { get; set; }
        [Reactive] public string Title { get; set; }
        [Reactive] public WalletAddressViewModel SelectedAddress { get; set; }

        [Reactive] public Result ScanResult { get; set; }
        [Reactive] public bool IsScanning { get; set; }
        [Reactive] public bool IsAnalyzing { get; set; }

        [Reactive] public bool IsMyAddressesTab { get; set; }
        [Reactive] public string ToolbarIcon { get; set; }
        [Reactive] public ReactiveCommand<Unit, Unit> ToolbarCommand { get; set; }

        [Reactive] public Message Message { get; set; }
        private bool _pasted;

        public SelectAddressViewModel()
        {
        }

        public SelectAddressViewModel(
            IAccount account,
            CurrencyConfig currency,
            INavigationService navigationService,
            TabNavigation tab = TabNavigation.Portfolio,
            SelectAddressMode mode = SelectAddressMode.ReceiveTo,
            string selectedAddress = null,
            int selectedTokenId = 0,
            string tokenContract = null)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            Message = new Message();

            TabNavigation = tab;
            SelectAddressMode = mode;
            IsMyAddressesTab = false;
            SelectAddressFrom = SelectAddressFrom.Init;
            SortButtonName = SortByBalance
                ? AppResources.SortByBalanceButton
                : AppResources.SortByDateButton;

            this.WhenAnyValue(
                    vm => vm.IsMyAddressesTab,
                    vm => vm.SelectAddressMode)
                .SubscribeInMainThread(value =>
                {
                    ToolbarIcon = SelectAddressMode == SelectAddressMode.ReceiveTo && !IsMyAddressesTab ||
                                  SelectAddressMode == SelectAddressMode.EnterExternalAddress
                        ? "ic_qr"
                        : "ic_search";

                    ToolbarCommand = SelectAddressMode == SelectAddressMode.ReceiveTo && !IsMyAddressesTab ||
                                     SelectAddressMode == SelectAddressMode.EnterExternalAddress
                        ? ScanCommand
                        : SearchCommand;
                });

            this.WhenAnyValue(
                    vm => vm.SortByBalance,
                    vm => vm.SortIsAscending,
                    vm => vm.SearchPattern)
                .SubscribeInMainThread(value =>
                {
                    var (item1, item2, item3) = value;

                    if (MyAddresses == null) return;

                    var myAddresses = new ObservableCollection<WalletAddressViewModel>(
                        _initialMyAddresses
                            .Where(addressViewModel => addressViewModel.WalletAddress.Address
                                .ToLower()
                                .Contains(item3?.ToLower() ?? string.Empty)));

                    if (!item1)
                    {
                        var myAddressesList = myAddresses.ToList();
                        if (item2)
                        {
                            myAddressesList.Sort((a1, a2) =>
                            {
                                var typeResult = a1.WalletAddress.KeyType.CompareTo(a2.WalletAddress.KeyType);

                                if (typeResult != 0)
                                    return typeResult;

                                var accountResult =
                                    a1.WalletAddress.KeyIndex.Account.CompareTo(a2.WalletAddress.KeyIndex.Account);

                                if (accountResult != 0)
                                    return accountResult;

                                var chainResult =
                                    a1.WalletAddress.KeyIndex.Chain.CompareTo(a2.WalletAddress.KeyIndex.Chain);

                                return chainResult != 0
                                    ? chainResult
                                    : a1.WalletAddress.KeyIndex.Index.CompareTo(a2.WalletAddress.KeyIndex.Index);
                            });
                        }
                        else
                        {
                            myAddressesList.Sort((a2, a1) =>
                            {
                                var typeResult = a1.WalletAddress.KeyType.CompareTo(a2.WalletAddress.KeyType);

                                if (typeResult != 0)
                                    return typeResult;

                                var accountResult =
                                    a1.WalletAddress.KeyIndex.Account.CompareTo(a2.WalletAddress.KeyIndex.Account);

                                if (accountResult != 0)
                                    return accountResult;

                                var chainResult =
                                    a1.WalletAddress.KeyIndex.Chain.CompareTo(a2.WalletAddress.KeyIndex.Chain);

                                return chainResult != 0
                                    ? chainResult
                                    : a1.WalletAddress.KeyIndex.Index.CompareTo(a2.WalletAddress.KeyIndex.Index);
                            });
                        }

                        MyAddresses = new ObservableCollection<WalletAddressViewModel>(myAddressesList);
                    }
                    else
                    {
                        MyAddresses = new ObservableCollection<WalletAddressViewModel>(item2
                            ? myAddresses.OrderBy(addressViewModel => addressViewModel.Balance)
                            : myAddresses.OrderByDescending(addressViewModel => addressViewModel.Balance));
                    }
                });

            this.WhenAnyValue(vm => vm.ToAddress)
                .Where(_ => !_pasted)
                .SubscribeInMainThread(_ =>
                {
                    Message.Text = string.Empty;
                    this.RaisePropertyChanged(nameof(Message));
                });

            var onlyAddressesWithBalances =
                SelectAddressMode is SelectAddressMode.SendFrom;

            var addresses = AddressesHelper
                .GetReceivingAddressesAsync(
                    account: account,
                    currency: currency,
                    tokenContract: tokenContract,
                    tokenId: selectedTokenId)
                .WaitForResult()
                .Where(address => !onlyAddressesWithBalances || address.Balance != 0)
                .OrderByDescending(address => address.Balance);

            MyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);
            _initialMyAddresses = new ObservableCollection<WalletAddressViewModel>(addresses);

            SelectedAddress = selectedAddress != null
                ? MyAddresses.FirstOrDefault(vm =>
                    vm.Address == selectedAddress && vm.TokenId == selectedTokenId)
                : SelectAddressMode is SelectAddressMode.SendFrom or SelectAddressMode.Connect
                    ? SelectDefaultAddress()
                    : null;
            Title = SelectAddressMode == SelectAddressMode.ReceiveTo
                ? AppResources.ReceiveTo
                : SelectAddressMode == SelectAddressMode.SendFrom
                    ? AppResources.SendFrom
                    : SelectAddressMode == SelectAddressMode.EnterExternalAddress
                        ? AppResources.EnterAnExternalAddress
                        : SelectAddressMode == SelectAddressMode.ChangeRedeemAddress
                            ? AppResources.ChangeRedeemAddress
                            : SelectAddressMode == SelectAddressMode.Connect
                                ? AppResources.AddressToConnect
                                : string.Format(AppResources.MyCurrencyAddresses, _currency.DisplayedName);
        }

        public WalletAddressViewModel SelectDefaultAddress()
        {
            if (_currency is TezosConfig or EthereumConfig)
            {
                var activeAddressViewModel = MyAddresses
                    .Where(vm => vm.HasActivity && vm.AvailableBalance > 0)
                    .MaxByOrDefault(vm => vm.AvailableBalance);

                if (activeAddressViewModel != null)
                {
                    SelectedAddress = activeAddressViewModel;
                }
                else
                {
                    SelectedAddress = MyAddresses.FirstOrDefault(vm => vm.IsFreeAddress) ??
                                      MyAddresses.FirstOrDefault();
                }
            }
            else
            {
                SelectedAddress = MyAddresses.FirstOrDefault(vm => vm.IsFreeAddress) ?? MyAddresses.FirstOrDefault();
            }

            return SelectedAddress;
        }

        private ReactiveCommand<Unit, Unit> _changeSortTypeCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortTypeCommand => _changeSortTypeCommand ??=
            (_changeSortTypeCommand = ReactiveCommand.Create(() =>
            {
                SortByBalance = !SortByBalance;
                SortButtonName = SortByBalance
                    ? AppResources.SortByBalanceButton
                    : AppResources.SortByDateButton;
            }));

        private ReactiveCommand<Unit, Unit> _changeSortDirectionCommand;

        public ReactiveCommand<Unit, Unit> ChangeSortDirectionCommand => _changeSortDirectionCommand ??=
            (_changeSortDirectionCommand = ReactiveCommand.Create(() => { SortIsAscending = !SortIsAscending; }));

        private ICommand _confirmCommand;
        public ICommand ConfirmCommand => _confirmCommand ??= new Command(ConfirmExternalAddress);

        private ICommand _selectAddressCommand;

        public ICommand SelectAddressCommand => _selectAddressCommand ??= new Command(() =>
        {
            ConfirmAction?.Invoke(this, SelectedAddress);
        });

        private ReactiveCommand<Unit, Unit> _pasteCommand;

        public ReactiveCommand<Unit, Unit> PasteCommand =>
            _pasteCommand ??= (_pasteCommand = ReactiveCommand.CreateFromTask(OnPasteButtonClicked));

        private ReactiveCommand<WalletAddressViewModel, Unit> _copyCommand;

        public ReactiveCommand<WalletAddressViewModel, Unit> CopyCommand =>
            _copyCommand ??=
                (_copyCommand = ReactiveCommand.CreateFromTask<WalletAddressViewModel>(OnCopyButtonClicked));

        private ReactiveCommand<Unit, Unit> _scanCommand;

        public ReactiveCommand<Unit, Unit> ScanCommand =>
            _scanCommand ??= (_scanCommand = ReactiveCommand.CreateFromTask(OnScanButtonClicked));

        private ReactiveCommand<Unit, Unit> _searchCommand;

        public ReactiveCommand<Unit, Unit> SearchCommand =>
            _searchCommand ??= (_searchCommand = ReactiveCommand.Create(OnSearchButtonClicked));

        private ReactiveCommand<Unit, Unit> _clearToAddressCommand;

        public ReactiveCommand<Unit, Unit> ClearToAddressCommand =>
            _clearToAddressCommand ??= (_clearToAddressCommand = ReactiveCommand.Create(() =>
            {
                ToAddress = string.Empty;
            }));

        private ReactiveCommand<Unit, Unit> _clearSearchAddressCommand;

        public ReactiveCommand<Unit, Unit> ClearSearchAddressCommand =>
            _clearSearchAddressCommand ??= (_clearSearchAddressCommand = ReactiveCommand.Create(() =>
            {
                SearchPattern = string.Empty;
            }));

        private ReactiveCommand<bool, Unit> _changeAddressesTabCommand;

        public ReactiveCommand<bool, Unit> ChangeAddressesTabCommand =>
            _changeAddressesTabCommand ??= (_changeAddressesTabCommand = ReactiveCommand.Create<bool>((value) =>
            {
                IsMyAddressesTab = value;
            }));

        private ICommand _scanResultCommand;

        public ICommand ScanResultCommand =>
            _scanResultCommand ??= new Command(OnScanResult);

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??= new Command(() =>
        {
            SearchPattern = string.Empty;
            SelectAddressFrom = SelectAddressFrom == SelectAddressFrom.ChangeSearch
                ? SelectAddressFrom.Change
                : SelectAddressFrom.Init;
        });

        private ICommand _validateAddressCommand;

        public ICommand ValidateAddressCommand => _validateAddressCommand ??= new Command(() =>
        {
            ValidateAddress(ToAddress);
        });

        public void SetAddressMode(SelectAddressMode selectAddressMode)
        {
            SelectAddressMode = selectAddressMode;
            this.RaisePropertyChanged(nameof(SelectAddressMode));
        }

        protected async void OnCopyClicked(string value)
        {
            await Device.InvokeOnMainThreadAsync(() =>
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Clipboard.SetTextAsync(value);
                    _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular,
                        AppResources.AddressCopied);
                }
                else
                {
                    _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError,
                        AppResources.AcceptButton);
                }
            });
        }

        private void OnScanResult()
        {
            IsScanning = false;
            IsAnalyzing = false;
            
            if (ScanResult == null)
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.IncorrectQrCodeFormat,
                    AppResources.AcceptButton);
                Device.BeginInvokeOnMainThread(() => { _navigationService?.ClosePage(TabNavigation.Portfolio); });

                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                int indexOfChar = ScanResult.Text.IndexOf(':');
                if (indexOfChar == -1)
                    ToAddress = ScanResult.Text;
                else
                    ToAddress = ScanResult.Text.Substring(indexOfChar + 1);

                _navigationService?.ClosePage(TabNavigation.Portfolio);

                ConfirmExternalAddress();
            });
        }

        private async Task OnScanButtonClicked()
        {
            PermissionStatus permissions = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (permissions != PermissionStatus.Granted)
                permissions = await Permissions.RequestAsync<Permissions.Camera>();
            if (permissions != PermissionStatus.Granted)
                return;

            IsScanning = true;
            IsAnalyzing = true;

            _navigationService?.ShowPage(new ScanningQrPage(this), TabNavigation);
        }

        private void OnSearchButtonClicked()
        {
            SelectAddressFrom = SelectAddressFrom == SelectAddressFrom.Change
                ? SelectAddressFrom.ChangeSearch
                : SelectAddressFrom.InitSearch;

            _navigationService?.ShowPage(new SearchAddressPage(this), TabNavigation);
        }

        private async Task OnPasteButtonClicked()
        {
            if (Clipboard.HasText)
            {
                _pasted = true;
                var text = await Clipboard.GetTextAsync();
                ToAddress = text;
                ValidateAddress(ToAddress);
            }
            else
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.EmptyClipboard,
                    AppResources.AcceptButton);
            }

            _pasted = false;
        }

        private async Task OnCopyButtonClicked(WalletAddressViewModel address)
        {
            if (address != null)
            {
                await Clipboard.SetTextAsync(address.Address);
                _navigationService?.DisplaySnackBar(SnackbarMessage.MessageType.Regular, AppResources.AddressCopied);
            }
            else
            {
                _navigationService?.ShowAlert(AppResources.Error, AppResources.CopyError, AppResources.AcceptButton);
            }
        }

        private void ValidateAddress(string address)
        {
            try
            {
                if (_currency.IsValidAddress(address))
                    return;

                Message.Type = MessageType.Error;
                Message.RelatedElement = RelatedTo.Address;
                Message.Text = AppResources.InvalidAddressError;
                this.RaisePropertyChanged(nameof(Message));
            }
            catch (Exception e)
            {
                Log.Error(e, "Validate address error");
            }
        }

        private void ConfirmExternalAddress()
        {
            try
            {
                if (!_currency.IsValidAddress(ToAddress))
                {
                    Message.Type = MessageType.Error;
                    Message.RelatedElement = RelatedTo.Address;
                    Message.Text = AppResources.InvalidAddressError;
                    this.RaisePropertyChanged(nameof(Message));

                    return;
                }

                var selectedAddress = new WalletAddressViewModel
                {
                    Address = ToAddress,
                    AvailableBalance = 0,
                    TokenId = 0
                };

                ConfirmAction?.Invoke(this, selectedAddress);
            }
            catch (Exception e)
            {
                Log.Error(e, "Confirm external address error");
            }
        }
        
        private ICommand _closeBottomSheetCommand;

        public ICommand CloseBottomSheetCommand => _closeBottomSheetCommand ??= new Command(() =>
            _navigationService?.ClosePopup());
    }
}