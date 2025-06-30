using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VS_Theme_Editor;

[ValueConversion(typeof(string), typeof(Color))]
public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(s);
            }
            catch
            {
                return Colors.Transparent;
            }
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
