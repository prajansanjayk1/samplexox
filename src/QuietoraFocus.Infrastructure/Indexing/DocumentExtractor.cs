using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Infrastructure.Indexing;

public static class DocumentExtractor
{
    private static readonly Regex PdfTextRegex = new(@"\((.*?)\)\s*Tj|\[(.*?)\]\s*TJ", RegexOptions.Compiled);

    public static async Task<(string Content, string ContentHash)> ExtractContentAsync(
        string filePath,
        int maxReadChars = 64 * 1024,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return (string.Empty, string.Empty);

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        string content;

        if (ext == ".pdf")
        {
            content = await ExtractPdfContentAsync(filePath, ct);
        }
        else
        {
            content = await ExtractTextContentAsync(filePath, maxReadChars, ct);
        }

        var hash = ComputeHash(content);
        return (content, hash);
    }

    private static async Task<string> ExtractTextContentAsync(string filePath, int maxChars, CancellationToken ct)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var buffer = new char[maxChars];
            var count = await reader.ReadAsync(buffer.AsMemory(0, maxChars), ct);
            return new string(buffer, 0, count);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Could not extract text from {filePath}: {ex.Message}");
            return string.Empty;
        }
    }

    private static async Task<string> ExtractPdfContentAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var rawBytes = await File.ReadAllBytesAsync(filePath, ct);
            var rawText = Encoding.Latin1.GetString(rawBytes);

            var sb = new StringBuilder();
            var matches = PdfTextRegex.Matches(rawText);

            foreach (Match match in matches)
            {
                if (ct.IsCancellationRequested) break;
                var val = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                if (!string.IsNullOrWhiteSpace(val))
                {
                    sb.Append(val).Append(' ');
                }
            }

            var result = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(result))
            {
                result = $"PDF Document: {Path.GetFileName(filePath)}";
            }

            return result.Length > 64 * 1024 ? result[..(64 * 1024)] : result;
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"PDF extraction fallback for {filePath}: {ex.Message}");
            return $"PDF Document: {Path.GetFileName(filePath)}";
        }
    }

    public static string ComputeHash(string content)
    {
        if (string.IsNullOrEmpty(content)) return "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // Empty SHA256
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
