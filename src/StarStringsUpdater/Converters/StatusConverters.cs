using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using StarStringsUpdater.ViewModels;

namespace StarStringsUpdater.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
    public static readonly StatusToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value switch
        {
            ChannelStatus.UpToDate => "#3E8E68",
            ChannelStatus.UpdateAvailable => "#C98A3E",
            ChannelStatus.NotInstalled => "#5B6478",
            ChannelStatus.Updating => "#2FB6D9",
            ChannelStatus.Error => "#B84842",
            _ => "#5B6478",
        };
        return SolidColorBrush.Parse(hex);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StatusToTextConverter : IValueConverter
{
    public static readonly StatusToTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ChannelStatus.UpToDate => "Up to date",
            ChannelStatus.UpdateAvailable => "Update available",
            ChannelStatus.NotInstalled => "Not installed",
            ChannelStatus.Updating => "Updating…",
            ChannelStatus.Error => "Error",
            _ => "Unknown",
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
