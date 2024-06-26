﻿using atomex.ViewModels;
using RGPopup.Maui.Pages;

namespace atomex.Views.Popup
{
    public partial class CompletionPopup : PopupPage
    {
        public CompletionPopup(PopupViewModel popupViewModel)
        {
            InitializeComponent();
            BindingContext = popupViewModel;
        }
    }
}
