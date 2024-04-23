using System;
using Atomex;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Bitcoin;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;
using Atomex.Core;
using Atomex.EthereumTokens;
using Atomex.Wallets.Abstract;

namespace atomex.ViewModels.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static TransactionViewModel CreateViewModel(
            ITransaction tx,
            CurrencyConfig currencyConfig,
            INavigationService navigationService)
        {
            return currencyConfig switch
            {
                BitcoinBasedConfig config =>
                    new BitcoinBasedTransactionViewModel(tx as BitcoinTransaction, config, navigationService),
                Erc20Config config =>
                    new EthereumERC20TransactionViewModel(tx as EthereumTransaction, config, navigationService),
                EthereumConfig config =>
                    new EthereumTransactionViewModel(tx as EthereumTransaction, config, navigationService),
                TezosConfig config =>
                    new TezosTransactionViewModel(tx as TezosTransaction, config, navigationService),

                _ => throw new ArgumentOutOfRangeException("Not supported transaction type.")
            };
        }
    }
}