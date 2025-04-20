using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models;

/// <summary>
/// Envelope class that wraps a MessageBody with Event Hub specific metadata
/// </summary>
public class EventHubMessageEnvelope
{
    /// <summary>
    /// The partition key used for the message in Event Hub
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string? PartitionKey { get; set; }
    
    /// <summary>
    /// The sequence number of the message in its partition
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public long SequenceNumber { get; set; }
    
    /// <summary>
    /// The time when the message was enqueued in Event Hub
    /// </summary>
    [JsonPropertyName("enqueuedTime")]
    public DateTimeOffset EnqueuedTime { get; set; }
    
    /// <summary>
    /// The actual message data
    /// </summary>
    [JsonPropertyName("data")]
    public MessageBody? Data { get; set; }
}