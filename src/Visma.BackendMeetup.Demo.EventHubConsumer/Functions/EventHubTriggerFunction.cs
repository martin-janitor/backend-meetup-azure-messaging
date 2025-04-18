using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Visma.BackendMeetup.Demo.Models;

namespace Visma.BackendMeetup.Demo.EventHubConsumer.Functions;

public class EventHubTriggerFunction
{
    private readonly ILogger<EventHubTriggerFunction> _logger;

    public EventHubTriggerFunction(ILogger<EventHubTriggerFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(EventHubTriggerFunction))]
    public async Task Run(
        [EventHubTrigger(
            "%EventHubName%",
            Connection = "EventHubConnection",
            ConsumerGroup = "%EventHubConsumerGroup%")]
        EventData[] eventData)
    {
        foreach (var message in eventData)
        {
            try
            {
                var messageText = Encoding.UTF8.GetString(message.Body.ToArray());
                _logger.LogInformation($"EventHub message received with Partition: {message.PartitionKey}");

                // Deserialize directly to MessageBody instead of MessageModel
                var messageBody = JsonSerializer.Deserialize<MessageBody>(messageText);
                if (messageBody == null)
                {
                    _logger.LogWarning("Received message with no message body or invalid format");
                    continue;
                }

                // Process the message content
                await ProcessMessageContentAsync(messageBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing EventHub message");
                // Continue processing other messages in the batch
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EventHub message");
                // Continue processing other messages in the batch
            }
        }
    }

    private async Task ProcessMessageContentAsync(MessageBody messageBody)
    {
        // Apply delay if specified in message properties
        if (messageBody.DelaySec > 0)
        {
            _logger.LogInformation($"Delaying processing for {messageBody.DelaySec}sec as specified in message properties");
            await Task.Delay(
                TimeSpan.FromSeconds(messageBody.DelaySec));
        }

        // Simplified processing using only MessageBody
        if (!string.IsNullOrEmpty(messageBody.Content))
        {
            // Process the actual message content
            _logger.LogInformation($"Processing message content: {messageBody.Content}");

            if (!string.IsNullOrEmpty(messageBody.Subject))
            {
                _logger.LogInformation($"Message subject: {messageBody.Subject}");
            }

            if (!string.IsNullOrEmpty(messageBody.Recipient))
            {
                _logger.LogInformation($"Message recipient: {messageBody.Recipient}");
            }
        }
    }
}