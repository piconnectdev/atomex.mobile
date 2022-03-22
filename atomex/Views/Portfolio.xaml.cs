﻿using Xamarin.Forms;
using atomex.ViewModel;

namespace atomex
{
    public partial class Portfolio : ContentPage
    {
        public Portfolio(PortfolioViewModel portfolioViewModel)
        {
            InitializeComponent();
            BindingContext = portfolioViewModel;
        }
    }
}