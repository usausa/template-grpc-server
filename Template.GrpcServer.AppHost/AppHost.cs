var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Template_GrpcServer_Host>("grpcserver");

builder.Build().Run();
