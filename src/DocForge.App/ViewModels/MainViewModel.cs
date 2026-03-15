using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [RelayCommand]
    private void BrowsePdf()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Multiselect = false,
            Title = "Select a PDF file"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            StatusMessage = "PDF selected";
        }
    }

    [RelayCommand]
    private void ExtractText()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            StatusMessage = "Select a PDF first";
            return;
        }

        ExtractedText = $"Selected PDF:{Environment.NewLine}{SelectedFilePath}";
        StatusMessage = "Extraction placeholder executed";
    }

    [RelayCommand]
    private void ExportTxt()
    {
        StatusMessage = "Export TXT clicked";
    }
}