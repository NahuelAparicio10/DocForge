using System.Windows;
using DocForge.App.ViewModels;

namespace DocForge.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}