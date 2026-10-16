using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace VS_Theme_Editor;


public partial class MainWindow : FluentWindow
{

    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        ISnackbarService snackbarService = new SnackbarService();
        snackbarService.SetSnackbarPresenter(RootSnackbar);

        _viewModel = new MainWindowViewModel(snackbarService);
        DataContext = _viewModel;

        //if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) == false)
        //{
        //    //Restart as admin
        //    var startInfo = new ProcessStartInfo
        //    {
        //        FileName = System.AppContext.BaseDirectory + AppDomain.CurrentDomain.FriendlyName,
        //        UseShellExecute = true,
        //        Verb = "runas"
        //    };

        //    Process.Start(startInfo);

        //    Application.Current.Shutdown();
        //}

    }

    private async void btn_AddCategory_clicked(object sender, RoutedEventArgs e)
    {

        //var msgError = new Wpf.Ui.Controls.ContentDialog { Title = $"New Settings Version Detected", Content = "Your settings have been reset to their default to accommodate the update", CloseButtonText = "OK" };
        //await msgError.ShowAsync();

        if (_viewModel.WorkingTheme is null) return;


        var catNameTextbox = new Wpf.Ui.Controls.TextBox
        {
            PlaceholderText = "Category Name"
        };

        var catGuidTextbox = new Wpf.Ui.Controls.TextBox
        {
            PlaceholderText = "GUID"
        };

        var stackPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Vertical,
            Children = { catNameTextbox, catGuidTextbox }
        };

        var cDialog = new Wpf.Ui.Controls.ContentDialog
        {
            Title = "Add Category",
            Content = stackPanel,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DialogHost = RootContentDialog
        };

        var retX = await cDialog.ShowAsync();

        if (retX != Wpf.Ui.Controls.ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(catNameTextbox.Text))
        {
            var msgBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Error",
                Content = "Category name cannot be empty."
            };
            await msgBox.ShowDialogAsync();
			return;
        }

		if( !Guid.TryParse(catGuidTextbox.Text, out Guid parsedGuid) ) {
            var msgBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Error",
                Content = "Invalid Guid."
            };
            await msgBox.ShowDialogAsync();
			return;
		}

        _viewModel.AddCategory(catNameTextbox.Text, parsedGuid);

        WorkingThemeCategories.ScrollIntoView(WorkingThemeCategories.SelectedItem);

    }

}



