namespace CodeSpirit.Core.Mvvm;

/// <summary>
/// Specifies the binding direction between a ViewModel property and the page.
/// </summary>
public enum BindDirection
{
    /// <summary>ViewModel to page only. Property is read-only from the page.</summary>
    OneWay,
    /// <summary>Both directions. Page can read and write the property via POST.</summary>
    TwoWay,
    /// <summary>Page to ViewModel only. Property value flows into the ViewModel.</summary>
    OneWayToSource
}
