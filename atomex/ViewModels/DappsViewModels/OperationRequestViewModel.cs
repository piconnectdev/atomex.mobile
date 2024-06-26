﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Atomex;
using Atomex.Blockchain.Tezos.Abstract;
using atomex.Common;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using atomex.Models;
using atomex.Resources;
using Atomex.Wallet.Tezos;
using Atomex.Wallets.Tezos;
using Netezos.Forging.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using DecimalExtensions = Atomex.Common.DecimalExtensions;
using Atomex.Wallets;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.ViewModels.DappsViewModels
{
    public abstract class BaseBeaconOperationViewModel : BaseViewModel, IDisposable
    {
        public int Id { get; set; }
        protected static string BaseCurrencyCode => "USD";
        public abstract string JsonStringOperation { get; }
        
        [Reactive] public IQuotesProvider QuotesProvider { get; set; }

        protected BaseBeaconOperationViewModel()
        {
            this.WhenAnyValue(vm => vm.QuotesProvider)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(quotesProvider =>
                {
                    quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
                    OnQuotesUpdatedEventHandler(quotesProvider, EventArgs.Empty);
                });
        }

        protected abstract void OnQuotesUpdatedEventHandler(object sender, EventArgs args);


        public void Dispose()
        {
            if (QuotesProvider != null)
                QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;
        }
    }

    public class TransactionContentViewModel : BaseBeaconOperationViewModel
    {
        [Reactive] public TransactionContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);

        public string TezosFormat =>
            DecimalExtensions.GetFormatWithPrecision((int) Math.Round(Math.Log10(TezosConfig.XtzDigitsMultiplier)));

        public decimal AmountInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Amount));
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        public string DestinationIcon => $"https://services.tzkt.io/v1/avatars/{Operation.Destination}";
        [Reactive] public string DestinationAlias { get; set; }
        [Reactive] public decimal AmountInBase { get; set; }
        [Reactive] public decimal FeeInBase { get; set; }
        [ObservableAsProperty] public string Entrypoint { get; }
        public string ExplorerUri { get; set; }

        public TransactionContentViewModel()
        {
            this.WhenAnyValue(vm => vm.Operation)
                .WhereNotNull()
                .SubscribeInMainThread(async operation =>
                {
                    DestinationAlias = operation.Destination.TruncateAddress();
                    var url = $"v1/accounts/{operation.Destination}?metadata=true";

                    try
                    {
                        using var response = await HttpHelper.GetAsync(
                            baseUri: "https://api.tzkt.io/",
                            relativeUri: url);

                        if (response.StatusCode != HttpStatusCode.OK) return;

                        var responseContent = await response
                            .Content
                            .ReadAsStringAsync();

                        var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        var alias = responseJObj?["metadata"]?["alias"]?.ToString();
                        if (alias != null)
                            DestinationAlias = alias;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }
                });

            this.WhenAnyValue(vm => vm.Operation)
                .WhereNotNull()
                .Select(operation =>
                    operation.Parameters == null ? "Transfer to " : $"Call {operation.Parameters.Entrypoint} in ")
                .ToPropertyExInMainThread(this, vm => vm.Entrypoint);
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            try
            {
                var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
                
                if (xtzQuote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    AmountInBase = AmountInTez * (xtzQuote?.Bid ?? 0m);
                    FeeInBase = FeeInTez * (xtzQuote?.Bid ?? 0m);
                });
                Log.Debug("Quotes updated for beacon TransactionContent operation {Id}", Id);
            }
            catch (Exception e)
            {
                Log.Error(e, "Update quotes error for beacon TransactionContent operation {Id}", Id);
            }
        }

        private ReactiveCommand<Unit, Unit> _openDestinationInExplorer;

        public ReactiveCommand<Unit, Unit> OpenDestinationInExplorer => _openDestinationInExplorer ??=
            ReactiveCommand.Create(() =>
            {
                if (Uri.TryCreate($"{ExplorerUri}{Operation.Destination}", UriKind.Absolute,
                        out var uri))
                    Launcher.OpenAsync(new Uri(uri.ToString()));
            });
    }

    public class RevealContentViewModel : BaseBeaconOperationViewModel
    {
        public RevealContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        [Reactive] public decimal FeeInBase { get; set; }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;

            try
            {
                var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
                if (xtzQuote == null) return;
                
                Device.InvokeOnMainThreadAsync(() =>
                    FeeInBase = FeeInTez * (xtzQuote?.Bid ?? 0m));
                Log.Debug("Quotes updated for beacon RevealContent operation {Id}", Id);
            }
            catch (Exception e)
            {
                Log.Error(e,"Update quotes error for beacon RevealContent operation {Id}", Id);
            }
            
        }
    }

    public class DelegationContentViewModel : BaseBeaconOperationViewModel
    {
        [Reactive] public DelegationContent Operation { get; set; }
        public override string JsonStringOperation => JsonConvert.SerializeObject(Operation, Formatting.Indented);
        public string DestinationIcon => $"https://services.tzkt.io/v1/avatars/{Operation.Delegate}";
        [Reactive] public string DestinationAlias { get; set; }
        public decimal FeeInTez => TezosConfig.MtzToTz(Convert.ToDecimal(Operation.Fee));
        [Reactive] public decimal FeeInBase { get; set; }
        public string ExplorerUri { get; set; }

        public DelegationContentViewModel()
        {
            this.WhenAnyValue(vm => vm.Operation)
                .WhereNotNull()
                .SubscribeInMainThread(async operation =>
                {
                    DestinationAlias = operation.Delegate.TruncateAddress();
                    var url = $"v1/accounts/{operation.Delegate}?metadata=true";

                    try
                    {
                        using var response = await HttpHelper.GetAsync(
                            baseUri: "https://api.tzkt.io/",
                            relativeUri: url);

                        if (response.StatusCode != HttpStatusCode.OK) return;

                        var responseContent = await response
                            .Content
                            .ReadAsStringAsync();

                        var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        var alias = responseJObj?["metadata"]?["alias"]?.ToString();
                        if (alias != null)
                            DestinationAlias = alias;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }
                });
        }

        private ReactiveCommand<Unit, Unit> _openDestinationInExplorer;

        public ReactiveCommand<Unit, Unit> OpenDestinationInExplorer => _openDestinationInExplorer ??=
            ReactiveCommand.Create(() =>
            {
                if (Uri.TryCreate($"{ExplorerUri}{Operation.Delegate}", UriKind.Absolute, out var uri))
                    Launcher.OpenAsync(new Uri(uri.ToString()));
            });

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;
            try
            {
                var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
                if (xtzQuote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                    FeeInBase = FeeInTez * (xtzQuote?.Bid ?? 0m));
                Log.Debug("Quotes updated for beacon DelegationContent operation {Id}", Id);

            }
            catch (Exception e)
            {
                Log.Error(e, "Update quotes error for beacon DelegationContent operation {Id}", Id);
            }
        }
    }

    public class OperationRequestViewModel : BaseViewModel, IDisposable
    {
        public string DappName { get; set; }
        public string DappLogo { get; set; }

        public WalletAddress ConnectedWalletAddress { get; set; }
        public string Title => string.Format(AppResources.DappRequestToConfirm, DappName);
        [Reactive] public OperationRequestTab SelectedTab { get; set; }
        [Reactive] public bool IsDetailsOpened { get; set; }

        public string TezosFormat =>
            DecimalExtensions.GetFormatWithPrecision((int) Math.Round(Math.Log10(TezosConfig.XtzDigitsMultiplier)));

        [Reactive] public IEnumerable<BaseBeaconOperationViewModel> Operations { get; set; }
        private IEnumerable<ManagerOperationContent> _initialOperations { get; set; }
        [Reactive] private byte[] _forgedOperations { get; set; }
        [Reactive] public string RawOperations { get; set; }
        [ObservableAsProperty] public string OperationsBytes { get; set; }
        [Reactive] public decimal TotalFees { get; set; }
        [Reactive] public decimal TotalFeesInBase { get; set; }
        [Reactive] public int TotalGasLimit { get; set; }
        [Reactive] public int TotalStorageLimit { get; set; }
        [Reactive] public decimal TotalGasFee { get; set; }

        public string TotalGasFeeString
        {
            get => TotalGasFee.ToString(CultureInfo.InvariantCulture);
            set
            {
                string temp = value.Replace(",", ".");
                if (!decimal.TryParse(
                        s: temp,
                        style: NumberStyles.AllowDecimalPoint,
                        provider: CultureInfo.InvariantCulture,
                        result: out var result))
                {
                    TotalGasFee = 0;
                }
                else
                {
                    TotalGasFee = result;

                    if (TotalGasFee > long.MaxValue)
                        TotalGasFee = long.MaxValue;
                }

                Device.InvokeOnMainThreadAsync(() => this.RaisePropertyChanged(nameof(TotalGasFee)));
            }
        }

        [Reactive] public decimal TotalGasFeeInBase { get; set; }
        [Reactive] public decimal TotalStorageFee { get; set; }

        public string TotalStorageFeeString
        {
            get => TotalStorageFee.ToString(CultureInfo.InvariantCulture);
            set
            {
                var temp = value.Replace(",", ".");
                if (!decimal.TryParse(
                        s: temp,
                        style: NumberStyles.AllowDecimalPoint,
                        provider: CultureInfo.InvariantCulture,
                        result: out var result))
                {
                    TotalStorageFee = 0;
                }
                else
                {
                    TotalStorageFee = result;

                    if (TotalStorageFee > long.MaxValue)
                        TotalStorageFee = long.MaxValue;
                }

                Device.InvokeOnMainThreadAsync(() => this.RaisePropertyChanged(nameof(TotalStorageFee)));
            }
        }

        [Reactive] public decimal TotalStorageFeeInBase { get; set; }
        [Reactive] public bool UseDefaultFee { get; set; }
        [Reactive] public bool AutofillError { get; set; }
        [Reactive] public IQuotesProvider QuotesProvider { get; set; }
        private TezosConfig Tezos { get; }
        private static string BaseCurrencyCode => "USD";
        private static string BaseCurrencyFormat => "$0.##";
        private int? _byteCost;
        private int _defaultOperationGasLimit;
        [ObservableAsProperty] public bool IsSending { get; }
        [ObservableAsProperty] public bool IsRejecting { get; }

        [Reactive] public string CopyButtonName { get; set; }
        [ObservableAsProperty] public bool IsCopied { get; }

        public OperationRequestViewModel(
            IEnumerable<ManagerOperationContent> operations,
            WalletAddress connectedAddress,
            int operationGasLimit,
            TezosConfig tezosConfig)
        {
            Tezos = tezosConfig;
            ConnectedWalletAddress = connectedAddress;
            _initialOperations = operations;
            _defaultOperationGasLimit = operationGasLimit;

            CopyCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsCopied);

            OnConfirmCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsSending);

            OnRejectCommand
                .IsExecuting
                .ToPropertyExInMainThread(this, vm => vm.IsRejecting);

            this.WhenAnyValue(vm => vm.QuotesProvider)
                .WhereNotNull()
                .Take(1)
                .SubscribeInMainThread(quotesProvider =>
                    quotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler);

            this.WhenAnyValue(vm => vm.Operations, vm => vm.QuotesProvider)
                .WhereAllNotNull()
                .Select(args => args.Item1.ToList())
                .SubscribeInMainThread(async operations =>
                {
                    TotalGasLimit = operations.Aggregate(0, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.Operation.GasLimit,
                        RevealContentViewModel revealOp => res + revealOp.Operation.GasLimit,
                        DelegationContentViewModel delegationOp => res + delegationOp.Operation.GasLimit,
                        _ => res
                    });

                    TotalStorageLimit = operations.Aggregate(0, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.Operation.StorageLimit,
                        RevealContentViewModel revealOp => res + revealOp.Operation.StorageLimit,
                        DelegationContentViewModel delegationOp => res + delegationOp.Operation.StorageLimit,
                        _ => res
                    });

                    TotalGasFee = operations.Aggregate(0m, (res, currentOp) => currentOp switch
                    {
                        TransactionContentViewModel transactionOp => res + transactionOp.FeeInTez,
                        RevealContentViewModel revealOp => res + revealOp.FeeInTez,
                        DelegationContentViewModel delegationOp => res + delegationOp.FeeInTez,
                        _ => res
                    });
                    TotalGasFeeString = TotalGasFee.ToString(CultureInfo.CurrentCulture);
                    this.RaisePropertyChanged(nameof(TotalGasFeeString));

                    const string url = "v1/protocols/current";
                    try
                    {
                        using var response = await HttpHelper.GetAsync(
                            baseUri: "https://api.tzkt.io/",
                            relativeUri: url);

                        if (response.StatusCode != HttpStatusCode.OK) return;

                        var responseContent = await response
                            .Content
                            .ReadAsStringAsync();

                        var responseJObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                        if (!int.TryParse(responseJObj?["constants"]?["byteCost"]?.ToString(), out var byteCost))
                            return;

                        _byteCost = byteCost;
                        TotalStorageFee = TezosConfig.MtzToTz(Convert.ToDecimal(byteCost)) * TotalStorageLimit;
                        TotalStorageFeeString = TotalStorageFee.ToString(CultureInfo.CurrentCulture);
                        this.RaisePropertyChanged(nameof(TotalStorageFeeString));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error during sending request to {Url}", url);
                    }
                });

            this.WhenAnyValue(vm => vm.TotalGasFee, vm => vm.TotalStorageFee)
                .WhereAllNotNull()
                .Throttle(TimeSpan.FromMilliseconds(1))
                .Select(values => values.Item1 + values.Item2)
                .SubscribeInMainThread(totalFees =>
                {
                    TotalFees = totalFees;
                    OnQuotesUpdatedEventHandler(QuotesProvider, EventArgs.Empty);
                });

            this.WhenAnyValue(vm => vm.TotalStorageLimit)
                .WhereNotNull()
                .Skip(1)
                .SubscribeInMainThread(totalStorageLimit =>
                {
                    if (_byteCost != null)
                    {
                        TotalStorageFee = TezosConfig.MtzToTz(Convert.ToDecimal(_byteCost)) * totalStorageLimit;
                        TotalStorageFeeString = TotalStorageFee.ToString(CultureInfo.CurrentCulture);
                        this.RaisePropertyChanged(nameof(TotalStorageFeeString));
                    }
                });

            this.WhenAnyValue(vm => vm.UseDefaultFee)
                .WhereNotNull()
                .Where(useDefaultFee => useDefaultFee)
                .SubscribeInMainThread(_ => { AutofillOperations(); });

            this.WhenAnyValue(vm => vm.TotalGasFee, vm => vm.TotalStorageLimit)
                .WhereAllNotNull()
                .Throttle(TimeSpan.FromMilliseconds(250))
                .Where(_ => !UseDefaultFee)
                .SubscribeInMainThread(_ => { AutofillOperations(); });

            this.WhenAnyValue(vm => vm._forgedOperations)
                .WhereNotNull()
                .Select(forgedOperations => forgedOperations.ToHexString())
                .ToPropertyExInMainThread(this, vm => vm.OperationsBytes);

            CopyButtonName = AppResources.CopyButton;
            SelectedTab = OperationRequestTab.Preview;
            UseDefaultFee = true;
        }

        private async void AutofillOperations()
        {
            AutofillError = false;
            
            var rpc = new Rpc(Tezos.RpcNodeUri);
            JObject head;
            try
            {
                head = await rpc
                    .GetHeader()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during querying rpc, {Message}", ex.Message);
                return;
            }

            try
            {
                var operations = _initialOperations.Select(op => op switch
                    {
                        TransactionContent txContent => new TransactionContent
                        {
                            Amount = txContent.Amount,
                            Destination = txContent.Destination,
                            Parameters = txContent.Parameters,
                            Source = txContent.Source,
                            Fee = UseDefaultFee ? 0 : txContent.Fee,
                            Counter = txContent.Counter,
                            GasLimit = UseDefaultFee ? _defaultOperationGasLimit : txContent.GasLimit,
                            StorageLimit = UseDefaultFee
                                ? DappsViewModel.StorageLimitPerOperation
                                : txContent.StorageLimit
                        },
                        RevealContent revealContent => new RevealContent
                        {
                            PublicKey = revealContent.PublicKey,
                            Source = revealContent.Source,
                            Fee = UseDefaultFee ? 0 : revealContent.Fee,
                            Counter = revealContent.Counter,
                            GasLimit = UseDefaultFee ? _defaultOperationGasLimit : revealContent.GasLimit,
                            StorageLimit = UseDefaultFee
                                ? DappsViewModel.StorageLimitPerOperation
                                : revealContent.StorageLimit
                        },
                        DelegationContent delegateContent => new DelegationContent
                        {
                            Delegate = delegateContent.Delegate,
                            Source = delegateContent.Source,
                            Fee = UseDefaultFee ? 0 : delegateContent.Fee,
                            Counter = delegateContent.Counter,
                            GasLimit = UseDefaultFee ? _defaultOperationGasLimit : delegateContent.GasLimit,
                            StorageLimit = UseDefaultFee 
                                ? DappsViewModel.StorageLimitPerOperation
                                : delegateContent.StorageLimit
                        },
                        _ => op
                    })
                    .ToList();
                
                var avgFee = Convert.ToInt64(TotalGasFee * TezosConfig.XtzDigitsMultiplier / operations.Count);

                if (!UseDefaultFee)
                {
                    foreach (var op in operations)
                    {
                        op.Fee = avgFee;
                    }
                }

                var error = await TezosOperationFiller.AutoFillAsync(
                        operations,
                        head["hash"]!.ToString(),
                        head["chain_id"]!.ToString(),
                        Tezos)
                    .ConfigureAwait(false);

                if (!UseDefaultFee)
                {
                    foreach (var op in operations)
                    {
                        op.Fee = avgFee;
                    }
                }

                if (error != null)
                    AutofillError = true;

                var branch = head["hash"]!.ToString();

                _forgedOperations = await TezosForge.ForgeAsync(
                    operations: operations,
                    branch: branch);

                var rawJObj = new JObject
                {
                    ["branch"] = branch,
                    ["contents"] = JArray.Parse(JsonConvert.SerializeObject(operations))
                };

                RawOperations = JsonConvert.SerializeObject(rawJObj, Formatting.Indented);

                if (!UseDefaultFee && Operations != null) return;

                var operationsViewModel = new ObservableCollection<BaseBeaconOperationViewModel>();
                foreach (var item in operations.Select((value, idx) => new {idx, value}))
                {
                    var operation = item.value;
                    var index = item.idx;

                    switch (operation)
                    {
                        case TransactionContent transactionOperation:
                            operationsViewModel.Add(new TransactionContentViewModel
                            {
                                Id = index + 1,
                                Operation = transactionOperation,
                                QuotesProvider = QuotesProvider,
                                ExplorerUri = Tezos.AddressExplorerUri
                            });
                            break;
                        case RevealContent revealOperation:
                            operationsViewModel.Add(new RevealContentViewModel
                            {
                                Id = index + 1,
                                Operation = revealOperation,
                                QuotesProvider = QuotesProvider,
                            });
                            break;
                        case DelegationContent delegationOperation:
                            operationsViewModel.Add(new DelegationContentViewModel
                            {
                                Id = index + 1,
                                Operation = delegationOperation,
                                QuotesProvider = QuotesProvider,
                                ExplorerUri = Tezos.AddressExplorerUri
                            });
                            break;
                    }
                }

                if (Operations != null)
                {
                    foreach (var op in Operations)
                    {
                        if (op is IDisposable disposable)
                            disposable.Dispose();
                    }
                }

                _initialOperations = operations;
                Operations = operationsViewModel;
            }
            catch (Exception e)
            {
                Log.Error(e, "Autofill operations error");
            }
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not IQuotesProvider quotesProvider)
                return;
            
            try
            {
                var xtzQuote = quotesProvider.GetQuote(TezosConfig.Xtz, BaseCurrencyCode);
                if (xtzQuote == null) return;

                Device.InvokeOnMainThreadAsync(() =>
                {
                    TotalGasFeeInBase = TotalGasFee * (xtzQuote?.Bid ?? 0m);
                    TotalStorageFeeInBase = TotalStorageFee * (xtzQuote?.Bid ?? 0m);
                    TotalFeesInBase = TotalFees * (xtzQuote?.Bid ?? 0m);
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "Update quotes error for operation request");
            }
        }

        public Func<byte[], Task> OnConfirm { get; set; }
        public Func<Task> OnReject { get; set; }

        private ReactiveCommand<Unit, Unit> _onConfirmCommand;

        public ReactiveCommand<Unit, Unit> OnConfirmCommand =>
            _onConfirmCommand ??= ReactiveCommand.CreateFromTask(async () =>
            {
                if (_forgedOperations != null)
                    await OnConfirm(_forgedOperations);
            });

        private ReactiveCommand<Unit, Unit> _onRejectCommand;

        public ReactiveCommand<Unit, Unit> OnRejectCommand =>
            _onRejectCommand ??= ReactiveCommand.CreateFromTask(async () => await OnReject());

        private ReactiveCommand<string, Unit> _changeTabCommand;

        public ReactiveCommand<string, Unit> ChangeTabCommand => _changeTabCommand ??=
            ReactiveCommand.Create<string>(value =>
            {
                Enum.TryParse(value, out OperationRequestTab selectedTab);
                SelectedTab = selectedTab;
            });

        private ReactiveCommand<string, Unit> _copyCommand;

        public ReactiveCommand<string, Unit> CopyCommand => _copyCommand ??= ReactiveCommand.CreateFromTask<string>(
            async data =>
            {
                try
                {
                    CopyButtonName = AppResources.Copied;
                    await Clipboard.SetTextAsync(data);
                    await Task.Delay(1500);
                    CopyButtonName = AppResources.CopyButton;
                }
                catch (Exception e)
                {
                    CopyButtonName = AppResources.CopyButton;
                    Log.Error(e, "Copy to clipboard error");
                }
            });

        private ReactiveCommand<Unit, Unit> _onOpenDetailsCommand;

        public ReactiveCommand<Unit, Unit> OnOpenDetailsCommand =>
            _onOpenDetailsCommand ??= ReactiveCommand.Create(() => { IsDetailsOpened = !IsDetailsOpened; });

        public void Dispose()
        {
            if (QuotesProvider != null)
                QuotesProvider.QuotesUpdated -= OnQuotesUpdatedEventHandler;

            foreach (var operation in Operations)
            {
                operation.Dispose();
            }
        }
    }
}