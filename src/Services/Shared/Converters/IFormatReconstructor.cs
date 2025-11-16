namespace DataProcessing.Shared.Converters;

/// <summary>
/// Interface for reconstructing original file formats from JSON
/// Used by OutputService to convert validated JSON back to original formats
/// </summary>
public interface IFormatReconstructor
{
    /// <summary>
    /// Gets the target format this reconstructor produces (e.g., "csv", "xml", "excel")
    /// </summary>
    string TargetFormat { get; }

    /// <summary>
    /// Reconstructs the original format from JSON
    /// </summary>
    /// <param name="jsonContent">JSON string to reconstruct from</param>
    /// <param name="metadata">Format metadata extracted during conversion (delimiters, encoding, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream containing the reconstructed format</returns>
    Task<Stream> ReconstructFromJsonAsync(
        string jsonContent,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the JSON can be reconstructed to this format
    /// </summary>
    /// <param name="jsonContent">JSON to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> CanReconstructAsync(
        string jsonContent,
        CancellationToken cancellationToken = default);
}
