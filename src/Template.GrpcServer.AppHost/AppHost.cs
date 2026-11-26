// ReSharper disable StringLiteralTypo
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDotnetProject("grpcserver", "../Template.GrpcServer.Host/Template.GrpcServer.Host.csproj");

builder.Build().Run();
