using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocForge.Application.Abstractions;
using Microsoft.Win32;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ITextExportService _textExportService;

    [ObservableProperty]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isBusy;

    public MainViewModel(IPdfTextExtractor pdfTextExtractor, ITextExportService textExportService)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _textExportService = textExportService;
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
    private async Task ExportTxtAsync()
    {
        if (string.IsNullOrWhiteSpace(ExtractedText))
        {
            StatusMessage = "There is no extracted text to export";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt",
            FileName = "extracted-text.txt",
            Title = "Save extracted text"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Exporting TXT...";

            await _textExportService.ExportAsync(dialog.FileName, ExtractedText);

            StatusMessage = "TXT exported successfully";
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
}