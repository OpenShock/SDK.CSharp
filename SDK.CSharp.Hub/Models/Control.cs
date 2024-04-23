using System.ComponentModel.DataAnnotations;
using OpenShock.SDK.CSharp.Models;

namespace OpenShock.SDK.CSharp.Hub.Models;

public class Control
{
    public required Guid Id { get; set; }
    public required ControlType Type { get; set; }
    [Range(1, 100)]
    public required byte Intensity { get; set; }
    [Range(300, 30000)]
    public required uint Duration { get; set; }
    public bool Exclusive { get; set; } = false;
}