using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PharmacyMS.Domain.Enums;

namespace PharmacyMS.Desktop.Converters;

public class ApprovalStatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ApprovalStatus status)
        {
            return status switch
            {
                ApprovalStatus.Pending => new SolidColorBrush(Color.Parse("#F59E0B")),
                ApprovalStatus.Rejected => new SolidColorBrush(Color.Parse("#EF4444")),
                _ => new SolidColorBrush(Color.Parse("#10B981"))
            };
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ApprovalStatusEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ApprovalStatus status && parameter is string s && Enum.TryParse<ApprovalStatus>(s, out var target))
            return status == target;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class PaymentStatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Paid" => new SolidColorBrush(Color.Parse("#10B981")),
                "Partial" => new SolidColorBrush(Color.Parse("#F59E0B")),
                _ => new SolidColorBrush(Color.Parse("#EF4444")) // Unpaid
            };
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
