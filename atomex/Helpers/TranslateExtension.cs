using System;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Helpers
{
    [ContentProperty("Text")]
    public class TranslateExtension : IMarkupExtension<BindingBase>
    {
        public string Text { get; set; }
        public string StringFormat { get; set; }
        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ProvideValue(serviceProvider);
        }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding
            {
                Mode = BindingMode.OneWay,
                Path = $"[{Text}]",
                Source = LocalizationResourceManager.Instance,
                StringFormat = StringFormat

            };
            return binding;
        }
    }
}