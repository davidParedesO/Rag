using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Rag.Converters;

/// <summary>
/// Devuelve el color activo (#3B82F6) si el valor booleano es true,
/// o el color inactivo (#334155) si es false.
/// Usado para resaltar el tab (Chat / Documentos) que está activo.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;
        return isActive ? Color.FromArgb("#3B82F6") : Color.FromArgb("#334155");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
