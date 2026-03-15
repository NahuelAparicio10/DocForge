using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocForge.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace DocForge.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ITextExportService _textExportService;
    private readonly IDocxExportService _docxExportService;
    private readonly ITextStructureReconstructor _textStructureReconstructor;
    private readonly ISummaryService _summaryService;
    private readonly ILogger<MainViewModel> _logger;

    public ObservableCollection<ProcessedPdfItem> Documents { get; } = new();

    [ObservableProperty]
    private ProcessedPdfItem? selectedDocument;

    [ObservableProperty]
    private bool isAiSummaryAvailable;

    [ObservableProperty]
    private string aiSummaryStatus = "Checking Ollama...";

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private bool isBusy;

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

    partial void OnSelectedDocumentChanged(ProcessedPdfItem? value)
    {
        OnPropertyChanged(nameof(SelectedFileName));
        OnPropertyChanged(nameof(ExtractedTextPreview));
        OnPropertyChanged(nameof(AiSummaryPreview));
        OnPropertyChanged(nameof(HasSelectedDocument));
        OnPropertyChanged(nameof(HasExtractedText));
        OnPropertyChanged(nameof(HasAiSummary));
    }

    public string SelectedFileName =>
        SelectedDocument?.FileName ?? "No file selected";

    public string ExtractedTextPreview =>
        SelectedDocument?.ExtractedText ?? string.Empty;

    public string AiSummaryPreview =>
        SelectedDocument?.AiSummary ?? string.Empty;

    public bool HasSelectedDocument =>
        SelectedDocument is not null;

    public bool HasExtractedText =>
        !string.IsNullOrWhiteSpace(SelectedDocument?.ExtractedText);

    public bool HasAiSummary =>
        !string.IsNullOrWhiteSpace(SelectedDocument?.AiSummary);

    public async Task InitializeAsync()
    {
        IsAiSummaryAvailable = await _summaryService.IsAvailableAsync();
        AiSummaryStatus = IsAiSummaryAvailable
            ? "Ollama detected"
            : "AI Summary requires Ollama running locally";
    }

    [RelayCommand]
    private async Task BrowsePdfAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF files (*.pdf)|*.pdf",
            Multiselect = true,
            Title = "Select one or more PDF files"
        };

        if (dialog.ShowDialog() != true)
            return;

        await AddPdfFilesAsync(dialog.FileNames);
    }

    public Task AddPdfFilesAsync(IEnumerable<string> paths)
    {
        var validPaths = paths
            .Where(File.Exists)
            .Where(p => string.Equals(Path.GetExtension(p), ".pdf", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validPaths.Count == 0)
        {
            StatusMessage = "No valid PDF files found";
            return Task.CompletedTask;
        }

        int added = 0;

        foreach (var path in validPaths)
        {
            if (Documents.Any(d => string.Equals(d.SourceFilePath, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            Documents.Add(new ProcessedPdfItem
            {
                SourceFilePath = path,
                Status = "Pending"
            });

            added++;
        }

        if (SelectedDocument is null && Documents.Count > 0)
            SelectedDocument = Documents[0];

        StatusMessage = added > 0
            ? $"{added} PDF(s) added"
            : "All dropped PDFs were already loaded";

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ClearDocuments()
    {
        Documents.Clear();
        SelectedDocument = null;
        StatusMessage = "Document list cleared";
    }

    [RelayCommand]
    private async Task ExtractAllAsync()
    {
        if (Documents.Count == 0 || IsBusy)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = $"Extracting text from {Documents.Count} PDF(s)...";

            foreach (var document in Documents)
            {
                document.Status = "Extracting...";

                var result = await _pdfTextExtractor.ExtractTextAsync(document.SourceFilePath);

                if (!result.Success)
                {
                    document.Status = $"Error: {result.ErrorMessage}";
                    _logger.LogError("Extraction failed for {FilePath}. Error: {Error}",
                        document.SourceFilePath, result.ErrorMessage);
                    continue;
                }

                document.ExtractedText = _textStructureReconstructor.Reconstruct(result.ExtractedText);
                document.IsExtracted = true;
                document.Status = $"Extracted ({result.PageCount} pages)";
            }

            OnPropertyChanged(nameof(ExtractedTextPreview));
            OnPropertyChanged(nameof(HasExtractedText));

            StatusMessage = "Extraction finished";
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during extraction";
            _logger.LogError(ex, "Unexpected error during multi-PDF extraction");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateSelectedAiSummaryAsync()
    {
        if (IsBusy || SelectedDocument is null || string.IsNullOrWhiteSpace(SelectedDocument.ExtractedText) || !IsAiSummaryAvailable)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = $"Generating AI summary for {SelectedDocument.FileName}...";
            SelectedDocument.Status = "Generating AI summary...";

            SelectedDocument.AiSummary = await _summaryService.SummarizeAsync(SelectedDocument.ExtractedText);
            SelectedDocument.IsAiSummarized = !string.IsNullOrWhiteSpace(SelectedDocument.AiSummary);
            SelectedDocument.Status = "AI summary generated";

            OnPropertyChanged(nameof(AiSummaryPreview));
            OnPropertyChanged(nameof(HasAiSummary));

            StatusMessage = "AI summary generated";
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during AI summary";
            _logger.LogError(ex, "Unexpected error during AI summary for {FilePath}", SelectedDocument.SourceFilePath);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateAllAiSummariesAsync()
    {
        if (IsBusy || !IsAiSummaryAvailable)
            return;

        var docsToSummarize = Documents
            .Where(d => !string.IsNullOrWhiteSpace(d.ExtractedText))
            .ToList();

        if (docsToSummarize.Count == 0)
        {
            StatusMessage = "No extracted documents available to summarize";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"Generating AI summaries for {docsToSummarize.Count} document(s)...";

            foreach (var document in docsToSummarize)
            {
                document.Status = "Generating AI summary...";
                document.AiSummary = await _summaryService.SummarizeAsync(document.ExtractedText);
                document.IsAiSummarized = !string.IsNullOrWhiteSpace(document.AiSummary);
                document.Status = "AI summary generated";
            }

            OnPropertyChanged(nameof(AiSummaryPreview));
            OnPropertyChanged(nameof(HasAiSummary));

            StatusMessage = "All AI summaries generated";
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error during AI summaries";
            _logger.LogError(ex, "Unexpected error during AI summaries");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportSelectedTextTxtAsync()
    {
        if (SelectedDocument is null || string.IsNullOrWhiteSpace(SelectedDocument.ExtractedText) || IsBusy)
            return;

        try
        {
            IsBusy = true;

            var path = BuildExtractedTextPath(SelectedDocument, ".txt");
            await _textExportService.ExportAsync(path, SelectedDocument.ExtractedText);

            StatusMessage = $"Exported: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting selected TXT";
            _logger.LogError(ex, "Error exporting selected extracted TXT");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportSelectedTextDocxAsync()
    {
        if (SelectedDocument is null || string.IsNullOrWhiteSpace(SelectedDocument.ExtractedText) || IsBusy)
            return;

        try
        {
            IsBusy = true;

            var path = BuildExtractedTextPath(SelectedDocument, ".docx");
            await _docxExportService.ExportAsync(path, SelectedDocument.ExtractedText);

            StatusMessage = $"Exported: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting selected DOCX";
            _logger.LogError(ex, "Error exporting selected extracted DOCX");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllTextTxtAsync()
    {
        if (IsBusy)
            return;

        var docs = Documents.Where(d => !string.IsNullOrWhiteSpace(d.ExtractedText)).ToList();
        if (docs.Count == 0)
        {
            StatusMessage = "No extracted texts to export";
            return;
        }

        try
        {
            IsBusy = true;

            foreach (var doc in docs)
            {
                var path = BuildExtractedTextPath(doc, ".txt");
                await _textExportService.ExportAsync(path, doc.ExtractedText);
            }

            StatusMessage = $"Exported {docs.Count} extracted TXT file(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting extracted TXT files";
            _logger.LogError(ex, "Error exporting all extracted TXT files");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllTextDocxAsync()
    {
        if (IsBusy)
            return;

        var docs = Documents.Where(d => !string.IsNullOrWhiteSpace(d.ExtractedText)).ToList();
        if (docs.Count == 0)
        {
            StatusMessage = "No extracted documents to export";
            return;
        }

        try
        {
            IsBusy = true;

            foreach (var doc in docs)
            {
                var path = BuildExtractedTextPath(doc, ".docx");
                await _docxExportService.ExportAsync(path, doc.ExtractedText);
            }

            StatusMessage = $"Exported {docs.Count} extracted DOCX file(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting extracted DOCX files";
            _logger.LogError(ex, "Error exporting all extracted DOCX files");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportSelectedAiSummaryTxtAsync()
    {
        if (SelectedDocument is null || string.IsNullOrWhiteSpace(SelectedDocument.AiSummary) || IsBusy)
            return;

        try
        {
            IsBusy = true;

            var path = BuildAiSummaryPath(SelectedDocument, ".txt");
            await _textExportService.ExportAsync(path, SelectedDocument.AiSummary);

            StatusMessage = $"Exported: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting selected AI summary TXT";
            _logger.LogError(ex, "Error exporting selected AI summary TXT");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportSelectedAiSummaryDocxAsync()
    {
        if (SelectedDocument is null || string.IsNullOrWhiteSpace(SelectedDocument.AiSummary) || IsBusy)
            return;

        try
        {
            IsBusy = true;

            var path = BuildAiSummaryPath(SelectedDocument, ".docx");
            await _docxExportService.ExportAsync(path, SelectedDocument.AiSummary);

            StatusMessage = $"Exported: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting selected AI summary DOCX";
            _logger.LogError(ex, "Error exporting selected AI summary DOCX");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllAiSummaryTxtAsync()
    {
        if (IsBusy)
            return;

        var docs = Documents.Where(d => !string.IsNullOrWhiteSpace(d.AiSummary)).ToList();
        if (docs.Count == 0)
        {
            StatusMessage = "No AI summaries to export";
            return;
        }

        try
        {
            IsBusy = true;

            foreach (var doc in docs)
            {
                var path = BuildAiSummaryPath(doc, ".txt");
                await _textExportService.ExportAsync(path, doc.AiSummary);
            }

            StatusMessage = $"Exported {docs.Count} AI summary TXT file(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting AI summary TXT files";
            _logger.LogError(ex, "Error exporting all AI summary TXT files");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllAiSummaryDocxAsync()
    {
        if (IsBusy)
            return;

        var docs = Documents.Where(d => !string.IsNullOrWhiteSpace(d.AiSummary)).ToList();
        if (docs.Count == 0)
        {
            StatusMessage = "No AI summaries to export";
            return;
        }

        try
        {
            IsBusy = true;

            foreach (var doc in docs)
            {
                var path = BuildAiSummaryPath(doc, ".docx");
                await _docxExportService.ExportAsync(path, doc.AiSummary);
            }

            StatusMessage = $"Exported {docs.Count} AI summary DOCX file(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error exporting AI summary DOCX files";
            _logger.LogError(ex, "Error exporting all AI summary DOCX files");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildExtractedTextPath(ProcessedPdfItem document, string extension)
    {
        var fileName = SanitizeFileName(document.BaseFileName) + extension;
        return Path.Combine(document.DirectoryPath, fileName);
    }

    private static string BuildAiSummaryPath(ProcessedPdfItem document, string extension)
    {
        var fileName = SanitizeFileName(document.BaseFileName) + "_ai_summary" + extension;
        return Path.Combine(document.DirectoryPath, fileName);
    }

    private static string SanitizeFileName(string input)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(invalid, '_');
        }

        return input;
    }
}