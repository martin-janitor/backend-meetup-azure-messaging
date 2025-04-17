using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Visma.BackendMeetup.Demo.MessageService.Configuration;

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
            if (messageModel?.Header == null || messageModel.MessageBody == null)
            {
                throw new ArgumentException("Invalid message format. Missing required fields.");
            }

            int messageCount = messageModel.Header.MessageCount;
            int timeoutSeconds = messageModel.Header.Timeout;
            
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
                    var currentMessage = new MessageModel
                    {
                        Header = messageModel.Header,
                        MessageBody = new MessageBody
                        {
                            Recipient = messageModel.MessageBody.Recipient,
                            Subject = messageModel.MessageBody.Subject,
                            Content = $"{messageModel.MessageBody.Content} (Message {i+1} of {messageCount})"
                        }
                    };
                    
                    // Create an individual event grid event
                    var eventGridEvent = new EventGridEvent(
                        subject: $"{topicName}/MessagePublished/{i+1}",
                        eventType: "MessageSent",
                        dataVersion: "1.0",
                        data: new BinaryData(JsonSerializer.Serialize(currentMessage.MessageBody)));
                        
                    // Add metadata to event
                    if (currentMessage.Header.Properties != null)
                    {
                        eventGridEvent.Subject = $"{eventGridEvent.Subject}/{currentMessage.Header.Properties.Priority}";
                        // EventGrid doesn't support custom properties directly in the same way as EventHub
                    }
                    
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