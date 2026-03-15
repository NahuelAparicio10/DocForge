using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocForge.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ITextExportService _textExportService;
    private readonly IDocxExportService _docxExportService;
    private readonly ITextStructureReconstructor _textStructureReconstructor;
    private readonly ISummaryService _summaryService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateAiSummaryCommand))]
    private bool isAiSummaryAvailable;

    [ObservableProperty]
    private string aiSummaryStatus = "Checking Ollama...";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportAiSummaryTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportAiSummaryDocxCommand))]
    private string aiSummaryResult = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrowsePdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtractTextCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocxCommand))]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocxCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateAiSummaryCommand))]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrowsePdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtractTextCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocxCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateAiSummaryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportAiSummaryTxtCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportAiSummaryDocxCommand))]
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
        ITextStructureReconstructor textStructureReconstructor,
        ISummaryService summaryService,
        ILogger<MainViewModel> logger)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _textExportService = textExportService;
        _docxExportService = docxExportService;
        _textStructureReconstructor = textStructureReconstructor;
        _summaryService = summaryService;
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

    public bool HasAiSummary =>
        !string.IsNullOrWhiteSpace(AiSummaryResult);

    public string PreviewPlaceholder =>
        "Extracted text preview will appear here.";

    private bool CanBrowsePdf() => !IsBusy;

    private bool CanExtractText() =>
        !IsBusy && !string.IsNullOrWhiteSpace(SelectedFilePath);

    private bool CanExportTxt() =>
        !IsBusy && !string.IsNullOrWhiteSpace(ExtractedText);

    private bool CanExportDocx() =>
        !IsBusy && !string.IsNullOrWhiteSpace(ExtractedText);

    private bool CanGenerateAiSummary() =>
        !IsBusy && IsAiSummaryAvailable && !string.IsNullOrWhiteSpace(ExtractedText);

    private bool CanExportAiSummaryTxt() =>
        !IsBusy && !string.IsNullOrWhiteSpace(AiSummaryResult);

    private bool CanExportAiSummaryDocx() =>
        !IsBusy && !string.IsNullOrWhiteSpace(AiSummaryResult);

    public async Task InitializeAsync()
    {
        IsAiSummaryAvailable = await _summaryService.IsAvailableAsync();
        AiSummaryStatus = IsAiSummaryAvailable
            ? "Ollama detected"
            : "AI Summary requires Ollama running locally";
    }

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

            ExtractedText = _textStructureReconstructor.Reconstruct(result.ExtractedText);
            AiSummaryResult = string.Empty;
            OnPropertyChanged(nameof(HasAiSummary));

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

    [RelayCommand(CanExecute = nameof(CanGenerateAiSummary))]
    private async Task GenerateAiSummaryAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Generating AI summary...";

            AiSummaryResult = await _summaryService.SummarizeAsync(ExtractedText);
            OnPropertyChanged(nameof(HasAiSummary));

            StatusMessage = "AI summary generated";
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during AI summary";
            _logger.LogError(ex, "Unexpected error during AI summary");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportAiSummaryTxt))]
    private async Task ExportAiSummaryTxtAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt",
            FileName = "ai-summary.txt",
            Title = "Save AI summary"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Exporting AI summary TXT...";
            _logger.LogInformation("Starting AI summary TXT export to {DestinationPath}", dialog.FileName);

            await _textExportService.ExportAsync(dialog.FileName, AiSummaryResult);

            StatusMessage = "AI summary TXT exported successfully";
            _logger.LogInformation("AI summary TXT export completed to {DestinationPath}", dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during AI summary TXT export";
            _logger.LogError(ex, "Unexpected error during AI summary TXT export to {DestinationPath}", dialog.FileName);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportAiSummaryDocx))]
    private async Task ExportAiSummaryDocxAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Word document (*.docx)|*.docx",
            FileName = "ai-summary.docx",
            Title = "Save AI summary as DOCX"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Exporting AI summary DOCX...";
            _logger.LogInformation("Starting AI summary DOCX export to {DestinationPath}", dialog.FileName);

            await _docxExportService.ExportAsync(dialog.FileName, AiSummaryResult);

            StatusMessage = "AI summary DOCX exported successfully";
            _logger.LogInformation("AI summary DOCX export completed to {DestinationPath}", dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during AI summary DOCX export";
            _logger.LogError(ex, "Unexpected error during AI summary DOCX export to {DestinationPath}", dialog.FileName);
        }
        finally
        {
            IsBusy = false;
        }
    }
}