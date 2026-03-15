using DocForge.Domain.Models;

namespace DocForge.Application.Abstractions;

public interface IPdfTextExtractor
{
    Task<ExtractionResult> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
}