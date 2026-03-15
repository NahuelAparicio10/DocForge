using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocForge.Application.Abstractions;
using DocForge.Infrastructure.Services;
using Microsoft.Win32;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPdfTextExtractor _pdfTextExtractor;

    [ObservableProperty]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isBusy;

    public MainViewModel()
    {
        _pdfTextExtractor = new PdfPigTextExtractor();
    }

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
    private async Task ExtractTextAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            StatusMessage = "Select a PDF first";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Extracting text...";

            var result = await _pdfTextExtractor.ExtractTextAsync(SelectedFilePath);

            if (!result.Success)
            {
                StatusMessage = result.ErrorMessage ?? "Extraction failed";
                return;
            }

            ExtractedText = result.ExtractedText;
            StatusMessage = $"Extracted {result.PageCount} pages";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ExportTxt()
    {
        StatusMessage = "TXT export will be implemented next";
    }
}