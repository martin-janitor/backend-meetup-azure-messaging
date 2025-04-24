using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Options;
using Visma.BackendMeetup.Demo.MessageService.Configuration;
using Visma.BackendMeetup.Demo.Models;

namespace Visma.BackendMeetup.Demo.MessageService.Handlers;

public class EventGridMessageHandler
{
    private readonly EventGridOptions _options;
    private readonly ILogger<EventGridMessageHandler> _logger;
    private readonly EventGridPublisherClient _eventGridClient;

    public EventGridMessageHandler(
        IOptions<EventGridOptions> options,
        ILogger<EventGridMessageHandler> logger,
        EventGridPublisherClient eventGridClient)
    {
        _options = options.Value;
        _logger = logger;
        _eventGridClient = eventGridClient;
    }

    public async Task SendMessageAsync(string message)
    {
        try
        {
            // Deserialize the message to get batch settings
            var messageModel = JsonSerializer.Deserialize<MessageModel>(message);
            if (messageModel?.Body == null)
            {
                throw new ArgumentException("Invalid message format. Missing required fields.");
            }

            // Extract values from message model and properties
            int messageCount = messageModel.MessageCount; // Use MessageCount directly
            int timeoutSeconds = 30;
            string? messageGroup = messageModel.MessageGroup;

            // Get timeout value from Properties collection if available
            if (messageModel.Properties != null && messageModel.Properties.Any())
            {
                var timeoutProperty = messageModel.Properties.FirstOrDefault(p => p.Key == "Timeout");

                if (timeoutProperty != null && int.TryParse(timeoutProperty.Value, out int timeout))
                {
                    timeoutSeconds = timeout;
                }
            }

            // Validate Event Grid topic name if needed
            var topicName = !string.IsNullOrEmpty(_options.TopicName)
                ? _options.TopicName
                : "default-topic"; // Use a default or log a warning

            // Setup cancellation token based on timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            // Track successful message count
            int sentMessageCount = 0;

            try
            {
                // Collection for batch sending
                var eventGridEvents = new List<EventGridEvent>();

                // Prepare messages based on messageCount
                for (int i = 0; i < messageCount && !cts.Token.IsCancellationRequested; i++)
                {
                    // Create a message with incrementing index in the content
                    var currentMessageBody = new MessageBody
                    {
                        Recipient = messageModel.Body.Recipient,
                        Subject = messageModel.Body.Subject,
                        Content = $"{messageModel.Body.Content} (Message {i + 1} of {messageCount})",
                        DelaySec = messageModel.Body.DelaySec
                    };

                    // Construct dynamic subject with message group if available
                    string subjectPrefix = !string.IsNullOrEmpty(messageGroup)
                        ? $"{topicName}/{messageGroup}"
                        : $"{topicName}";

                    // Extract properties into a dictionary for data payload
                    var propertiesDict = new Dictionary<string, string>();
                    if (messageModel.Properties != null && messageModel.Properties.Any())
                    {
                        foreach (var prop in messageModel.Properties)
                        {
                            if (!string.IsNullOrEmpty(prop.Key) && !string.IsNullOrEmpty(prop.Value))
                            {
                                propertiesDict[prop.Key] = prop.Value;
                            }
                        }
                    }

                    // Create a composite data object that includes both the message body and properties
                    var eventData = new
                    {
                        body = currentMessageBody,
                        // Include properties at the root level for easy access
                        groupId = propertiesDict.ContainsKey("groupId") ? propertiesDict["groupId"] : "",
                        priority = propertiesDict.ContainsKey("Priority") ? propertiesDict["Priority"] : "",
                        messageType = propertiesDict.ContainsKey("MessageType") ? propertiesDict["MessageType"] : "",
                        // Also include all properties as a dictionary
                        properties = propertiesDict
                    };

                    // Create an individual event grid event with the composite data
                    var eventGridEvent = new EventGridEvent(
                        subject: $"{subjectPrefix}/MessagePublished/{i + 1}",
                        eventType: messageGroup ?? "MessagePublished",
                        dataVersion: "1.0",
                        data: new BinaryData(JsonSerializer.Serialize(eventData)));

                    eventGridEvents.Add(eventGridEvent);
                    sentMessageCount++;

                    // Send in batches of 10 to avoid any size limitations
                    if (eventGridEvents.Count >= 10)
                    {
                        await _eventGridClient.SendEventsAsync(eventGridEvents, cts.Token);

                        eventGridEvents.Clear();
                    }
                }

                // Send any remaining events
                if (eventGridEvents.Count > 0 && !cts.Token.IsCancellationRequested)
                {
                    await _eventGridClient.SendEventsAsync(eventGridEvents, cts.Token);
                }

                _logger.LogInformation("Successfully sent {count}/{total} messages to Event Grid.",
                    sentMessageCount, messageCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sending operation was canceled after timeout of {seconds} seconds. Sent {count}/{total} messages.",
                    timeoutSeconds, sentMessageCount, messageCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message to Event Grid: {ex.Message}");
            throw;
        }
    }
}