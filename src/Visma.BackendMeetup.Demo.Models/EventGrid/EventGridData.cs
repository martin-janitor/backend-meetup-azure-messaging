using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models.EventGrid;

/// <summary>
/// Represents the data structure within an Event Grid message
/// </summary>
public class EventGridData
{
    [JsonPropertyName("message")]
    public EventGridMessageContent Message { get; set; } = new();

    [JsonPropertyName("groupId")]
    public string GroupId { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}