using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models;

public class MessageBody
{
    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }
    
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}