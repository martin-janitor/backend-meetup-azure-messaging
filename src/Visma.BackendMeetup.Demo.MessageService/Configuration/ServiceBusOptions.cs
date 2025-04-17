namespace Visma.BackendMeetup.Demo.MessageService.Configuration;

public class ServiceBusOptions
{
    public const string ServiceBus = "ServiceBus";

    /// <summary>
    /// Fully qualified namespace, e.g., "myservicebus.servicebus.windows.net"
    /// </summary>
    public string FullyQualifiedNamespace { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the Service Bus queue
    /// </summary>
    public string TopicName { get; set; } = string.Empty;
}