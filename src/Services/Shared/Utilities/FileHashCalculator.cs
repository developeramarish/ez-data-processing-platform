using System.Security.Cryptography;
using System.Text;
using DataProcessing.Shared.Connectors;

namespace DataProcessing.Shared.Utilities;

/// <summary>
/// Utility class for calculating file hashes for deduplication
/// Uses SHA256 hash of (filePath|fileSize|lastModified) to uniquely identify files
/// </summary>
public static class FileHashCalculator
{
    /// <summary>
    /// Calculates a unique hash for a file based on its metadata
    /// Format: SHA256(filePath|fileSize|lastModifiedUtc)
    /// </summary>
    /// <param name="file">File metadata</param>
    /// <returns>Base64-encoded SHA256 hash</returns>
    public static string CalculateHash(FileMetadata file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        return CalculateHash(
            file.FilePath,
            file.FileSizeBytes,
            file.LastModifiedUtc);
    }

    /// <summary>
    /// Calculates a unique hash for a file based on its properties
    /// Format: SHA256(filePath|fileSize|lastModifiedUtc)
    /// </summary>
    /// <param name="filePath">Full file path</param>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <param name="lastModifiedUtc">Last modified UTC timestamp</param>
    /// <returns>Base64-encoded SHA256 hash</returns>
    public static string CalculateHash(
        string filePath,
        long fileSizeBytes,
        DateTime lastModifiedUtc)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        // Normalize file path (case-insensitive, forward slashes)
        var normalizedPath = NormalizePath(filePath);

        // Create composite string: filePath|fileSize|lastModifiedISO8601
        var composite = $"{normalizedPath}|{fileSizeBytes}|{lastModifiedUtc:O}";

        // Calculate SHA256 hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(composite));

        // Return Base64-encoded hash (shorter than hex)
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Normalizes a file path for consistent hashing
    /// - Converts to lowercase
    /// - Replaces backslashes with forward slashes
    /// - Trims whitespace
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path
            .Trim()
            .Replace('\\', '/')
            .ToLowerInvariant();
    }

    /// <summary>
    /// Validates if a hash string is in the expected format
    /// </summary>
    /// <param name="hash">Hash string to validate</param>
    /// <returns>True if hash is valid Base64 string, false otherwise</returns>
    public static bool IsValidHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            Convert.FromBase64String(hash);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
