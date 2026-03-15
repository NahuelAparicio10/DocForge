using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace DocForge.App.ViewModels;

public partial class ProcessedPdfItem : ObservableObject
{
    [ObservableProperty]
    private string sourceFilePath = string.Empty;

    [ObservableProperty]
    private string extractedText = string.Empty;

    [ObservableProperty]
    private string aiSummary = string.Empty;

    [ObservableProperty]
    private bool isExtracted;

    [ObservableProperty]
    private bool isAiSummarized;

    [ObservableProperty]
    private string status = "Pending";

    public string FileName =>
        string.IsNullOrWhiteSpace(SourceFilePath)
            ? "Unknown PDF"
            : Path.GetFileName(SourceFilePath);

    public string BaseFileName =>
        string.IsNullOrWhiteSpace(SourceFilePath)
            ? "document"
            : Path.GetFileNameWithoutExtension(SourceFilePath);

    public string DirectoryPath =>
        string.IsNullOrWhiteSpace(SourceFilePath)
            ? string.Empty
            : Path.GetDirectoryName(SourceFilePath) ?? string.Empty;
}