using System.Text;
using DocForge.Application.Abstractions;
using DocForge.Domain.Models;
using UglyToad.PdfPig;

namespace DocForge.Infrastructure.Services;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<ExtractionResult> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(new ExtractionResult
                {
                    SourceFilePath = filePath,
                    Success = false,
                    ErrorMessage = "The file path is null or empty."
                });
            }

            if (!File.Exists(filePath))
            {
                return Task.FromResult(new ExtractionResult
                {
                    SourceFilePath = filePath,
                    Success = false,
                    ErrorMessage = "The specified PDF file does not exist."
                });
            }

            var builder = new StringBuilder();

            using var document = PdfDocument.Open(filePath);

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AppendLine(page.Text);
                builder.AppendLine();
            }

            return Task.FromResult(new ExtractionResult
            {
                SourceFilePath = filePath,
                ExtractedText = builder.ToString(),
                PageCount = document.NumberOfPages,
                Success = true
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ExtractionResult
            {
                SourceFilePath = filePath,
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }
}