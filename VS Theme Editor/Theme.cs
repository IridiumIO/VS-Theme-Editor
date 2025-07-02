using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

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
    private ObservableCollection<CategoryData> categories = new ObservableCollection<CategoryData>();
}