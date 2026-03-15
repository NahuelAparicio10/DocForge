using DocForge.Application.Abstractions;

namespace DocForge.Infrastructure.Services;

public sealed class TxtExportService : ITextExportService
{
    public async Task ExportAsync(string destinationPath, string content, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(destinationPath, content, cancellationToken);
    }
}