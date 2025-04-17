namespace Visma.BackendMeetup.Demo.MessageService.Configuration;

public class EventGridOptions
{
    public const string EventGrid = "EventGrid";

    /// <summary>
    /// The Event Grid endpoint URI, e.g., "https://myeventgrid.westus2-1.eventgrid.azure.net"
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// The Event Grid topic name
    /// </summary>
    public string TopicName { get; set; } = string.Empty;
}