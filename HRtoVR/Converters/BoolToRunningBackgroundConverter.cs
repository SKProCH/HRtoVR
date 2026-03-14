using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace HRtoVRChat.Converters;

public class BoolToRunningBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRunning && isRunning)
        {
            return Brush.Parse("#4CAF50"); // Green
        }
        return Brush.Parse("#757575"); // Gray
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
