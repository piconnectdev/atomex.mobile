using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace atomex.Styles
{
    public partial class SmallDevicesStyle : ResourceDictionary
    {
        public static SmallDevicesStyle SharedInstance { get; } = new SmallDevicesStyle();
        public SmallDevicesStyle()
        {
            InitializeComponent();
        }
    }
}
