using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Visma.BackendMeetup.Demo.Models;

namespace Visma.BackendMeetup.Demo.ServiceBusConsumer.Functions.Second;

public class ServiceBusTopicTriggerFunction
{
    private readonly ILogger<ServiceBusTopicTriggerFunction> _logger;

    public ServiceBusTopicTriggerFunction(ILogger<ServiceBusTopicTriggerFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessTopicMessage))]
    public async Task ProcessTopicMessage(
        [ServiceBusTrigger(
            "%ServiceBusTopic%",
            "%ServiceBusSubscription%",
            IsSessionsEnabled = true,
            Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message)
    {
        try
        {
            _logger.LogInformation($"Received message from Service Bus Topic with SessionId: {message.SessionId}, MessageId: {message.MessageId}");

            // Extract message body from ServiceBusReceivedMessage
            var messageBodyBytes = message.Body.ToArray();
            var messageBodyString = Encoding.UTF8.GetString(messageBodyBytes);

            _logger.LogInformation($"Message body content: {messageBodyString}");
           

            // Deserialize message body to MessageBody
            var messageBody = JsonSerializer.Deserialize<MessageBody>(messageBodyString);
            if (messageBody == null)
            {
                _logger.LogWarning("Received message with no message body or invalid format");
                return;
            }            

            // Log key details from the message
            _logger.LogInformation($"Message details: Subject: {messageBody.Subject}, " +
                                 $"Recipient: {messageBody.Recipient}");

            // Process the message based on its content
            await ProcessMessageContentAsync(messageBody);

            _logger.LogInformation($"Successfully processed message from Service Bus Topic");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Error deserializing message. Message content: {message.Body}");
            // Don't throw for deserialization errors as retrying wouldn't help
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message from Service Bus Topic: {ex.Message}");
            throw; // Rethrow to let the Service Bus retry policy handle it
        }
    }

    private async Task ProcessMessageContentAsync(MessageBody messageBody)
    {
        // Apply delay if specified in message properties (converted from seconds to milliseconds)
        if (messageBody.DelaySec > 0)
        {
            _logger.LogInformation($"Delaying processing for {messageBody.DelaySec} seconds as specified in message properties");
            await Task.Delay(messageBody.DelaySec * 1000); // Convert seconds to milliseconds
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