using CommunityToolkit.Mvvm.ComponentModel;

namespace VS_Theme_Editor;

public partial class ThemeColorEntry : ObservableObject
{
    [ObservableProperty]
    private string? name;

    [ObservableProperty]
    private byte backgroundType;

    [ObservableProperty]
    private string background;

    [ObservableProperty]
    private byte foregroundType;

    [ObservableProperty]
    private string foreground;
}
