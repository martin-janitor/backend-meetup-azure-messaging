using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models.EventGrid;

/// <summary>
/// Represents an envelope that contains Event Hub metadata and the Event Grid message
/// </summary>
public class EventGridEventHubEnvelope
{
    /// <summary>
    /// Partition key from Event Hub
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number from Event Hub
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Time when the message was enqueued in Event Hub
    /// </summary>
    public DateTimeOffset EnqueuedTime { get; set; }

    /// <summary>
    /// The Event Grid message
    /// </summary>
    public EventGridMessage Data { get; set; } = new();
}