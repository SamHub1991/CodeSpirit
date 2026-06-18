namespace CodeSpirit.Core.Mvvm;

/// <summary>
/// Marks a ViewModel method as a command that can be invoked from the page.
/// The method must have no parameters. Commands are invoked via <c>data-cs-command</c> buttons.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Command]              // Command name = method name "Refresh"
/// [Command("Search")]     // Command name = "Search"
/// public void Refresh() { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    /// <summary>
    /// Command name referenced by <c>data-cs-command</c>. Defaults to the method name.
    /// </summary>
    public string? Name { get; }

    public CommandAttribute() { }

    public CommandAttribute(string name) => Name = name;
}
