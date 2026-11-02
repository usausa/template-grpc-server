namespace Template.GrpcServer.Host.Handlers;

using Google.Protobuf.WellKnownTypes;
using Google.Rpc;

using Grpc.Core;

// google.rpc.Statusのdetailsにエラーの構造を積む。Web系のProblemDetailsに相当し、独自のエラー型は定義しない
public static class RpcErrors
{
    public static RpcException InvalidArgument(string message, IEnumerable<(string Field, string Description)> violations) =>
        new Google.Rpc.Status
        {
            Code = (int)Code.InvalidArgument,
            Message = message,
            Details =
            {
                Any.Pack(new BadRequest
                {
                    FieldViolations = { violations.Select(static x => new BadRequest.Types.FieldViolation { Field = x.Field, Description = x.Description }) }
                })
            }
        }.ToRpcException();

    public static RpcException AlreadyExists(string message, string reason, string field, string value) =>
        new Google.Rpc.Status
        {
            Code = (int)Code.AlreadyExists,
            Message = message,
            Details = { Any.Pack(new ErrorInfo { Reason = reason, Domain = "data", Metadata = { [field] = value } }) }
        }.ToRpcException();

    public static RpcException Aborted(string message, string reason, string field, string value) =>
        new Google.Rpc.Status
        {
            Code = (int)Code.Aborted,
            Message = message,
            Details = { Any.Pack(new ErrorInfo { Reason = reason, Domain = "data", Metadata = { [field] = value } }) }
        }.ToRpcException();

    public static RpcException NotFound(string message, string resourceType, IEnumerable<string> resourceNames) =>
        new Google.Rpc.Status
        {
            Code = (int)Code.NotFound,
            Message = message,
            Details = { resourceNames.Select(x => Any.Pack(new ResourceInfo { ResourceType = resourceType, ResourceName = x })) }
        }.ToRpcException();
}
