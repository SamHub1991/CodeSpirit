using System.Reflection;

namespace CodeSpirit.Infrastructure;

public static class ValueConverter
{
    public static object? ConvertValue(object? value, Type target)
    {
        if (value is null)
            return null;

        var underlying = Nullable.GetUnderlyingType(target) ?? target;
        if (underlying.IsInstanceOfType(value))
            return value;

        var stringValue = value.ToString();
        return underlying switch
        {
            Type t when t == typeof(string) => stringValue,
            Type t when t == typeof(int) && int.TryParse(stringValue, out var i) => i,
            Type t when t == typeof(long) && long.TryParse(stringValue, out var l) => l,
            Type t when t == typeof(decimal) && decimal.TryParse(stringValue, out var d) => d,
            Type t when t == typeof(double) && double.TryParse(stringValue, out var dbl) => dbl,
            Type t when t == typeof(Guid) && Guid.TryParse(stringValue, out var g) => g,
            Type t when t == typeof(DateTime) && DateTime.TryParse(stringValue, out var dt) => dt,
            Type t when t == typeof(TimeSpan) && TimeSpan.TryParse(stringValue, out var ts) => ts,
            Type t when t == typeof(bool) && bool.TryParse(stringValue, out var b) => b,
            Type t when t.IsEnum => Enum.Parse(t, stringValue!, ignoreCase: true),
            _ => Convert.ChangeType(value, underlying)
        };
    }
}
