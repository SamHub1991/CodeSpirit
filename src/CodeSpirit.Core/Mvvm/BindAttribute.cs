namespace CodeSpirit.Core.Mvvm;

/// <summary>
/// Exposes a ViewModel property to the page runtime for data binding.
/// Properties without this attribute are not included in ViewModel state.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Bind]                                  // OneWay (read-only from ViewModel)
/// [Bind(BindDirection.TwoWay)]             // Accepts POST values back into ViewModel
/// [Bind(BindDirection.TwoWay, Name = "city")]  // Custom binding name
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class BindAttribute : Attribute
{
    /// <summary>
    /// Custom binding name exposed to the page. Defaults to the property name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Binding direction. <see cref="BindDirection.OneWay"/> for display only,
    /// <see cref="BindDirection.TwoWay"/> for forms that post values back.
    /// </summary>
    public BindDirection Direction { get; set; } = BindDirection.OneWay;

    public BindAttribute() { }

    public BindAttribute(BindDirection direction) => Direction = direction;
}
