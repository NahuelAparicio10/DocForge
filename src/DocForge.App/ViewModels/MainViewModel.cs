using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocForge.Application.Abstractions;
using Microsoft.Win32;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ITextExportService _textExportService;
    private readonly IDocxExportService _docxExportService;

    [ObservableProperty]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isBusy;

    public MainViewModel(
        IPdfTextExtractor pdfTextExtractor,
        ITextExportService textExportService,
        IDocxExportService docxExportService)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _textExportService = textExportService;
        _docxExportService = docxExportService;
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

    [RelayCommand]
    private async Task ExportDocxAsync()
    {
        if (string.IsNullOrWhiteSpace(ExtractedText))
        {
            StatusMessage = "There is no extracted text to export";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Word document (*.docx)|*.docx",
            FileName = "extracted-text.docx",
            Title = "Save extracted text as DOCX"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Exporting DOCX...";

            await _docxExportService.ExportAsync(dialog.FileName, ExtractedText);

            StatusMessage = "DOCX exported successfully";
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