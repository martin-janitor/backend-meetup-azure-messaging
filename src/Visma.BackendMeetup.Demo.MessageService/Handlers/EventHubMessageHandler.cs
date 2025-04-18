using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Options;
using Visma.BackendMeetup.Demo.MessageService.Configuration;
using Visma.BackendMeetup.Demo.Models;

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
            if (messageModel?.Body == null)
            {
                throw new ArgumentException("Invalid message format. Missing required fields.");
            }

            // Extract values from message model and properties
            int messageCount = messageModel.MessageCount; // Use MessageCount directly
            int timeoutSeconds = 30;
            string? partitionKey = messageModel.MessageGroup;

            // Get timeout value from Properties collection if available
            if (messageModel.Properties != null && messageModel.Properties.Any())
            {
                var timeoutProperty = messageModel.Properties.FirstOrDefault(p => p.Key == "Timeout");

                if (timeoutProperty != null && int.TryParse(timeoutProperty.Value, out int timeout))
                {
                    timeoutSeconds = timeout;
                }
            }

            // Validate Event Hub name
            var eventHubName = !string.IsNullOrEmpty(_options.Name)
                ? _options.Name
                : throw new InvalidOperationException("Event Hub name is not configured");

            // Create a batch for sending multiple messages
            CreateBatchOptions batchOptions = new CreateBatchOptions();
            if (!string.IsNullOrEmpty(partitionKey))
            {
                batchOptions.PartitionKey = partitionKey;

                //batchOptions.PartitionId = partitionKey.EndsWith("1")
                //    ? "1"
                //    : "0";
            }

            using var eventBatch = await _eventHubClient.CreateBatchAsync(batchOptions);

            // Setup cancellation token based on timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            // Track successful message count
            int sentMessageCount = 0;

            try
            {
                // Add messages to batch based on messageCount
                for (int i = 0; i < messageCount && !cts.Token.IsCancellationRequested; i++)
                {
                    // Create a message body with incrementing index in the content
                    var currentMessageBody = new MessageBody
                    {
                        Recipient = messageModel.Body.Recipient,
                        Subject = messageModel.Body.Subject,
                        Content = $"{messageModel.Body.Content} (Message {i + 1} of {messageCount})",
                        DelaySec = messageModel.Body.DelaySec
                    };

                    // Serialize only the MessageBody part
                    var messageBodyJson = JsonSerializer.Serialize(currentMessageBody);
                    var eventData = new EventData(Encoding.UTF8.GetBytes(messageBodyJson));

                    // Add all properties from the Properties collection
                    if (messageModel.Properties != null)
                    {
                        foreach (var prop in messageModel.Properties)
                        {
                            if (!string.IsNullOrEmpty(prop.Key))
                            {
                                eventData.Properties.Add(prop.Key, prop.Value);
                            }
                        }
                    }

                    // Check if batch has space for this message
                    if (!eventBatch.TryAdd(eventData))
                    {
                        // Batch is full, send what we have and create a new batch
                        await _eventHubClient.SendAsync(eventBatch, cts.Token);
                        sentMessageCount += eventBatch.Count;

                        // Reset batch
                        using var newBatch = await _eventHubClient.CreateBatchAsync(batchOptions, cts.Token);

                        // Try again with the current message in a new batch
                        if (!newBatch.TryAdd(eventData))
                        {
                            throw new Exception($"Message {i + 1} is too large for the batch and cannot be sent.");
                        }

                        // Continue with the new batch
                        i--;  // Process this index again
                        continue;
                    }
                }

                // Send any remaining messages in the batch
                if (eventBatch.Count > 0 && !cts.Token.IsCancellationRequested)
                {
                    await _eventHubClient.SendAsync(eventBatch, cts.Token);
                    sentMessageCount += eventBatch.Count;
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