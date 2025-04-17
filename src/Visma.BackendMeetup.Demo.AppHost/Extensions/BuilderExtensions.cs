using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;
using Visma.BackendMeetup.Demo.AppHost.Constants;

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

            // Create multiple Service Bus function app instances using a loop instead of WithReplicas
            var serviceBusFunctionApps = new List<IResourceBuilder<ProjectResource>>();
            
            // Base HTTP port for Service Bus function apps
            int sbBaseHttpPort = 7071;
            
            for (int i = 0; i < ProgramConstants.ServiceBusConsumerFunctionAppReplicas; i++)
            {
                // Create a unique name for each instance
                string instanceName = $"{ProgramConstants.ServiceBusConsumerFunctionApp}-{i}";
                int httpPort = sbBaseHttpPort + i;
                
                // Add a new Azure Functions project for each replica
                var functionApp = builder.AddAzureFunctionsProject<Projects.Visma_BackendMeetup_Demo_ServiceBusConsumer>(instanceName)
                    .WithHostStorage(azureStorage)
                    .WithHttpEndpoint(httpPort, name: $"http-sb-{i}")  // Add unique endpoint name
                    .WaitFor(azureStorage);
                
                // Add references to mock services for each instance
                foreach (var mock in mockResourceList)
                {
                    functionApp.WithReference(mock);
                    functionApp.WaitFor(mock);
                }
                
                serviceBusFunctionApps.Add(functionApp);
            }

            // Create multiple Event Hub function app instances using a loop instead of WithReplicas
            var eventHubFunctionApps = new List<IResourceBuilder<ProjectResource>>();
            
            // Base HTTP port for Event Hub function apps
            int ehBaseHttpPort = 7081;
            
            for (int i = 0; i < ProgramConstants.EventHubConsumerFunctionAppReplicas; i++)
            {
                // Create a unique name for each instance
                string instanceName = $"{ProgramConstants.EventHubConsumerFunctionApp}-{i}";
                int httpPort = ehBaseHttpPort + i;
                
                // Add a new Azure Functions project for each replica
                var functionApp = builder.AddAzureFunctionsProject<Projects.Visma_BackendMeetup_Demo_EventHubConsumer>(instanceName)
                    .WithHostStorage(azureStorage)
                    .WithHttpEndpoint(httpPort, name: $"http-eh-{i}")  // Add unique endpoint name
                    .WaitFor(azureStorage);
                
                // Add references to mock services for each instance
                foreach (var mock in mockResourceList)
                {
                    functionApp.WithReference(mock);
                    functionApp.WaitFor(mock);
                }
                
                eventHubFunctionApps.Add(functionApp);
            }

            return builder;
        }

        public static IList<IResourceBuilder<ProjectResource>> BuildMockServices(
            this IDistributedApplicationBuilder builder)
        {
            return new List<IResourceBuilder<ProjectResource>>
            {
                builder.AddProject<Projects.Visma_BackendMeetup_Demo_MessageService>(ProgramConstants.ServiceBusSenderApiName)
            };
        }

        public static IResourceBuilder<AzureStorageResource> BuildAzureStorage(
            this IDistributedApplicationBuilder builder)
        {
            var azureResourceBuilderList = new List<IResourceBuilder<IResourceWithConnectionString>>();

            var storge = builder.AddAzureStorage(ProgramConstants.AzureStorage)
                .RunAsEmulator(options =>
                {
                    options.WithBlobPort(ProgramConstants.BlobPortNumber);
                    options.WithQueuePort(ProgramConstants.QueuePortNumber);
                    options.WithTablePort(ProgramConstants.TablePortNumber);
                    options.WithDataVolume(ProgramConstants.BlobPort);
                })
                .WithEndpoint(
                    port: ProgramConstants.StorageEndpointPort,
                    targetPort: ProgramConstants.StorageTargetPort);

            azureResourceBuilderList.Add(storge.AddBlobs(ProgramConstants.FunctionBlob));
            azureResourceBuilderList.Add(storge.AddQueues(ProgramConstants.FunctionQueues));
            azureResourceBuilderList.Add(storge.AddTables(ProgramConstants.FunctionTables));

            return storge;
        }
    }
}
