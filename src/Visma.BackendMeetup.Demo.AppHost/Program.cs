var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Visma_BackendMeetup_Demo_ApiService>("apiservice");

builder.AddProject<Projects.Visma_BackendMeetup_Demo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
