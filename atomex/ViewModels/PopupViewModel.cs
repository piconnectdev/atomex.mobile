﻿using System.ComponentModel;
using System.Windows.Input;
using RGPopup.Maui.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.ViewModels
{
    public enum PopupType
    {
        [Description ("Success")]
        Success,
        [Description ("Error")]
        Error
    }

    public class PopupViewModel
    {
        public PopupType Type { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string ButtonText { get; set; }

        private ICommand _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new Command(async () => await PopupNavigation.Instance.PopAsync());
    }
}
