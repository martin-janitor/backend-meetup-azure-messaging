using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Visma.BackendMeetup.Demo.MessageService.Configuration;

namespace Visma.BackendMeetup.Demo.MessageService.Handlers;

public class ServiceBusMessageHandler
{
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusMessageHandler> _logger;
    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusMessageHandler(
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusMessageHandler> logger,
        ServiceBusClient serviceBusClient)
    {
        _options = options.Value;
        _logger = logger;
        _serviceBusClient = serviceBusClient;
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
            
            // Validate queue name
            var queueName = !string.IsNullOrEmpty(_options.QueueName) 
                ? _options.QueueName 
                : throw new InvalidOperationException("Service Bus queue name is not configured");
            
            // Setup cancellation token based on timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            
            // Create a sender using the injected client
            await using ServiceBusSender sender = _serviceBusClient.CreateSender(queueName);
            
            // Track successful message count
            int sentMessageCount = 0;

            try 
            {
                // Collection for batch sending
                var messages = new List<ServiceBusMessage>();
                
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
                    
                    var messageJson = JsonSerializer.Serialize(currentMessage);
                    var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageJson));
                    
                    // Add metadata as properties
                    if (currentMessage.Header.Properties != null)
                    {
                        serviceBusMessage.ContentType = currentMessage.Header.Properties.ContentType;
                        serviceBusMessage.ApplicationProperties.Add("priority", currentMessage.Header.Properties.Priority);
                        serviceBusMessage.ApplicationProperties.Add("timestamp", currentMessage.Header.Properties.Timestamp);
                    }
                    
                    // Add message sequence info
                    serviceBusMessage.MessageId = Guid.NewGuid().ToString();
                    serviceBusMessage.Subject = $"{messageModel.MessageBody.Subject ?? "Message"} {i+1} of {messageCount}";
                    
                    messages.Add(serviceBusMessage);
                    
                    // Send in batches of 100 to optimize throughput
                    if (messages.Count >= 100)
                    {
                        await sender.SendMessagesAsync(messages, cts.Token);
                        sentMessageCount += messages.Count;
                        messages.Clear();
                    }
                }
                
                // Send any remaining messages
                if (messages.Count > 0 && !cts.Token.IsCancellationRequested)
                {
                    await sender.SendMessagesAsync(messages, cts.Token);
                    sentMessageCount += messages.Count;
                }
                
                _logger.LogInformation("Successfully sent {count}/{total} messages to Service Bus.", 
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
            _logger.LogError(ex, $"Error sending message to Service Bus: {ex.Message}");
            throw;
        }
    }
}