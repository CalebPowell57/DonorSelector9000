using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace DonorSelector9000
{
    public class CollapsedWhenZeroConverter : IValueConverter
    {
        public object Convert(object v, Type t, object p, string l) => 
            Equals(0, v) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object v, Type t, object p, string l) => null;
    }
}
