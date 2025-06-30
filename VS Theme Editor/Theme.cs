using CommunityToolkit.Mvvm.ComponentModel;

namespace VS_Theme_Editor;

public partial class Theme: ObservableObject
{

    [ObservableProperty]
    private Guid? guid  = null;

    [ObservableProperty]
    private string? name  = null;

    [ObservableProperty]
    private string? slug  = null;

    [ObservableProperty]
    private string? fallback  = null;

    [ObservableProperty]
    private List<CategoryData> categories = new List<CategoryData>();
}