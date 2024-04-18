using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp.Hub.Models;

public class ControlLogSender : GenericIni
{
    public required string ConnectionId { get; set; }
    public required string? CustomName { get; set; }
    public required IDictionary<string, object> AdditionalItems { get; set; }
}