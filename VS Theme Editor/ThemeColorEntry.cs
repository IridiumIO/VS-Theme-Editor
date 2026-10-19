using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace VS_Theme_Editor;

public partial class ThemeColorEntry : ObservableObject, Json.IDictionaryable
{
    [property: JsonIgnore]
    [ObservableProperty]
    private string? name;

    [JsonIgnore]
    public string? Id {
        get => Name;
        set => Name = value;
    }

    [ObservableProperty]
    private byte backgroundType;

    [ObservableProperty]
    private string background;

    [ObservableProperty]
    private byte foregroundType;

    [ObservableProperty]
    private string foreground;
}
