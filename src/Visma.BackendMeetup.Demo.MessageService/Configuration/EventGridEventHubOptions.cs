namespace Visma.BackendMeetup.Demo.MessageService.Configuration;

public class EventGridEventHubOptions
{
    public const string EventGridEventHub = "EventGridEventHub";

    /// <summary>
    /// Fully qualified namespace, e.g., "myeventhub.servicebus.windows.net"
    /// </summary>
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the Event Hub
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The consumer group to use when reading from Event Hub
    /// </summary>
    public string ConsumerGroup { get; set; } = "$Default";
}