using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Wpf.Ui;

namespace VS_Theme_Editor;

public partial class MainWindowViewModel : ObservableObject
{

    [ObservableProperty]
    private Theme? workingTheme;

    [ObservableProperty]
    private CategoryData? selectedCategory;


    [ObservableProperty]
    private string visualStudioDirectory = @"C:\Program Files\Microsoft Visual Studio\2023\Community";
    
    [ObservableProperty]
    private string? ThemeFilePath { get; set; }


    [ObservableProperty]
    private string? colorFilter;

    public ICollectionView? FilteredEntries { get; private set; }


    private ISnackbarService _snackbarService;

    public MainWindowViewModel(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
   
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
    public async void TestTheme()
    {

        if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) == false)
        {

            var ret = MessageBox.Show("Admin mode is required to test the theme as it needs access to the Visual Studio Directory. Restart in Admin mode? (Make sure you save the theme first!)", "Administrator Mode required", MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                //Restart as admin
                var startInfo = new ProcessStartInfo
                {
                    FileName = System.AppContext.BaseDirectory + AppDomain.CurrentDomain.FriendlyName,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);

                Application.Current.Shutdown();
            }

            return;
          
        }



        if (Directory.Exists(VisualStudioDirectory) == false)
        {
            MessageBox.Show("Please select the Visual Studio Directory (/Microsoft Visual Studio/2022/Community", "Visual Studio Directory Not Found");
            var folderDialog = new Microsoft.Win32.OpenFolderDialog()
            {
                Title = "Select Visual Studio Community 2022 Directory (/Microsoft Visual Studio/2022/Community)",
                Multiselect = false
            };
            if (folderDialog.ShowDialog() == true)
            {
                var directoryName = folderDialog.FolderName;
                VisualStudioDirectory = folderDialog.FolderName ?? string.Empty;

            }
            else
            {
                return; // User cancelled the dialog
            }
        }

        var VSExe = Path.Combine(VisualStudioDirectory, @"Common7\IDE\devenv.exe");
        var VSThemeDir = @"Common7\IDE\CommonExtensions\Platform";

        if (!File.Exists(VSExe))
        {
            MessageBox.Show("Visual Studio executable not found. Please ensure the path is correct.", "Error");
            return;
        }
        if (!Directory.Exists(Path.Combine(VisualStudioDirectory, VSThemeDir)))
        {
            MessageBox.Show("Visual Studio theme directory not found. Please ensure the path is correct.", "Error");
            return;
        }

        var outputFilePath = Path.Combine(VisualStudioDirectory, VSThemeDir, "ThemeTest.pkgdef");

        var compiler = new PkgDefCompiler();
        compiler.Compile(WorkingTheme, outputFilePath);

        var updateConfigProcess = Process.Start(VSExe, "/updateconfiguration");
        
        _snackbarService.Show("Testing Theme", "Updating Visual Studio Configuration. Please wait...", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(15));

        await updateConfigProcess.WaitForExitAsync();
        _snackbarService.Show("Testing Theme", "Close Visual Studio to Continue", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(1000));


        if (updateConfigProcess.ExitCode != 0) throw new Exception("VS Failed to Update Config");
        await Task.Delay(1000); // Wait a bit to ensure the config is updated

        var runner = Process.Start(VSExe);

        runner.WaitForExit();
        _snackbarService.Show("Testing Theme", "Visual Studio Closed. You can now edit the theme.", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(10));

        File.Delete(outputFilePath);

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
