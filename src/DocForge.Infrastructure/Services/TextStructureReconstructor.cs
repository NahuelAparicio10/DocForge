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
            .Replace('\r', '\n')
            .Replace("\t", " ");

        var rawLines = normalized.Split('\n');
        var lines = rawLines
            .Select(l => NormalizeLine(l))
            .ToList();

        var blocks = new List<string>();
        var currentParagraph = new StringBuilder();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(currentParagraph, blocks);
                continue;
            }

            if (IsHeading(line))
            {
                FlushParagraph(currentParagraph, blocks);
                blocks.Add(line);
                continue;
            }

            if (IsBullet(line))
            {
                FlushParagraph(currentParagraph, blocks);
                blocks.Add(line);
                continue;
            }

            if (currentParagraph.Length == 0)
            {
                currentParagraph.Append(line);
                continue;
            }

            var currentText = currentParagraph.ToString();

            if (ShouldStartNewParagraph(currentText, line))
            {
                FlushParagraph(currentParagraph, blocks);
                currentParagraph.Append(line);
            }
            else
            {
                AppendToParagraph(currentParagraph, line);
            }
        }

        FlushParagraph(currentParagraph, blocks);

        return string.Join(Environment.NewLine + Environment.NewLine, blocks).Trim();
    }

    private static string NormalizeLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return string.Empty;

        line = line.Trim();
        line = Regex.Replace(line, @"\s+", " ");
        return line;
    }

    private static void AppendToParagraph(StringBuilder paragraph, string nextLine)
    {
        if (paragraph.Length == 0)
        {
            paragraph.Append(nextLine);
            return;
        }

        var current = paragraph.ToString();

        if (EndsWithHyphen(current))
        {
            paragraph.Length--;
            paragraph.Append(nextLine);
            return;
        }

        if (!current.EndsWith(' '))
            paragraph.Append(' ');

        paragraph.Append(nextLine);
    }

    private static void FlushParagraph(StringBuilder paragraph, List<string> blocks)
    {
        if (paragraph.Length == 0)
            return;

        var cleaned = CleanupParagraph(paragraph.ToString());

        if (!string.IsNullOrWhiteSpace(cleaned))
            blocks.Add(cleaned);

        paragraph.Clear();
    }

    private static bool ShouldStartNewParagraph(string currentParagraph, string nextLine)
    {
        if (IsHeading(nextLine) || IsBullet(nextLine))
            return true;

        if (EndsWithStrongPunctuation(currentParagraph) && StartsLikeSentence(nextLine))
            return true;

        if (IsVeryShortLine(currentParagraph) && StartsLikeSentence(nextLine))
            return false;

        return false;
    }

    private static bool IsBullet(string line)
    {
        return Regex.IsMatch(line, @"^(\-|\*|•|▪|◦|\d+[\.\)])\s+");
    }

    private static bool IsHeading(string line)
    {
        if (line.Length == 0 || line.Length > 90)
            return false;

        if (IsBullet(line))
            return false;

        if (line.EndsWith(":"))
            return true;

        var letters = line.Count(char.IsLetter);
        if (letters == 0)
            return false;

        var uppercase = line.Count(char.IsUpper);
        var uppercaseRatio = (double)uppercase / letters;

        return uppercaseRatio > 0.7;
    }

    private static bool StartsLikeSentence(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var firstLetter = line.FirstOrDefault(char.IsLetter);
        return firstLetter != default && char.IsUpper(firstLetter);
    }

    private static bool EndsWithStrongPunctuation(string text)
    {
        var trimmed = text.TrimEnd();
        return trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?') || trimmed.EndsWith(':');
    }

    private static bool EndsWithHyphen(string text)
    {
        return text.TrimEnd().EndsWith("-");
    }

    private static bool IsVeryShortLine(string text)
    {
        return text.Trim().Length < 45;
    }

    private static string CleanupParagraph(string text)
    {
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}