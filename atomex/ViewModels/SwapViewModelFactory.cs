﻿using Atomex.Abstract;
using Atomex.Common;
using Atomex.Core;

namespace atomex.ViewModels
{
    public static class SwapViewModelFactory
    {
        public static SwapViewModel CreateSwapViewModel(Swap swap, ICurrencies currencies, INavigationService navigationService)
        {
            var soldCurrency = currencies.GetByName(swap.SoldCurrency);
            var purchasedCurrency = currencies.GetByName(swap.PurchasedCurrency);
            
            var fromAmount = AmountHelper.QtyToSellAmount(swap.Side, swap.Qty, swap.Price, soldCurrency.DigitsMultiplier);
            var toAmount = AmountHelper.QtyToSellAmount(swap.Side.Opposite(), swap.Qty, swap.Price, purchasedCurrency.DigitsMultiplier);

            var quoteCurrency = swap.Symbol.QuoteCurrency() == swap.SoldCurrency
                ? soldCurrency
                : purchasedCurrency;

            var swapViewModel = new SwapViewModel
            {
                Id               = swap.Id.ToString(),
                Mode             = ModeBySwap(swap),
                Time             = swap.TimeStamp,
                FromAmount       = fromAmount,
                FromCurrencyCode = soldCurrency.Name,
                FromCurrencyName = soldCurrency.DisplayedName,
                ToAmount         = toAmount,
                ToCurrencyCode   = purchasedCurrency.Name,
                ToCurrencyName   = purchasedCurrency.DisplayedName,
                Price            = swap.Price,
                PriceFormat      = $"F{quoteCurrency.Digits}",
                Currencies       = currencies
            };

            swapViewModel.SetNavigationService(navigationService);
            swapViewModel.UpdateSwap(swap);

            return swapViewModel;
        }

        private static SwapMode ModeBySwap(Swap swap)
        {
            return swap.IsInitiator
                ? SwapMode.Initiator
                : SwapMode.CounterParty;
        }
    }
}
