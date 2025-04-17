using System.Text.Json.Serialization;

namespace Visma.BackendMeetup.Demo.Models;

public class MessageProperties
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}