using System.Text;
using System.Text.RegularExpressions;
using DocForge.Application.Abstractions;

namespace DocForge.Infrastructure.Services;

public sealed class TextStructureReconstructor : ITextStructureReconstructor
{
    public string Reconstruct(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var normalized = rawText
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        var lines = normalized
            .Split('\n')
            .Select(l => l.Trim())
            .ToList();

        var paragraphs = new List<string>();
        var currentParagraph = new StringBuilder();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(currentParagraph, paragraphs);
                continue;
            }

            if (IsStandaloneBlock(line))
            {
                FlushParagraph(currentParagraph, paragraphs);
                paragraphs.Add(line);
                continue;
            }

            if (currentParagraph.Length == 0)
            {
                currentParagraph.Append(line);
                continue;
            }

            if (ShouldStartNewParagraph(currentParagraph.ToString(), line))
            {
                FlushParagraph(currentParagraph, paragraphs);
                currentParagraph.Append(line);
            }
            else
            {
                AppendInline(currentParagraph, line);
            }
        }

        FlushParagraph(currentParagraph, paragraphs);

        return string.Join(Environment.NewLine + Environment.NewLine, paragraphs)
            .Trim();
    }

    private static void FlushParagraph(StringBuilder builder, List<string> paragraphs)
    {
        if (builder.Length == 0)
            return;

        var text = CleanupParagraph(builder.ToString());

        if (!string.IsNullOrWhiteSpace(text))
            paragraphs.Add(text);

        builder.Clear();
    }

    private static void AppendInline(StringBuilder builder, string line)
    {
        var previous = builder.ToString();

        if (EndsWithHyphen(previous))
        {
            builder.Length--;
            builder.Append(line);
            return;
        }

        if (!previous.EndsWith(' ') && !line.StartsWith(' '))
            builder.Append(' ');

        builder.Append(line);
    }

    private static bool ShouldStartNewParagraph(string currentParagraph, string nextLine)
    {
        if (EndsWithTerminalPunctuation(currentParagraph))
            return true;

        if (LooksLikeHeading(nextLine))
            return true;

        return false;
    }

    private static bool IsStandaloneBlock(string line)
    {
        return IsBullet(line) || LooksLikeHeading(line);
    }

    private static bool IsBullet(string line)
    {
        return Regex.IsMatch(line, @"^(\-|\*|•|\d+[\.\)])\s+");
    }

    private static bool LooksLikeHeading(string line)
    {
        if (line.Length > 80)
            return false;

        if (IsBullet(line))
            return false;

        var letters = line.Count(char.IsLetter);
        if (letters == 0)
            return false;

        var uppercase = line.Count(char.IsUpper);
        var ratio = (double)uppercase / letters;

        return ratio > 0.6 || line.EndsWith(':');
    }

    private static bool EndsWithTerminalPunctuation(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var last = text.TrimEnd();
        return last.EndsWith('.') || last.EndsWith('!') || last.EndsWith('?') || last.EndsWith(':');
    }

    private static bool EndsWithHyphen(string text)
    {
        return text.TrimEnd().EndsWith("-");
    }

    private static string CleanupParagraph(string text)
    {
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}