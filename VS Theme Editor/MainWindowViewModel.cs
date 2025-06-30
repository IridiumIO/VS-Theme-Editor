using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VS_Theme_Editor;

public partial class MainWindowViewModel : ObservableObject
{

    [ObservableProperty]
    public Theme WorkingTheme { get; set; }


    public MainWindowViewModel()
    {
        var decompiler = new PkgDefDecompiler();
        WorkingTheme = decompiler.Decompile();


    }



    [RelayCommand]
    public void SaveTheme()
    {

        var compiler = new PkgDefCompiler();
        compiler.Compile(WorkingTheme, "Theme.Modified.pkgdef");
    }


}
