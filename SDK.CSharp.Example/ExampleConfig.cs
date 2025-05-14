using System;
using System.Collections.Generic;

namespace SDK.CSharp.Example;

public sealed class ExampleConfig
{
    public Uri ApiUrl { get; set; } = new("https://api.openshock.app");
    public required string ApiToken { get; set; }
    public Guid? Hub { get; set; }
    public IReadOnlyCollection<Guid> Shockers { get; set; } = Array.Empty<Guid>();
}