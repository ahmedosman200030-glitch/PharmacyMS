using Avalonia.Media;

namespace PharmacyMS.Desktop.Services;

public static class AvatarHelper
{
    private static readonly string[] Palette =
    {
        "#DC2626", "#3B82F6", "#10B981", "#F59E0B", "#8B5CF6",
        "#EC4899", "#14B8A6", "#F97316", "#6366F1", "#84CC16"
    };

    public static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "?";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0][0].ToString() + parts[^1][0]).ToUpperInvariant();
    }

    public static IBrush GetBrush(string fullName)
    {
        var key = string.IsNullOrWhiteSpace(fullName) ? "?" : fullName;
        var hash = 0;
        foreach (var c in key) hash = (hash * 31 + c) & 0x7FFFFFFF;
        var color = Palette[hash % Palette.Length];
        return new SolidColorBrush(Color.Parse(color));
    }
}
