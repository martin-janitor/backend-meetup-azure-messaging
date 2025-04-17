using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models;

public class MessageProperties
{
    [JsonPropertyName("content-type")]
    public string ContentType { get; set; } = "application/json";
    
    [JsonPropertyName("priority")]
    public string Partition { get; set; } = "normal";
    
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}