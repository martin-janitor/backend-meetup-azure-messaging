using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Visma.BackendMeetup.Demo.MessageService.Configuration;

namespace Visma.BackendMeetup.Demo.MessageService.Handlers;

public class EventHubMessageHandler
{
    private readonly EventHubOptions _options;
    private readonly ILogger<EventHubMessageHandler> _logger;
    private readonly EventHubProducerClient _eventHubClient;

    public EventHubMessageHandler(
        IOptions<EventHubOptions> options,
        ILogger<EventHubMessageHandler> logger,
        EventHubProducerClient eventHubClient)
    {
        _options = options.Value;
        _logger = logger;
        _eventHubClient = eventHubClient;
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
            
            // Validate Event Hub name
            var eventHubName = !string.IsNullOrEmpty(_options.Name) 
                ? _options.Name 
                : throw new InvalidOperationException("Event Hub name is not configured");
            
            // Create a batch for sending multiple messages
            using var eventBatch = await _eventHubClient.CreateBatchAsync();
            
            // Setup cancellation token based on timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            
            // Track successful message count
            int sentMessageCount = 0;

            try 
            {
                // Add messages to batch based on messageCount
                for (int i = 0; i < messageCount; i++)
                {
                    // Check for timeout
                    if (cts.Token.IsCancellationRequested)
                    {
                        _logger.LogWarning("Sending operation timed out after {seconds} seconds. Sent {count}/{total} messages.", 
                            timeoutSeconds, sentMessageCount, messageCount);
                        break;
                    }
                    
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
                    
                    var messageJson = JsonSerializer.Serialize(currentMessage);
                    var eventData = new EventData(Encoding.UTF8.GetBytes(messageJson));
                    
                    // Add metadata as properties
                    if (currentMessage.Header.Properties != null)
                    {
                        eventData.Properties.Add("content-type", currentMessage.Header.Properties.ContentType);
                        eventData.Properties.Add("priority", currentMessage.Header.Properties.Priority);
                        eventData.Properties.Add("timestamp", currentMessage.Header.Properties.Timestamp);
                    }
                    
                    // Check if batch has space for this message
                    if (!eventBatch.TryAdd(eventData))
                    {
                        // Batch is full, send what we have and create a new batch
                        await _eventHubClient.SendAsync(eventBatch, cts.Token);
                        sentMessageCount += i;
                        
                        // Reset batch
                        using var newBatch = await _eventHubClient.CreateBatchAsync(cts.Token);
                        
                        // Try again with the current message in a new batch
                        if (!newBatch.TryAdd(eventData))
                        {
                            throw new Exception($"Message {i+1} is too large for the batch and cannot be sent.");
                        }
                        
                        // Continue with new batch
                        i--;  // Process this index again
                        continue;
                    }
                    
                    sentMessageCount++;
                }
                
                // Send any remaining messages in the batch
                if (eventBatch.Count > 0 && !cts.Token.IsCancellationRequested)
                {
                    await _eventHubClient.SendAsync(eventBatch, cts.Token);
                }
                
                _logger.LogInformation("Successfully sent {count}/{total} messages to Event Hub.", 
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
            _logger.LogError(ex, $"Error sending message to Event Hub: {ex.Message}");
            throw;
        }
    }
}