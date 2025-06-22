namespace OpenShock.SDK.CSharp.Hub.Models;

public enum HubUpdateType
{
    Created, // Whenever a new hub is created
    Updated, // Whenever name or something else directly related to the hub is updated
    ShockerUpdated, // Whenever a shocker is updated, name or limits for a person
    Deleted // Whenever a hub is deleted
    
}