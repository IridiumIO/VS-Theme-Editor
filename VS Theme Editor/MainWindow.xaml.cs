using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;

namespace VS_Theme_Editor;


public partial class MainWindow : FluentWindow
{

 
    public MainWindow()
    {
        InitializeComponent();

        DataContext = new MainWindowViewModel();

    }
}



