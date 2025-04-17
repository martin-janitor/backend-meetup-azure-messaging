using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models;

public class MessageModel
{
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; } = 1;

    [JsonPropertyName("messageGroup")]
    public string? MessageGroup { get; set; }

    [JsonPropertyName("properties")]
    public IList<MessageProperties> Properties { get; set; } = new List<MessageProperties>();

    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }
}