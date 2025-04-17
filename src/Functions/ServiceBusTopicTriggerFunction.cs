using System;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Visma.BackendMeetup.Demo.FunctionApp.Models;

namespace Visma.BackendMeetup.Demo.FunctionApp.Functions;

public class ServiceBusTopicTriggerFunction
{
    private readonly ILogger<ServiceBusTopicTriggerFunction> _logger;

    public ServiceBusTopicTriggerFunction(ILogger<ServiceBusTopicTriggerFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessTopicMessage))]
    public void ProcessTopicMessage(
        [ServiceBusTrigger("%ServiceBusTopic%", "%ServiceBusSubscription%", Connection = "ServiceBusConnection")] string message)
    {
        try
        {
            _logger.LogInformation($"Received message from Service Bus Topic");
            
            // Deserialize the message using our MessageModel class
            var messageModel = JsonSerializer.Deserialize<MessageModel>(message);
            if (messageModel?.MessageBody == null)
            {
                _logger.LogWarning("Received message with no message body or invalid format");
                return;
            }
            
            // Log key details from the message
            _logger.LogInformation($"Message details: Subject: {messageModel.MessageBody.Subject}, " +
                                 $"Recipient: {messageModel.MessageBody.Recipient}");
            
            // Process the message based on its content
            // This is where you'd implement your specific business logic
            ProcessMessageContent(messageModel);
            
            _logger.LogInformation($"Successfully processed message from Service Bus Topic");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Error deserializing message: {ex.Message}. Message content: {message}");
            // Don't throw for deserialization errors as retrying wouldn't help
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message from Service Bus Topic: {ex.Message}");
            throw; // Rethrow to let the Service Bus retry policy handle it
        }
    }
    
    private void ProcessMessageContent(MessageModel message)
    {
        // You can implement specific business logic here based on the message content
        // For example:
        
        // Check message priority
        string priority = message.Header?.Properties?.Priority ?? "normal";
        switch (priority.ToLower())
        {
            case "high":
                _logger.LogInformation("Processing high priority message");
                // Add special handling for high priority messages
                break;
                
            case "normal":
                _logger.LogInformation("Processing normal priority message");
                break;
                
            case "low":
                _logger.LogInformation("Processing low priority message");
                break;
        }
        
        // You could also process messages differently based on content, subject, or recipient
        if (!string.IsNullOrEmpty(message.MessageBody?.Content))
        {
            // Process the actual message content
            _logger.LogInformation($"Processing message content: {message.MessageBody.Content}");
        }
    }
}