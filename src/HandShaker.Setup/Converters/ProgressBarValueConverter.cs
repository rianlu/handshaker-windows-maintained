using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace HandShakerSimpleSetup.Converter
{
    public class ProgressBarValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Grid)values[0]).ActualWidth * (double)values[1] * 0.01;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
