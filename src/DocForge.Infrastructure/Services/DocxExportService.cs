using DocForge.Application.Abstractions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocForge.Infrastructure.Services;

public class DocxExportService : IDocxExportService
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

        var lines = (content).Replace("\r\n", "\n").Split('\n');

        foreach (var line in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var paragraph = new Paragraph(new Run(new Text(line)
            {
                Space = SpaceProcessingModeValues.Preserve
            }));

            body.Append(paragraph);
        }

        mainPart.Document.Append(body);
        mainPart.Document.Save();

        return Task.CompletedTask;
    }
    
}