namespace Visma.BackendMeetup.Demo.AppHost.Constants
{
    public static class ProgramConstants
    {
        public const string VipsServiceBusOptionsConfigSection = "VipsServiceBusOptions";

        public const string CosmosVolumeName = "cosmosdb";
        public const string AzureStorage = "azurite-meetup";
        public const string BlobPort = "volume-meetup";
        public const string FunctionBlob = "function-blob";
        public const string FunctionQueues = "function-queues";
        public const string FunctionTables = "function-tables";

        public const string ServiceBusSenderApiName = "message-sender-api";


        public const string ServiceBusConsumerFunctionApp = "service-bus-consumer-app";
        public const int ServiceBusConsumerFunctionAppReplicas = 2;

        public const string EventHubConsumerFunctionApp = "event-hub-consumer-app";
        public const int EventHubConsumerFunctionAppReplicas = 1;


        // Port constants
        public const int CosmosDbPort = 10020;
        public const int QueuePortNumber = 10002;
        public const int BlobPortNumber = 10001;
        public const int TablePortNumber = 10003;
        public const int StorageEndpointPort = 10000;
        public const int StorageTargetPort = 10000;
    }
}
