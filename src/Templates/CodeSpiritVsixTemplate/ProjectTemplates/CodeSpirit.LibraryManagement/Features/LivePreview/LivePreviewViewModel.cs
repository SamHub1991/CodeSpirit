using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;

namespace $safeprojectname$.Features.LivePreview;

[PageDirective(Route = "/live-preview", Title = "Live Preview UI Editor")]
[Service]
public class LivePreviewViewModel : ViewModel
{
    [Bind] public string Title { get; set; } = "Realtime Preview UI Editor";
    [Bind] public string Subtitle { get; set; } = "Pick an element, edit attributes, style, text, or HTML, then preview instantly.";
    [Bind] public string Status { get; set; } = "Active";
    [Bind] public bool CanEdit { get; set; } = true;
    [Bind] public int Score { get; set; } = 92;
    [Bind] public List<PreviewStep> Steps { get; set; } = [];
    [Bind] public List<PreviewNode> Nodes { get; set; } = [];

    public override Task LoadAsync()
    {
        Steps =
        [
            new("select", "Select", "Open the Dev Panel and enable Pick Page Element."),
            new("edit", "Edit", "Change class, style, text, html, data-cs-* or data-ui."),
            new("sync", "Sync", "Use Sync to File when the preview matches your target UI.")
        ];

        Nodes =
        [
            new("Framework", "Runtime, expression, intent, and UI behaviors", "expanded"),
            new("UI Editor", "Element picking, live preview, undo, diff, and sync", "active"),
            new("Template", "VSIX project template stays aligned with source", "ready")
        ];

        return Task.CompletedTask;
    }

    public record PreviewStep(string Key, string Title, string Description);
    public record PreviewNode(string Name, string Description, string Tone);
}
