﻿using System.Net;

namespace OpenShock.SDK.CSharp.Problems;

public sealed class ShockerControlProblem : ProblemDetails
{
    public Guid ShockerId { get; set; }
}