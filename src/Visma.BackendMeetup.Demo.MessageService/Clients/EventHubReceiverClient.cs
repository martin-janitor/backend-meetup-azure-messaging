using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Options;
using Visma.BackendMeetup.Demo.MessageService.Configuration;

namespace Visma.BackendMeetup.Demo.MessageService.Clients;

public class EventHubReceiverClient : IAsyncDisposable
{
    private readonly EventHubConsumerClient _consumerClient;
    private readonly ILogger<EventHubReceiverClient> _logger;
    private readonly EventHubOptions _options;

    public EventHubReceiverClient(
        IOptions<EventHubOptions> options,
        TokenCredential tokenCredential,
        ILogger<EventHubReceiverClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Validate options
        if (string.IsNullOrEmpty(_options.FullyQualifiedNamespace))
        {
            throw new ArgumentException("Event Hub fully qualified namespace is required", nameof(options));
        }

        if (string.IsNullOrEmpty(_options.Name))
        {
            throw new ArgumentException("Event Hub name is required", nameof(options));
        }

        // Create the consumer client with the configured consumer group and token credential for RBAC
        _consumerClient = new EventHubConsumerClient(
            _options.ConsumerGroup,
            _options.FullyQualifiedNamespace,
            _options.Name,
            tokenCredential);

        _logger.LogInformation("EventHub receiver client initialized with namespace: {Namespace}, hub: {Hub}, consumer group: {ConsumerGroup}, using RBAC",
            _options.FullyQualifiedNamespace, _options.Name, _options.ConsumerGroup);
    }

    /// <summary>
    /// Gets information about the partitions of the Event Hub
    /// </summary>
    public async Task<string[]> GetPartitionIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _consumerClient.GetPartitionIdsAsync(cancellationToken);
    }

    /// <summary>
    /// Reads events from a specific partition starting from a given event position
    /// </summary>
    public async Task<List<EventData>> ReadEventsFromPartitionAsync(
        string partitionId,
        EventPosition eventPosition,
        DateTime endTime,
        int maxEvents,
        ReadEventOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EventData>();
        var reader = _consumerClient.ReadEventsFromPartitionAsync(
            partitionId,
            eventPosition,
            options ?? new ReadEventOptions(),
            cancellationToken);

        try
        {
            int eventCount = 0;
            bool foundValidEvent = false;

            await foreach (var eventData in reader.WithCancellation(cancellationToken))
            {
                // Implement a timeout check to prevent infinite waiting
                if (eventCount++ > 100 && !foundValidEvent)
                {
                    _logger.LogWarning("No valid events found after checking {Count} events in partition {PartitionId}, stopping read",
                        eventCount, partitionId);
                    break;
                }

                // Check if the Data property is not null
                if (eventData.Data != null)
                {
                    foundValidEvent = true;

                    // Check if we've passed the end time
                    if (eventData.Data.EnqueuedTime > endTime)
                        break;

                    results.Add(eventData.Data);

                    // Stop if we've reached the max number of events
                    if (results.Count >= maxEvents)
                        break;
                }
            }

            if (results.Count == 0)
            {
                _logger.LogInformation("No valid events found in partition {PartitionId} for the specified time range", partitionId);
            }
            else
            {
                _logger.LogInformation("Found {Count} valid events in partition {PartitionId}", results.Count, partitionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading events from partition {PartitionId}", partitionId);
        }

        return results;
    }

    /// <summary>
    /// Reads events from all partitions or a specific partition starting from a given date and time
    /// </summary>
    public async IAsyncEnumerable<EventData> ReadEventsFromTimeAsync(
        DateTime startTime,
        DateTime? endTime = null,
        int? maxEvents = null,
        string? partitionId = null,
        ReadEventOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        int eventsRead = 0;
        var actualEndTime = endTime ?? DateTime.UtcNow;
        var actualMaxEvents = maxEvents ?? 100;

        // Get all partition IDs or use the specific one if provided
        string[] partitionIds;
        if (!string.IsNullOrEmpty(partitionId))
        {
            partitionIds = new[] { partitionId };
            _logger.LogInformation("Filtering for specific partition: {PartitionId}", partitionId);
        }
        else
        {
            partitionIds = await GetPartitionIdsAsync(cancellationToken);
            _logger.LogInformation("Reading from all {PartitionCount} partitions", partitionIds.Length);
        }

        if (partitionIds.Length == 0)
        {
            _logger.LogWarning("No partitions found in the Event Hub");
            yield break;
        }

        // Try each partition in sequence
        foreach (var currentPartitionId in partitionIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (eventsRead >= actualMaxEvents)
                break;

            _logger.LogInformation("Reading events from partition {PartitionId} starting at {StartTime}",
                currentPartitionId, startTime);

            var eventPosition = EventPosition.FromEnqueuedTime(startTime);

            // Instead of streaming, get a batch of events from this partition
            var partitionEvents = await ReadEventsFromPartitionAsync(
                currentPartitionId,
                eventPosition,
                actualEndTime,
                actualMaxEvents - eventsRead,
                options,
                cancellationToken);

            // If no events found in this partition, continue to the next one
            if (partitionEvents.Count == 0)
            {
                _logger.LogInformation("No valid events found in partition {PartitionId}, moving to next partition", currentPartitionId);
                continue;
            }

            // Return the events from this partition
            _logger.LogInformation("Returning {EventCount} events from partition {PartitionId}",
                partitionEvents.Count, currentPartitionId);

            foreach (var eventData in partitionEvents)
            {
                yield return eventData;
                eventsRead++;
            }
        }

        if (eventsRead == 0)
        {
            _logger.LogInformation("No events found in the specified partition(s) for the given time range");
        }
        else
        {
            _logger.LogInformation("Total events read across partition(s): {EventCount}", eventsRead);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _consumerClient.DisposeAsync();
    }
}