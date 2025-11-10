using System.ComponentModel.DataAnnotations;

namespace DataProcessing.Shared.Messages;

/// <summary>
/// Base interface for all Data Processing Platform messages
/// Ensures correlation ID tracking and timestamp consistency across all messages
/// </summary>
public interface IDataProcessingMessage
{
    /// <summary>
    /// Unique correlation ID for tracing requests across services
    /// Must be populated for all messages to enable distributed tracing
    /// </summary>
    [Required]
    string CorrelationId { get; set; }

    /// <summary>
    /// UTC timestamp when the message was created
    /// Used for message ordering and processing time analysis
    /// </summary>
    [Required]
    DateTime Timestamp { get; set; }

    /// <summary>
    /// Name of the service that published this message
    /// Used for debugging and message routing analysis
    /// </summary>
    [Required]
    string PublishedBy { get; set; }

    /// <summary>
    /// Version of the message schema for compatibility tracking
    /// </summary>
    int MessageVersion { get; set; }
}
