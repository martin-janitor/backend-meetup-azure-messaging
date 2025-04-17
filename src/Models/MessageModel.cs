using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.FunctionApp.Models;

public class MessageModel
{
    [JsonPropertyName("header")]
    public MessageHeader? Header { get; set; }
    
    [JsonPropertyName("messageBody")]
    public MessageBody? MessageBody { get; set; }
}

public class MessageHeader
{
    [JsonPropertyName("properties")]
    public MessageProperties? Properties { get; set; }
    
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; } = 1;
    
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30; // Default timeout in seconds
}

public class MessageProperties
{
    [JsonPropertyName("content-type")]
    public string ContentType { get; set; } = "application/json";
    
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "normal";
    
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;
}

public class MessageBody
{
    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }
    
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}