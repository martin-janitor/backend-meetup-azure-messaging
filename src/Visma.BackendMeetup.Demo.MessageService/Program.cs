using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Visma.BackendMeetup.Demo.MessageService.Clients;
using Visma.BackendMeetup.Demo.MessageService.Configuration;
using Visma.BackendMeetup.Demo.MessageService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register configuration options
builder.Services.Configure<EventHubOptions>(
    builder.Configuration.GetSection(EventHubOptions.EventHub));
builder.Services.Configure<EventGridOptions>(
    builder.Configuration.GetSection(EventGridOptions.EventGrid));
builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(ServiceBusOptions.ServiceBus));
builder.Services.Configure<EventGridEventHubOptions>(
    builder.Configuration.GetSection(EventGridEventHubOptions.EventGridEventHub));

// Register Azure default credential for dependency injection
builder.Services.AddSingleton<TokenCredential>(sp => new DefaultAzureCredential());

// Add Azure clients with RBAC authentication
builder.Services.AddAzureClients(clientBuilder =>
{
    // Get configuration options
    var serviceBusOptions = builder.Configuration.GetSection(ServiceBusOptions.ServiceBus).Get<ServiceBusOptions>();
    var eventHubOptions = builder.Configuration.GetSection(EventHubOptions.EventHub).Get<EventHubOptions>();
    var eventGridOptions = builder.Configuration.GetSection(EventGridOptions.EventGrid).Get<EventGridOptions>();
    var eventGridEventHubOptions = builder.Configuration.GetSection(EventGridEventHubOptions.EventGridEventHub).Get<EventGridEventHubOptions>();

    if (serviceBusOptions?.FullyQualifiedNamespace != null)
    {
        // Add Service Bus client with RBAC authentication
        clientBuilder.AddServiceBusClientWithNamespace(serviceBusOptions.FullyQualifiedNamespace);
    }

    if (eventHubOptions?.FullyQualifiedNamespace != null)
    {
        // Add Event Hub producer client with RBAC authentication
        clientBuilder.AddEventHubProducerClientWithNamespace(eventHubOptions.FullyQualifiedNamespace, eventHubOptions.Name);
    }

    if (eventGridOptions?.Endpoint != null)
    {
        // Add Event Grid client with RBAC authentication
        clientBuilder.AddEventGridPublisherClient(new Uri(eventGridOptions.Endpoint));
    }

    // Configure default credential for all clients
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

// Register service handlers
builder.Services.AddSingleton<EventHubMessageHandler>();
builder.Services.AddSingleton<EventGridMessageHandler>();
builder.Services.AddSingleton<ServiceBusMessageHandler>();
builder.Services.AddSingleton<EventHubReceiverClient>();
builder.Services.AddSingleton<EventHubReceiveMessageHandler>();
builder.Services.AddSingleton<EventGridEventHubReceiverClient>();
builder.Services.AddSingleton<EventGridEventHubReceiveMessageHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Hello Backend Meetup!")
    .WithName("GetHelloWorld")
    .Produces<string>(StatusCodes.Status200OK);

// Add endpoint for retrieving messages by enqueue time
app.MapGet("/messages/by-enqueue-time", async (
    DateTime startTime,
    DateTime? endTime,
    int? maxMessages,
    string? partitionId,
    EventHubReceiveMessageHandler handler,
    CancellationToken cancellationToken) =>
    await handler.GetMessagesByEnqueueTimeAsync(startTime, endTime, maxMessages, partitionId, cancellationToken));

// Add endpoint for retrieving messages from EventGrid Event Hub
app.MapGet("/messages/from-eventgrid", async (
    DateTime? startTime,
    DateTime? endTime,
    int? maxMessages,
    string? partitionId,
    EventGridEventHubReceiveMessageHandler handler,
    CancellationToken cancellationToken) =>
    await handler.GetMessagesFromEventGridAsync(startTime, endTime, maxMessages, partitionId, cancellationToken))
    .WithName("GetMessagesFromEventGrid")
    .Produces<List<Visma.BackendMeetup.Demo.Models.EventGrid.EventGridEventHubEnvelope>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError);

// Updated to handle JSON message with batch processing
app.MapPost("/publish/eventhub", async (HttpRequest request, EventHubMessageHandler handler) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var messageJson = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(messageJson))
        {
            return Results.BadRequest("Message body cannot be empty");
        }

        await handler.SendMessageAsync(messageJson);
        return Results.Ok("Messages sent to Event Hub successfully");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error sending messages to Event Hub: {ex.Message}");
    }
});


// Updated to handle JSON message with batch processing
app.MapPost("/publish/eventgrid", async (HttpRequest request, EventGridMessageHandler handler) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var messageJson = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(messageJson))
        {
            return Results.BadRequest("Message body cannot be empty");
        }

        await handler.SendMessageAsync(messageJson);
        return Results.Ok("Messages sent to Event Grid successfully");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error sending messages to Event Grid: {ex.Message}");
    }
});

// Updated to handle JSON message with batch processing
app.MapPost("/publish/servicebus", async (HttpRequest request, ServiceBusMessageHandler handler) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var messageJson = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(messageJson))
        {
            return Results.BadRequest("Message body cannot be empty");
        }

        await handler.SendMessageAsync(messageJson);
        return Results.Ok("Messages sent to Service Bus successfully");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error sending messages to Service Bus: {ex.Message}");
    }
});

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
