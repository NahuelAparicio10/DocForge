using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocForge.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Linq;
namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ITextExportService _textExportService;
    private readonly IDocxExportService _docxExportService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrowsePdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtractTextCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocxCommand))]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocxCommand))]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrowsePdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtractTextCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocxCommand))]
    private bool isBusy;
    
    partial void OnSelectedFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(PreviewTitle));
    }

    partial void OnExtractedTextChanged(string value)
    {
        OnPropertyChanged(nameof(ExtractedCharacterCount));
        OnPropertyChanged(nameof(ExtractedLineCount));
        OnPropertyChanged(nameof(HasExtractedText));
    }

    public MainViewModel(
        IPdfTextExtractor pdfTextExtractor,
        ITextExportService textExportService,
        IDocxExportService docxExportService,
        ILogger<MainViewModel> logger)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _textExportService = textExportService;
        _docxExportService = docxExportService;
        _logger = logger;
    }
    
    public string SelectedFileName =>
        string.IsNullOrWhiteSpace(SelectedFilePath)
            ? "No file selected"
            : Path.GetFileName(SelectedFilePath);

    public string PreviewTitle =>
        string.IsNullOrWhiteSpace(SelectedFilePath)
            ? "DocForge"
            : $"DocForge - {Path.GetFileName(SelectedFilePath)}";

    public int ExtractedCharacterCount =>
        string.IsNullOrEmpty(ExtractedText) ? 0 : ExtractedText.Length;

    public int ExtractedLineCount =>
        string.IsNullOrWhiteSpace(ExtractedText)
            ? 0
            : ExtractedText.Replace("\r\n", "\n").Split('\n').Length;

    public bool HasExtractedText =>
        !string.IsNullOrWhiteSpace(ExtractedText);

    public string PreviewPlaceholder =>
        "Extracted text preview will appear here.";

    private bool CanBrowsePdf() => !IsBusy;

    private bool CanExtractText() =>
        !IsBusy && !string.IsNullOrWhiteSpace(SelectedFilePath);

    private bool CanExportTxt() =>
        !IsBusy && !string.IsNullOrWhiteSpace(ExtractedText);

    private bool CanExportDocx() =>
        !IsBusy && !string.IsNullOrWhiteSpace(ExtractedText);

    [RelayCommand(CanExecute = nameof(CanBrowsePdf))]
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
            _logger.LogInformation("PDF selected: {FilePath}", SelectedFilePath);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExtractText))]
    private async Task ExtractTextAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Extracting text...";
            _logger.LogInformation("Starting text extraction for {FilePath}", SelectedFilePath);

            var result = await _pdfTextExtractor.ExtractTextAsync(SelectedFilePath);

            if (!result.Success)
            {
                StatusMessage = result.ErrorMessage ?? "Extraction failed";
                _logger.LogError("Extraction failed for {FilePath}. Error: {Error}", SelectedFilePath, result.ErrorMessage);
                return;
            }

            ExtractedText = result.ExtractedText;
            StatusMessage = $"Extracted {result.PageCount} pages";
            _logger.LogInformation("Extraction completed for {FilePath}. Pages: {PageCount}", SelectedFilePath, result.PageCount);
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during extraction";
            _logger.LogError(ex, "Unexpected error during extraction for {FilePath}", SelectedFilePath);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportTxt))]
    private async Task ExportTxtAsync()
    {
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
            _logger.LogInformation("Starting TXT export to {DestinationPath}", dialog.FileName);

            await _textExportService.ExportAsync(dialog.FileName, ExtractedText);

            StatusMessage = "TXT exported successfully";
            _logger.LogInformation("TXT export completed to {DestinationPath}", dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during TXT export";
            _logger.LogError(ex, "Unexpected error during TXT export to {DestinationPath}", dialog.FileName);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportDocx))]
    private async Task ExportDocxAsync()
    {
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
            _logger.LogInformation("Starting DOCX export to {DestinationPath}", dialog.FileName);

            await _docxExportService.ExportAsync(dialog.FileName, ExtractedText);

            StatusMessage = "DOCX exported successfully";
            _logger.LogInformation("DOCX export completed to {DestinationPath}", dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during DOCX export";
            _logger.LogError(ex, "Unexpected error during DOCX export to {DestinationPath}", dialog.FileName);
        }
        finally
        {
            IsBusy = false;
        }
    }
}