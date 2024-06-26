﻿using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using atomex.Common;
using atomex.Resources;
using atomex.Views;
using Atomex;
using Atomex.Common;
using Atomex.LiteDb;
using Atomex.Wallet;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using FileSystem = Atomex.Common.FileSystem;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.ViewModels
{
    public class UnlockViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private readonly INavigationService _navigationService;
        private MainViewModel _mainViewModel;

        [Reactive] public string WalletName { get; set; }
        [Reactive] public SecureString StoragePassword { get; set; }
        [Reactive] public SecureString StoragePasswordConfirmation { get; set; }
        [Reactive] public bool IsEnteredStoragePassword { get; set; }
        [Reactive] public string Header { get; set; }
        [Reactive] public bool IsPinExist { get; set; }
        [Reactive] public bool IsLoading { get; set; }
        [Reactive] public bool IsLocked { get; set; }
        [Reactive] public string Warning { get; set; }

        private CancellationTokenSource _cancellation;

        private static readonly TimeSpan CheckLockInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan LockTime = TimeSpan.FromMinutes(2);
        private const int DefaultAttemptsCount = 5;

        private Action _onMigrateAction;

        private Account _userAccount;

        public UnlockViewModel()
        {
        }

        public UnlockViewModel(
            IAtomexApp app, 
            WalletInfo wallet, 
            INavigationService navigationService)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            StoragePassword = new SecureString();
            WalletName = wallet?.Name;
            CheckWalletLock();
            _ = CheckPinExist();
            _ = CheckBiometric();
        }

        private void AddChar(string str)
        {
            if (IsPinExist)
            {
                if (!(StoragePassword?.Length < 4)) return;
                
                foreach (var c in str)
                    StoragePassword.AppendChar(c);
                this.RaisePropertyChanged(nameof(StoragePassword));

                if (StoragePassword.Length == 4)
                    _ = UnlockAsync();
            }
            else
            {
                if (!IsEnteredStoragePassword)
                {
                    if (!(StoragePassword?.Length < 4)) return;
                    foreach (var c in str)
                        StoragePassword.AppendChar(c);
                    this.RaisePropertyChanged(nameof(StoragePassword));

                    if (StoragePassword?.Length != 4) return;
                    
                    IsEnteredStoragePassword = true;
                    Header = AppResources.ReEnterPin;
                    this.RaisePropertyChanged(nameof(Header));
                }
                else
                {
                    if (!(StoragePasswordConfirmation?.Length < 4)) return;
                    
                    foreach (var c in str)
                        StoragePasswordConfirmation.AppendChar(c);
                    this.RaisePropertyChanged(nameof(StoragePasswordConfirmation));

                    if (StoragePasswordConfirmation?.Length != 4) return;
                    
                    if (IsValidStoragePassword())
                    {
                        _ = EnablePin();
                        _ = UnlockAsync();
                    }
                    else
                    {
                        _ = ShakePage();
                        ClearStoragePswd();
                    }
                }
            }
        }

        private void RemoveChar()
        {
            if (!IsEnteredStoragePassword)
            {
                if (StoragePassword == null || StoragePassword.Length == 0) return;
                
                StoragePassword.RemoveAt(StoragePassword.Length - 1);
                this.RaisePropertyChanged(nameof(StoragePassword));
            }
            else
            {
                if (StoragePasswordConfirmation == null || StoragePasswordConfirmation.Length == 0) return;
                
                StoragePasswordConfirmation.RemoveAt(StoragePasswordConfirmation.Length - 1);
                this.RaisePropertyChanged(nameof(StoragePasswordConfirmation));
            }
        }

        private async Task EnablePin()
        {
            IsPinExist = true;

            try
            {
                await SecureStorage.SetAsync(WalletName + "-" + "AuthType", "Pin");
                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", DefaultAttemptsCount.ToString());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Device doesn't support secure storage on device");
            }
        }

        private SecureString GenerateSecureString(string str)
        {
            var secureString = new SecureString();
            foreach (var c in str)
                secureString.AppendChar(c);

            return secureString;
        }

        private void SetPassword(string pswd)
        {
            SecureString secureString = GenerateSecureString(pswd);
            StoragePassword = secureString;
        }

        private void ClearStoragePswd()
        {
            IsEnteredStoragePassword = false;
            StoragePassword?.Clear();
            StoragePasswordConfirmation?.Clear();
            Header = AppResources.SetPinIsteadOfPswd;
            Warning = string.Empty;

            this.RaisePropertyChanged(nameof(Header));
            this.RaisePropertyChanged(nameof(Warning));
            this.RaisePropertyChanged(nameof(StoragePassword));
            this.RaisePropertyChanged(nameof(StoragePasswordConfirmation));
        }

        private bool IsValidStoragePassword()
        {
            return (StoragePassword == null ||
                    StoragePasswordConfirmation == null ||
                    StoragePassword.SecureEqual(StoragePasswordConfirmation)) && StoragePasswordConfirmation != null;
        }

        private ICommand _unlockCommand;
        public ICommand UnlockCommand => _unlockCommand ??= new Command(async () => await UnlockAsync());

        private ICommand _addCharCommand;
        public ICommand AddCharCommand => _addCharCommand ??= new Command<string>(AddChar);

        private ICommand _deleteCharCommand;
        public ICommand DeleteCharCommand => _deleteCharCommand ??= new Command(RemoveChar);

        private ICommand _backCommand;

        public ICommand BackCommand => _backCommand ??= ReactiveCommand.Create(() =>
        {
            _userAccount = null;
            _cancellation?.Cancel();
        });

        private ICommand _textChangedCommand;

        public ICommand TextChangedCommand =>
            _textChangedCommand ??= new Command<string>(SetPassword);

        private ICommand _cancelCommand;

        public ICommand CancelCommand => _cancelCommand ??= ReactiveCommand.Create(() =>
        {
            _cancellation?.Cancel();
            _userAccount = null;
            _navigationService?.ClosePage();
        });

        private async Task UnlockAsync()
        {
            IsLoading = true;
            Account account;

            try
            {
                if (_userAccount == null)
                {
                    var fileSystem = FileSystem.Current;
                    var walletPath = Path.Combine(
                        fileSystem.PathToDocuments,
                        WalletInfo.DefaultWalletsDirectory,
                        WalletName,
                        WalletInfo.DefaultWalletFileName);
                    
                    account = await Task.Run(() =>
                    {
                        return Account.LoadFromFile(
                            pathToAccount: walletPath,
                            password: StoragePassword,
                            currenciesProvider: _app.CurrenciesProvider,
                            migrationCompleteCallback: (MigrationActionType actionType) =>
                            {
                                if (actionType == MigrationActionType.XtzTransactionsDeleted)
                                    _onMigrateAction = ScanXtzCurrencies;
                                
                                if (actionType == MigrationActionType.XtzTokensDataDeleted)
                                    _onMigrateAction = ScanXtz;
                            });
                    });
                }
                else
                {
                    account = _userAccount;
                    
                    try
                    {
                        if (!account.ChangePassword(StoragePassword))
                            throw new Exception("Can't change password");

                        await SecureStorage.SetAsync(WalletName, SecureStringToString(StoragePassword));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Change password error");
                    }
                }

                if (account != null)
                {
                    try
                    {
                        if (IsPinExist)
                        {
                            await Task.Run(() =>
                            {
                                _mainViewModel = new MainViewModel(_app, account);
                                _onMigrateAction?.Invoke();
                            });

                            Application.Current.MainPage = new MainPage(_mainViewModel);

                            try
                            {
                                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts",
                                    DefaultAttemptsCount.ToString());
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Device doesn't support secure storage on device");
                            }
                        }
                        else
                        {
                            IsLoading = false;
                            _userAccount = account;
                            StoragePasswordConfirmation = new SecureString();
                            ClearStoragePswd();
                            _navigationService?.ShowPage(new AuthPage(this));
                        }
                    }
                    catch (Exception ex)
                    {
                        // msg to user
                        Log.Error(ex, "Device doesn't support secure storage on device");
                    }
                }
                else
                {
                    IsLoading = false;
                    StoragePassword.Clear();
                    this.RaisePropertyChanged(nameof(StoragePassword));
                    UpdateAttemptsCounter();
                    _ = ShakePage();
                }
            }
            catch (CryptographicException e)
            {
                IsLoading = false;
                StoragePassword.Clear();
                this.RaisePropertyChanged(nameof(StoragePassword));
                UpdateAttemptsCounter();
                _ = ShakePage();
                Log.Error(e, "Invalid password error");
            }
        }

        private async Task CheckPinExist()
        {
            var authType = await SecureStorage.GetAsync(WalletName + "-" + "AuthType");

            if (authType == "Pin")
            {
                Header = AppResources.EnterPin;
                IsPinExist = true;
            }
            else
            {
                Header = AppResources.SetPinIsteadOfPswd;
                IsPinExist = false;
            }

            this.RaisePropertyChanged(nameof(Header));
            this.RaisePropertyChanged(nameof(IsPinExist));
        }

        private String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        private async Task ShakePage()
        {
            try
            {
                Vibration.Vibrate();
            }
            catch (FeatureNotSupportedException ex)
            {
                Log.Error(ex, "Vibration not supported on device");
            }

            var view = Application.Current.MainPage;
            await view.TranslateTo(-15, 0, 50);
            await view.TranslateTo(15, 0, 50);
            await view.TranslateTo(-10, 0, 50);
            await view.TranslateTo(10, 0, 50);
            await view.TranslateTo(-5, 0, 50);
            await view.TranslateTo(5, 0, 50);
            view.TranslationX = 0;
        }

        private async void CheckWalletLock()
        {
            try
            {
                string lockTime = await SecureStorage.GetAsync(WalletName + "-" + "LockTime");

                if (string.IsNullOrEmpty(lockTime))
                {
                    IsLocked = false;
                    int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"), out var attemptsCount);

                    if (attemptsCount >= DefaultAttemptsCount) return;
                    
                    string message = string.Format(CultureInfo.InvariantCulture, AppResources.AttemptsLeft,
                        attemptsCount);
                    Warning = AppResources.InvalidPin + $"\r\n" + message;
                }
                else
                {
                    _ = StartLockTimer();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Check wallet lock error");
            }
        }

        private async Task StartLockTimer()
        {
            try
            {
                string time = await SecureStorage.GetAsync(WalletName + "-" + "LockTime");
                DateTime unlockTime = Convert.ToDateTime(time);

                if (DateTime.Compare(DateTime.Now.ToLocalTime(), unlockTime) < 0)
                {
                    _cancellation = new CancellationTokenSource();
                    IsLocked = true;
                    var lockMinutes = Math.Ceiling(unlockTime.Subtract(DateTime.UtcNow.ToLocalTime()).TotalMinutes);
                    Warning = string.Format(CultureInfo.InvariantCulture, AppResources.TryAgainInMinutes, lockMinutes);

                    while (IsLocked)
                    {
                        if (_cancellation.IsCancellationRequested) return;

                        await Task.Delay(CheckLockInterval);
                        var newTimeMin = Math.Ceiling(unlockTime.Subtract(DateTime.UtcNow.ToLocalTime()).TotalMinutes);
                        if (newTimeMin != lockMinutes)
                        {
                            Warning = string.Format(CultureInfo.InvariantCulture, AppResources.TryAgainInMinutes,
                                newTimeMin);
                            lockMinutes = newTimeMin;
                        }

                        if (DateTime.Compare(DateTime.Now.ToLocalTime(), unlockTime) >= 0)
                        {
                            try
                            {
                                IsLocked = false;
                                await SecureStorage.SetAsync(WalletName + "-" + "LockTime", string.Empty);

                                int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"),
                                    out int attemptsCount);
                                attemptsCount++;
                                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts",
                                    attemptsCount.ToString());

                                if (attemptsCount < DefaultAttemptsCount)
                                {
                                    string message = string.Format(CultureInfo.InvariantCulture,
                                        AppResources.AttemptsLeft, attemptsCount);
                                    Warning = AppResources.InvalidPin + $"\r\n" + message;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Device doesn't support secure storage on device");
                            }
                        }
                    }
                }
                else
                {
                    IsLocked = false;
                    await SecureStorage.SetAsync(WalletName + "-" + "LockTime", string.Empty);
                    int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"), out var attemptsCount);
                    attemptsCount++;
                    await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", attemptsCount.ToString());

                    if (attemptsCount < DefaultAttemptsCount)
                    {
                        string message = string.Format(CultureInfo.InvariantCulture, AppResources.AttemptsLeft,
                            attemptsCount);
                        Warning = AppResources.InvalidPin + $"\r\n" + message;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Lock timer error");
            }
        }

        private async void UpdateAttemptsCounter()
        {
            try
            {
                if (IsLocked) return;

                int.TryParse(await SecureStorage.GetAsync(WalletName + "-" + "PinAttempts"), out var attemptsCount);

                if (attemptsCount == 0) return;

                attemptsCount--;
                await SecureStorage.SetAsync(WalletName + "-" + "PinAttempts", attemptsCount.ToString());
                var message = string.Format(CultureInfo.InvariantCulture, AppResources.AttemptsLeft, attemptsCount);
                Warning = AppResources.InvalidPin + $"\r\n" + message;

                if (attemptsCount != 0) return;
                
                IsLocked = true;
                var lockTime = DateTime.UtcNow.ToLocalTime().AddMinutes(LockTime.Minutes);
                await SecureStorage.SetAsync(WalletName + "-" + "LockTime", lockTime.ToString(CultureInfo.InvariantCulture));
                _ = StartLockTimer();
            }
            catch (Exception e)
            {
                Log.Error(e, "Update attempts counter error");
            }
        }

        private async Task CheckBiometric()
        {
            try
            {
                var pswd = await SecureStorage.GetAsync(WalletName);
                if (string.IsNullOrEmpty(pswd))
                    return;
                bool isFingerprintAvailable = await CrossFingerprint.Current.IsAvailableAsync();
                if (isFingerprintAvailable)
                {
                    AuthenticationRequestConfiguration conf = new AuthenticationRequestConfiguration(
                        AppResources.Authentication,
                        AppResources.UseBiometric + $"'{WalletName}'");
                    var authResult = await CrossFingerprint.Current.AuthenticateAsync(conf);
                    if (authResult.Authenticated)
                    {
                        SetPassword(pswd);
                        _ = UnlockAsync();
                    }
                    else
                    {
                        if (_navigationService == null) return;

                        await _navigationService.ShowAlert(
                            AppResources.SorryLabel,
                            AppResources.NotAuthenticated,
                            AppResources.AcceptButton);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Device doesn't support secure storage on device");
            }
        }

        private void ScanXtzCurrencies()
        {
            var xtzCurrencies = new[] {"XTZ", "TZBTC", "KUSD"};
            _mainViewModel?.ScanCurrencies(xtzCurrencies);
        }

        private void ScanXtz()
        {
            var xtzCurrencies = new[] {"XTZ"};
            _mainViewModel?.ScanCurrencies(xtzCurrencies);
        }
    }
}