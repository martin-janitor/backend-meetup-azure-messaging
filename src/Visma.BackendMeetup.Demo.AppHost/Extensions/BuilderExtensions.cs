using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;

namespace Visma.BackendMeetup.Demo.AppHost.Extensions
{
    public static class BuilderExtensions
    {
        public static IDistributedApplicationBuilder BuildP2DIFunctionApp(
            this IDistributedApplicationBuilder builder,
            IConfiguration configuration,
            IResourceBuilder<AzureStorageResource> azureStorage,
            IList<IResourceBuilder<ProjectResource>> mockResourceList)
        {
            var environment = builder.Environment.EnvironmentName;

            var serviceBusFuctionApp = builder.AddAzureFunctionsProject<Projects.Visma_BackendMeetup_Demo_ServiceBusConsumer>(
               ServiceBusConsumerFunctionApp)
                .WithHostStorage(azureStorage)
                .WaitFor(azureStorage);

            foreach (var mock in mockResourceList)
            {
                serviceBusFuctionApp.WithReference(mock);
                serviceBusFuctionApp.WaitFor(mock);
            }

            var eventHubFuctionApp = builder.AddAzureFunctionsProject<Projects.Visma_BackendMeetup_Demo_EventHubConsumer>(
               EventHubConsumerFunctionApp)
                .WithHostStorage(azureStorage)
                .WaitFor(azureStorage);



            foreach (var mock in mockResourceList)
            {
                eventHubFuctionApp.WithReference(mock);
                eventHubFuctionApp.WaitFor(mock);
            }


            return builder;
        }

        public static IList<IResourceBuilder<ProjectResource>> BuildMockServices(
            this IDistributedApplicationBuilder builder)
        {
            return new List<IResourceBuilder<ProjectResource>>
            {
                builder.AddProject<Projects.Visma_BackendMeetup_Demo_MessageService>(ServiceBusSenderApiName)
            };
        }

        public static IResourceBuilder<AzureStorageResource> BuildAzureStorage(
            this IDistributedApplicationBuilder builder)
        {
            var azureResourceBuilderList = new List<IResourceBuilder<IResourceWithConnectionString>>();

            var storge = builder.AddAzureStorage(AzureStorage)
                .RunAsEmulator(options =>
                {
                    options.WithBlobPort(BlobPortNumber);
                    options.WithQueuePort(QueuePortNumber);
                    options.WithTablePort(TablePortNumber);
                    options.WithDataVolume(BlobPort);
                })
                .WithEndpoint(
                    port: StorageEndpointPort,
                    targetPort: StorageTargetPort);

            azureResourceBuilderList.Add(storge.AddBlobs(FunctionBlob));
            azureResourceBuilderList.Add(storge.AddQueues(FunctionQueues));
            azureResourceBuilderList.Add(storge.AddTables(FunctionTables));

            return storge;
        }


    }
}
