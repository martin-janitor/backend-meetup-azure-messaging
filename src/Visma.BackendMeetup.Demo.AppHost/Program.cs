using Visma.BackendMeetup.Demo.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.BuildAppConfiguration();

var azureStorage = builder.BuildAzureStorage();

var mockList = builder.BuildMockServices();


builder.BuildFunctionApps(
    azureStorage: azureStorage,
    configuration: builder.Configuration,
    mockResourceList: mockList);

builder.Build().Run();
