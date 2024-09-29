using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.SDK.CSharp.Hub.Models;
using OpenShock.SDK.CSharp.Serialization;
using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp.Hub;

public class OpenShockHubClient : IOpenShockHubClient, IAsyncDisposable
{
    private HubClientOptions? _hubClientOptions;
    private bool _disposed = false;

    private HubConnection? _connection = null;

    public async Task StartAsync()
    {
        if(_connection != null) await _connection.StartAsync();
        await Connected.Raise(_connection?.ConnectionId);
    }

    public Task StopAsync() => _connection == null ? Task.CompletedTask : _connection.StopAsync();
    public event Func<ControlLogSender, ICollection<ControlLog>, Task>? OnLog;
    public event Func<string, Task>? OnWelcome;
    public event Func<Guid, DeviceUpdateType, Task>? OnDeviceUpdate;
    public event Func<IEnumerable<DeviceOnlineState>, Task>? OnDeviceStatus;

    public event Func<Exception?, Task>? Reconnecting;
    public event Func<Exception?, Task>? Closed;
    public event Func<string?, Task>? Reconnected;
    public event Func<string?, Task>? Connected;

    /// <summary>
    /// Creates a new instance of <see cref="OpenShockHubClient"/>
    /// Also calls <see cref="Setup"/> with the provided <see cref="HubClientOptions"/>
    /// </summary>
    /// <param name="hubClientOptions"></param>
    public OpenShockHubClient(HubClientOptions hubClientOptions)
    {
        Setup(hubClientOptions).AsTask().Wait();
    }

    /// <summary>
    /// Blank constructor, use <see cref="Setup"/> to setup the client
    /// </summary>
    public OpenShockHubClient()
    {
    }

    public async ValueTask Setup(HubClientOptions hubClientOptions)
    {
        _hubClientOptions = hubClientOptions;
        if (_connection != null) await _connection.DisposeAsync().ConfigureAwait(false);

        var url = new Uri(hubClientOptions.Server, "/1/hubs/user");
        var connectionBuilder = new HubConnectionBuilder()
            .WithUrl(url, HttpTransportType.WebSockets, options =>
            {
                options.SkipNegotiation = true;
                options.Headers.Add("OpenShockToken", hubClientOptions.Token);
                options.Headers.Add("User-Agent", GetUserAgent());
            })
            .WithAutomaticReconnect(new OpenShockRetryPolicy())
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.Converters.Add(new CustomJsonStringEnumConverter());
            });

        if (hubClientOptions.ConfigureLogging != null)
            connectionBuilder.ConfigureLogging(hubClientOptions.ConfigureLogging);

        _connection = connectionBuilder.Build();
        
        _connection.Closed += Closed.Raise;
        _connection.Reconnecting += Reconnecting.Raise;
        _connection.Reconnected += Reconnected.Raise;
        
        _connection.On<ControlLogSender, ICollection<ControlLog>>("Log", OnLog.Raise);
        _connection.On<string>("Welcome", OnWelcome.Raise);
        _connection.On<Guid, DeviceUpdateType>("DeviceUpdate", OnDeviceUpdate.Raise);
        _connection.On<IEnumerable<DeviceOnlineState>>("DeviceStatus", OnDeviceStatus.Raise);
    }

    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    public Task Control(IEnumerable<Control> shocks, string? customName = null)
    {
        if (_connection != null) return _connection.SendAsync("ControlV2", shocks, customName);
        return Task.CompletedTask;
    }


    private string GetUserAgent()
    {
        var liveClientAssembly = GetType().Assembly;
        var liveClientVersion = liveClientAssembly.GetName().Version!;

        var signalRAssembly = typeof(HubConnection).Assembly;
        var signalRVersion = signalRAssembly.GetName().Version!;

        string programName;
        Version programVersion;
        
        if (_hubClientOptions?.Program == null)
        {
            (programName, programVersion) = UserAgentUtils.GetAssemblyInfo();
        }
        else
        {
            programName = _hubClientOptions.Program.Name;
            programVersion = _hubClientOptions.Program.Version;
        }

        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        if (string.IsNullOrEmpty(runtimeVersion)) runtimeVersion = "Unknown Runtime";

        return
            $"OpenShock.SDK.CSharp.Hub/{liveClientVersion.Major}.{liveClientVersion.Minor}.{liveClientVersion.Build} " +
            $"({runtimeVersion}; {UserAgentUtils.GetOs()}; SignalR {signalRVersion.Major}.{signalRVersion.Minor}.{signalRVersion.Build}; " +
            $"{programName} {programVersion!.Major}.{programVersion.Minor}.{programVersion.Build})";
    }

    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_connection != null) await _connection.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    ~OpenShockHubClient()
    {
        DisposeAsync().AsTask().Wait();
    }
}