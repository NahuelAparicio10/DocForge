namespace DocForge.Domain.Models;

public sealed class ExtractionResult
{
    public string SourceFilePath { get; init; } = string.Empty;
    public string ExtractedText { get; init; } = string.Empty;
    public int PageCount { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}