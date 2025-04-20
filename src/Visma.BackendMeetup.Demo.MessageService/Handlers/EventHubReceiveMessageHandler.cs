using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs.Consumer;
using Visma.BackendMeetup.Demo.MessageService.Clients;
using Visma.BackendMeetup.Demo.Models;

namespace Visma.BackendMeetup.Demo.MessageService.Handlers;

public class EventHubReceiveMessageHandler
{
    private readonly EventHubReceiverClient _eventHubReceiver;
    private readonly ILogger<EventHubReceiveMessageHandler> _logger;

    public EventHubReceiveMessageHandler(
        EventHubReceiverClient eventHubReceiver,
        ILogger<EventHubReceiveMessageHandler> logger)
    {
        _eventHubReceiver = eventHubReceiver;
        _logger = logger;
    }

    public async Task<IResult> GetMessagesByEnqueueTimeAsync(
        DateTime startTime,
        DateTime? endTime,
        int? maxMessages,
        string? partitionId = null,
        CancellationToken cancellationToken = default)
    {
        if (startTime > DateTime.UtcNow)
        {
            return Results.BadRequest("Start time cannot be in the future");
        }

        var actualEndTime = endTime ?? DateTime.UtcNow;
        var actualMaxMessages = maxMessages ?? 100;

        _logger.LogInformation("Reading events from {StartTime} to {EndTime}, max count: {MaxCount}{PartitionFilter}",
            startTime, actualEndTime, actualMaxMessages,
            partitionId == null ? "" : $", partition filter: {partitionId}");

        var envelopes = new List<EventHubMessageEnvelope>();
        bool hasPartitions = false;

        try
        {
            var partitionIds = await _eventHubReceiver.GetPartitionIdsAsync(cancellationToken);
            hasPartitions = partitionIds.Length > 0;

            // Validate the partitionId if specified
            if (!string.IsNullOrEmpty(partitionId) && !partitionIds.Contains(partitionId))
            {
                return Results.BadRequest($"Invalid partition ID: {partitionId}. Available partitions: {string.Join(", ", partitionIds)}");
            }

            var options = new ReadEventOptions
            {
                MaximumWaitTime = TimeSpan.FromSeconds(5)
            };

            var eventsStream = _eventHubReceiver.ReadEventsFromTimeAsync(
                startTime,
                actualEndTime,
                actualMaxMessages,
                partitionId,
                options,
                cancellationToken);

            await foreach (var eventData in eventsStream)
            {
                var messageText = Encoding.UTF8.GetString(eventData.EventBody.ToArray());
                try
                {
                    var messageBody = JsonSerializer.Deserialize<MessageBody>(messageText);
                    if (messageBody != null)
                    {
                        // Create a new message envelope with all metadata
                        var envelope = new EventHubMessageEnvelope
                        {
                            PartitionKey = eventData.PartitionKey,
                            SequenceNumber = eventData.SequenceNumber,
                            EnqueuedTime = eventData.EnqueuedTime,
                            Data = messageBody
                        };
                        
                        // Add the envelope to the list
                        envelopes.Add(envelope);
                        _logger.LogDebug("Added message with sequence {SequenceNumber} from partition {PartitionId}", 
                            eventData.SequenceNumber, eventData.PartitionKey);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, $"Could not deserialize message: {messageText}");
                }
            }

            if (envelopes.Count == 0)
            {
                string message = hasPartitions
                    ? $"No messages found in the specified {(partitionId == null ? "partitions" : $"partition {partitionId}")} for the given time range"
                    : "No partitions found in the Event Hub";

                _logger.LogInformation(message);
                return Results.Ok(new
                {
                    message = message,
                    startTime,
                    endTime = actualEndTime,
                    partitionId,
                    messages = Array.Empty<EventHubMessageEnvelope>()
                });
            }

            _logger.LogInformation($"Retrieved {envelopes.Count} messages based on enqueue time criteria");
            return Results.Ok(envelopes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages by enqueue time");
            return Results.Problem("Error retrieving messages: " + ex.Message);
        }
    }
}