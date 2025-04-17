using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models;

public class MessageHeader
{
    [JsonPropertyName("properties")]
    public MessageProperties? Properties { get; set; }
    
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; } = 1;
    
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30; // Default timeout in seconds
}