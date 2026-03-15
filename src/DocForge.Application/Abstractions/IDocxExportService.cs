namespace DocForge.Application.Abstractions;

public interface IDocxExportService
{
    Task ExportAsync(string destinationPath, string content, CancellationToken cancellationToken = default);
}