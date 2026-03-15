using DocForge.Application.Abstractions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocForge.Infrastructure.Services;

public sealed class DocxExportService : IDocxExportService
{
    public Task ExportAsync(string destinationPath, string content, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(destinationPath);

        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var document = WordprocessingDocument.Create(
            destinationPath,
            WordprocessingDocumentType.Document);

        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = new Body();

        var normalized = (content ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim();

        var blocks = normalized
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var block in blocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var linesInBlock = block
                .Split('\n', StringSplitOptions.TrimEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var paragraph = new Paragraph();
            var run = new Run();

            for (int i = 0; i < linesInBlock.Count; i++)
            {
                if (i > 0)
                    run.Append(new Break());

                run.Append(new Text(linesInBlock[i])
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
            }

            paragraph.Append(run);
            body.Append(paragraph);
        }

        mainPart.Document.Append(body);
        mainPart.Document.Save();

        return Task.CompletedTask;
    }
}