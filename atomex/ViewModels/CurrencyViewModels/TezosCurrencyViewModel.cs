﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using atomex.Common;
using atomex.Views;
using atomex.Views.Delegate;
using Atomex;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Tzkt;
using Atomex.Core;
using atomex.ViewModels.DappsViewModels;
using Atomex.Wallet;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Atomex.Wallets.Abstract;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.ViewModels.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public TezosConfig Tezos => Currency as TezosConfig;
        [Reactive] public ObservableCollection<DelegationViewModel> Delegations { get; set; }
        [Reactive] public DelegationViewModel SelectedDelegation { get; set; }
        [Reactive] public TezosTokensViewModel TezosTokensViewModel { get; set; }
        [Reactive] public CollectiblesViewModel CollectiblesViewModel { get; set; }
        [Reactive] public DappsViewModel DappsViewModel { get; set; }

        private DelegateViewModel _delegateViewModel;
        [Reactive] public bool IsDelegationsLoading { get; set; }
        [Reactive] public int QtyDisplayedDelegations { get; set; }

        public TezosCurrencyViewModel(
            IAtomexApp app,
            CurrencyConfig currency,
            INavigationService navigationService)
            : base(app, currency, navigationService)
        {
            Delegations = new ObservableCollection<DelegationViewModel>();

            this.WhenAnyValue(vm => vm.SelectedDelegation)
                .WhereNotNull()
                .SubscribeInMainThread(d =>
                {
                    NavigationService?.SetInitiatedPage(TabNavigation.Portfolio);
                    if (d.Baker != null)
                        NavigationService?.ShowPage(new DelegationInfoPage(d), TabNavigation.Portfolio);
                    else
                        ChangeBaker(SelectedDelegation);

                    SelectedDelegation = null;
                });

            _delegateViewModel = new DelegateViewModel(App, NavigationService);
            TezosTokensViewModel = new TezosTokensViewModel(App, NavigationService);
            CollectiblesViewModel = new CollectiblesViewModel(App, NavigationService);
            DappsViewModel = new DappsViewModel(App, NavigationService);
            
            _ = Task.Run(LoadDelegationInfoAsync);
        }

        private async Task LoadDelegationInfoAsync()
        {
            try
            {
                IsDelegationsLoading = true;

                var addresses = await App.Account
                    .GetUnspentAddressesAsync(Tezos.Name)
                    .ConfigureAwait(false);

                var rpc = new Rpc(Tezos.RpcNodeUri);

                var delegations = new List<DelegationViewModel>();

                var tzktApi = new TzktApi(Tezos);
                var head = await tzktApi.GetHeadLevelAsync();
                var headLevel = head.Value;

                var currentCycle = App.Account.Network == Network.MainNet
                    ? Math.Floor((headLevel - 1) / 4096)
                    : Math.Floor((headLevel - 1) / 2048);

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccount(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData["delegate"]?.ToString();

                    if (string.IsNullOrEmpty(@delegate))
                    {
                        delegations.Add(new DelegationViewModel
                        {
                            Baker = null,
                            Address = wa.Address,
                            ExplorerUri = Tezos.BbUri,
                            Balance = wa.Balance,
                            DelegationTime = DateTime.Today,
                            Status = DelegationStatus.NotDelegated,
                            CopyAddress = CopyAddress,
                            ChangeBaker = ChangeBaker,
                            Undelegate = Undelegate,
                            ShowActionSheet = ShowActionBottomSheet,
                            CloseActionSheet = CloseActionBottomSheet
                        });

                        continue;
                    }

                    var baker = await BbApi
                        .GetBaker(@delegate, App.Account.Network)
                        .ConfigureAwait(false) ?? new BakerData {Address = @delegate};

                    var account = await tzktApi.GetAccountByAddressAsync(wa.Address);

                    var txCycle = App.Account.Network == Network.MainNet
                        ? Math.Floor((account.Value.DelegationLevel - 1) / 4096)
                        : Math.Floor((account.Value.DelegationLevel - 1) / 2048);

                    delegations.Add(new DelegationViewModel
                    {
                        Baker = baker,
                        Address = wa.Address,
                        ExplorerUri = Tezos.BbUri,
                        Balance = wa.Balance,
                        DelegationTime = account.Value.DelegationTime,
                        Status = currentCycle - txCycle < 2 ? DelegationStatus.Pending :
                            currentCycle - txCycle < 7 ? DelegationStatus.Confirmed :
                            DelegationStatus.Active,
                        CopyAddress = CopyAddress,
                        ChangeBaker = ChangeBaker,
                        Undelegate = Undelegate,
                        ShowActionSheet = ShowActionBottomSheet,
                        CloseActionSheet = CloseActionBottomSheet
                    });
                }

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    Delegations = new ObservableCollection<DelegationViewModel>(delegations);
                    QtyDisplayedDelegations = Delegations.Count;
                });
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Load delegation info canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "Load delegation info error");
            }
            finally
            {
                IsDelegationsLoading = false;
            }
        }

        private void ChangeBaker(DelegationViewModel delegation)
        {
            CloseActionBottomSheet();
            _delegateViewModel?.InitializeWith(delegation);
            NavigationService?.ShowPage(new DelegatePage(_delegateViewModel), TabNavigation.Portfolio);
        }

        private void Undelegate(DelegationViewModel delegation)
        {
            CloseActionBottomSheet();
            NavigationService?.SetInitiatedPage(TabNavigation.Portfolio);
            _delegateViewModel?.Undelegate(delegation.Address);
        }

        private void ShowActionBottomSheet(DelegationViewModel delegation)
        {
            NavigationService?.ShowPopup(new DelegationActionBottomSheet(delegation));
        }

        private void CloseActionBottomSheet()
        {
            NavigationService?.ClosePopup();
        }

        //protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        //{
        //    try
        //    {
        //        if (Currency?.Name != args?.Currency) return;

        //        await Task.Run(async () =>
        //        {
        //            await UpdateBalanceAsync();
        //            await LoadTransactionsAsync();
        //            await LoadDelegationInfoAsync();
        //        });
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Error(e, "Account balance updated event handler error");
        //    }
        //}

        public override void Reset()
        {
            CollectiblesViewModel?.Reset();
            TezosTokensViewModel?.Reset();
            base.Reset();
        }
    }
}