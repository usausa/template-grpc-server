namespace Template.GrpcServer.TestClient.Commands;

using BunnyTail.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

using Smart.CommandLine.Hosting;

public static partial class CommandExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<GetCommand>();
        commands.AddCommand<SetCommand>();
        commands.AddCommand<DeleteCommand>();
        commands.AddCommand<ListCommand>();
        commands.AddCommand<UploadCommand>();
        commands.AddCommand<ChatCommand>();
        commands.AddCommand<NotifyCommand>();
        commands.AddCommand<UploadFileCommand>();
        commands.AddCommand<DownloadFileCommand>();
    }

    [ComponentRegistration(Lifetime.Transient, "Command$")]
    public static partial IServiceCollection AddCommandComponents(this IServiceCollection services);
}
