using CommunityToolkit.Mvvm.ComponentModel;

namespace VS_Theme_Editor;

public partial class CategoryData : ObservableObject
{
    [ObservableProperty]
    private string? name;

    [ObservableProperty]
    private Int32 dataLength;

    [ObservableProperty]
    private Int32 headerLength;

    [ObservableProperty]
    private Int32 categoryCount;

    [ObservableProperty]
    private Guid guid;

    [ObservableProperty]
    private Int32 entryCount;

    [ObservableProperty]
    private List<ThemeColorEntry> entries = new List<ThemeColorEntry>();
}