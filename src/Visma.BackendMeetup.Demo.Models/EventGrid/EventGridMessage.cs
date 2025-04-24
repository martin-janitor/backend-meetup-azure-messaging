using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models.EventGrid;

/// <summary>
/// Represents the structure of an Event Grid message
/// </summary>
public class EventGridMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public EventGridData Data { get; set; } = new();

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("dataVersion")]
    public string DataVersion { get; set; } = string.Empty;

    [JsonPropertyName("metadataVersion")]
    public string MetadataVersion { get; set; } = string.Empty;

    [JsonPropertyName("eventTime")]
    public DateTime EventTime { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
}