﻿namespace OpenShock.SDK.CSharp.Models;

public class ResponseDevice
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime CreatedOn { get; set; }
}