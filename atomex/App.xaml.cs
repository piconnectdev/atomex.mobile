﻿using System;
using System.Linq;
using Atomex;
using Atomex.Common.Configuration;
using Atomex.Core;
using Atomex.MarketData.Bitfinex;
using Microsoft.Extensions.Configuration;
using Xamarin.Forms;

namespace atomex
{
    public partial class App : Application
    {

        private IAtomexApp AtomexApp;

        public App()
        {
            InitializeComponent();

            var coreAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Atomex.Client.Core");

            var currenciesConfiguration = new ConfigurationBuilder()
                .AddEmbeddedJsonFile(coreAssembly, "currencies.json")
                .Build();

            var symbolsConfiguration = new ConfigurationBuilder()
                .AddEmbeddedJsonFile(coreAssembly, "symbols.json")
                .Build();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("configuration.json")
                .Build();
            // Android Error: System.ArgumentException: The path must be absolute.

            var currenciesProvider = new CurrenciesProvider(currenciesConfiguration);
            var symbolsProvider = new SymbolsProvider(symbolsConfiguration);

            AtomexApp = new AtomexApp()
                .UseCurrenciesProvider(currenciesProvider)
                .UseSymbolsProvider(symbolsProvider)
                .UseQuotesProvider(new BitfinexQuotesProvider(
                    currencies: currenciesProvider.GetCurrencies(Network.MainNet),
                    baseCurrency: BitfinexQuotesProvider.Usd));
            AtomexApp.Start();

            StartViewModel startViewModel = new StartViewModel(AtomexApp);
            MainPage = new NavigationPage(new StartPage(startViewModel));

            if (Resources.TryGetValue("NavigationBarBackgroundColor", out var navBarColor))
                ((NavigationPage)MainPage).BarBackgroundColor = (Color)navBarColor;
            if (Resources.TryGetValue("NavigationBarTextColor", out var navBarTextColor))
                ((NavigationPage)MainPage).BarTextColor = (Color)navBarTextColor;

        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
