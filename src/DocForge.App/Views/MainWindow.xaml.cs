using DocForge.App.ViewModels;
using System.Windows;

namespace DocForge.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        if (DataContext is not MainViewModel vm)
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        await vm.AddPdfFilesAsync(files);
    }
}