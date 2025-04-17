using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Visma.BackendMeetup.Demo.MessageService.Configuration;
using Visma.BackendMeetup.Demo.Models;

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
            if (messageModel?.Body == null)
            {
                throw new ArgumentException("Invalid message format. Missing required fields.");
            }

            // Extract values from message model and properties
            int messageCount = messageModel.MessageCount; // Use MessageCount directly
            int timeoutSeconds = 400;
            string? sessionId = messageModel.MessageGroup;

            // Validate topic name
            var topicName = !string.IsNullOrEmpty(_options.TopicName)
                ? _options.TopicName
                : throw new InvalidOperationException("Service Bus topic name is not configured");

            // Setup cancellation token based on timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            // Create a sender using the injected client
            await using ServiceBusSender sender = _serviceBusClient.CreateSender(topicName);

            // Track successful message count
            int sentMessageCount = 0;

            try
            {
                // Collection for batch sending
                var messages = new List<ServiceBusMessage>();

                // Prepare messages based on messageCount
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
                    var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBodyJson));

                    // Set SessionId from MessageGroup if available
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        serviceBusMessage.SessionId = sessionId;
                    }

                    // Add all properties from the Properties collection
                    if (messageModel.Properties != null)
                    {
                        foreach (var prop in messageModel.Properties)
                        {
                            if (!string.IsNullOrEmpty(prop.Key))
                            {
                                serviceBusMessage.ApplicationProperties.Add(prop.Key, prop.Value);
                            }
                        }
                    }

                    // Add message sequence info
                    serviceBusMessage.MessageId = Guid.NewGuid().ToString();
                    serviceBusMessage.Subject = $"{currentMessageBody.Subject ?? "Message"} {i + 1} of {messageCount}";
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