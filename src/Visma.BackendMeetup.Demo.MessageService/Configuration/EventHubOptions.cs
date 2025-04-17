namespace Visma.BackendMeetup.Demo.MessageService.Configuration;

public class EventHubOptions
{
    public const string EventHub = "EventHub";

    /// <summary>
    /// Fully qualified namespace, e.g., "myeventhub.servicebus.windows.net"
    /// </summary>
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the Event Hub
    /// </summary>
    public string Name { get; set; } = string.Empty;
}