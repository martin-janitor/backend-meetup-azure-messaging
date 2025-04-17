using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models;

public class MessageModel
{
    [JsonPropertyName("header")]
    public MessageHeader? Header { get; set; }
    
    [JsonPropertyName("messageBody")]
    public MessageBody? MessageBody { get; set; }
}