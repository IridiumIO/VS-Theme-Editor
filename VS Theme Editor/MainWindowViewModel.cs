using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace VS_Theme_Editor;

public partial class MainWindowViewModel : ObservableObject
{

    [ObservableProperty]
    private Theme? workingTheme;

    [ObservableProperty]
    private CategoryData? selectedCategory;


    [ObservableProperty]
    private string visualStudioDirectory = @"C:\Program Files\Microsoft Visual Studio\2022\Community";
    
    [ObservableProperty]
    private string? ThemeFilePath { get; set; }


    [ObservableProperty]
    private string? colorFilter;

    public ICollectionView? FilteredEntries { get; private set; }


    public MainWindowViewModel()
    {
 
    }



    [RelayCommand]
    public void SaveTheme()
    {

        if (WorkingTheme is null || ThemeFilePath is null) return;

        var compiler = new PkgDefCompiler();
        compiler.Compile(WorkingTheme, ThemeFilePath);
      

    }

    [RelayCommand]
    public void SaveAsTheme()
    {

        if (WorkingTheme is null || ThemeFilePath is null) return;

        var fileDialog = new Microsoft.Win32.SaveFileDialog();
        fileDialog.Filter = "PkgDef files (*.pkgdef)|*.pkgdef";
        fileDialog.DefaultExt = ".pkgdef";

        if (fileDialog.ShowDialog() == true)
        {
            var compiler = new PkgDefCompiler();
            compiler.Compile(WorkingTheme, fileDialog.FileName);
        }

    }


    [RelayCommand]
    public void SelectTheme()
    {
        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PkgDef files (*.pkgdef)|*.pkgdef",
            Title = "Select a theme file"
        };


        if (fileDialog.ShowDialog() == true)
        {
            var decompiler = new PkgDefDecompiler();
            WorkingTheme = decompiler.Decompile(fileDialog.FileName);
            ThemeFilePath = fileDialog.FileName;
            OnPropertyChanged(nameof(WorkingTheme));
        }

    }


    [RelayCommand]
    public void DeleteCategory(CategoryData parameter)
    {

        if (WorkingTheme is null) return;

        if (WorkingTheme.Categories.Contains(parameter))
        {
            WorkingTheme.Categories.Remove(parameter);
            OnPropertyChanged(nameof(WorkingTheme));

        }
        else
        {
            Debug.WriteLine("Category not found in theme.");
        }

    }


    [RelayCommand]
    public void AddCategory(string categoryName)
    {
        if (WorkingTheme is null) return;
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            Debug.WriteLine("Category name cannot be empty.");
            return;
        }
        var newCategory = new CategoryData
        {
            Name = categoryName,
            Entries = new List<ThemeColorEntry>()
        };
        WorkingTheme.Categories.Add(newCategory);
        OnPropertyChanged(nameof(WorkingTheme));

        SelectedCategory = newCategory;
        OnPropertyChanged(nameof(SelectedCategory));
    }


    [RelayCommand]
    public void TestTheme()
    {
        var VSExe = Path.Combine(VisualStudioDirectory, @"Common7\IDE\devenv.exe");
        var VSThemeDir = @"Common7\IDE\CommonExtensions\Platform";

        var outputFilePath = Path.Combine(VisualStudioDirectory, VSThemeDir, "ThemeTest.pkgdef");

        var compiler = new PkgDefCompiler();
        compiler.Compile(WorkingTheme, outputFilePath);

        var updateConfigProcess = Process.Start(VSExe, "/updateconfiguration");
        updateConfigProcess.WaitForExit();
        if (updateConfigProcess.ExitCode != 0) throw new Exception("VS Failed to Update Config");

        Process.Start(VSExe);


    }


    partial void OnSelectedCategoryChanged(CategoryData? value)
    {
        if (value != null)
        {
            FilteredEntries = CollectionViewSource.GetDefaultView(value.Entries);
            FilteredEntries.Filter = FilterPredicate;
            OnPropertyChanged(nameof(FilteredEntries));
        }
    }

    partial void OnColorFilterChanged(string? value)
    {
        FilteredEntries?.Refresh();
    }

    private bool FilterPredicate(object obj)
    {
        if (obj is not ThemeColorEntry entry)
            return false;
        if (string.IsNullOrWhiteSpace(ColorFilter))
            return true;
        var filter = ColorFilter.Trim().ToLowerInvariant();
        return (entry.Background?.ToLowerInvariant().Contains(filter) == true) ||
               (entry.Foreground?.ToLowerInvariant().Contains(filter) == true) ||
               (entry.Name?.ToLowerInvariant().Contains(filter) == true);
    }


    [RelayCommand]
    private void RegenerateGuid()
    {
        Guid guid = Guid.NewGuid();
        if (WorkingTheme is not null)
        {
            WorkingTheme.Guid = guid;
            OnPropertyChanged(nameof(WorkingTheme));
        }
        else
        {
            Debug.WriteLine("WorkingTheme is null, cannot regenerate GUID.");
        }
    }


}
