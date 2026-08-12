using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Desktop.Views.Users;

public class RoleBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        UserRole.Admin => new SolidColorBrush(Color.Parse("#7C3AED")),
        UserRole.Pharmacist => new SolidColorBrush(Color.Parse("#3B82F6")),
        UserRole.Cashier => new SolidColorBrush(Color.Parse("#F59E0B")),
        _ => new SolidColorBrush(Color.Parse("#64748B"))
    };
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class ActiveBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? new SolidColorBrush(Color.Parse("#22C55E")) : new SolidColorBrush(Color.Parse("#EF4444"));
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class ActiveTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "Active" : "Inactive";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

public class InitialsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value as string;
        if (string.IsNullOrWhiteSpace(name)) return "U";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0][0].ToString() + parts[^1][0]).ToUpperInvariant();
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
