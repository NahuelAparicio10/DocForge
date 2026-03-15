namespace DocForge.Application.Abstractions;

public interface ISummaryService
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<string> SummarizeAsync(string text, CancellationToken ct = default);
}