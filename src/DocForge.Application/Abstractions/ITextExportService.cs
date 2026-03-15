namespace DocForge.Application.Abstractions;

public interface ITextExportService
{
    Task ExportAsync(string destinationPath, string content, CancellationToken cancellationToken = default);
}