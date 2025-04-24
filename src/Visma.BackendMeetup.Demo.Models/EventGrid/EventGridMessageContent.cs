using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models.EventGrid;

/// <summary>
/// Represents the message content of an Event Grid message
/// </summary>
public class EventGridMessageContent
{
    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("delaySec")]
    public int DelaySec { get; set; }
}