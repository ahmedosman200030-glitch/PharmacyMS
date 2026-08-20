using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PharmacyMS.Desktop.ViewModels;

public class DirectionColorConverter : IValueConverter
{
    public static readonly DirectionColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var direction = value as string;
        return direction == "In"
            ? new SolidColorBrush(Color.Parse("#10B981"))
            : new SolidColorBrush(Color.Parse("#EF4444"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
