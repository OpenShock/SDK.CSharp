using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.SDK.CSharp.Live.Models;
using OpenShock.SDK.CSharp.Live.Utils;
using OpenShock.SDK.CSharp.Serialization;
using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp.Live;

public class OpenShockApiLiveClient : IOpenShockApiLiveClient, IAsyncDisposable
{
    private bool _disposed = false;

    private HubConnection? _connection = null;

    public Task StartAsync() => _connection == null ? Task.CompletedTask : _connection.StartAsync();
    public event Func<ControlLogSender, ICollection<ControlLog>, Task>? OnLog;
    public event Func<string, Task>? OnWelcome;
    public event Func<Guid, DeviceUpdateType, Task>? OnDeviceUpdate;
    public event Func<IEnumerable<DeviceOnlineState>, Task>? OnDeviceStatus;

    public event Func<Exception?, Task>? Reconnecting;
    public event Func<Exception?, Task>? Closed;
    public event Func<string?, Task>? Reconnected;

    /// <summary>
    /// Creates a new instance of <see cref="OpenShockApiLiveClient"/>
    /// Also calls <see cref="Setup"/> with the provided <see cref="ApiLiveClientOptions"/>
    /// </summary>
    /// <param name="apiLiveClientOptions"></param>
    public OpenShockApiLiveClient(ApiLiveClientOptions apiLiveClientOptions)
    {
        Setup(apiLiveClientOptions).AsTask().Wait();
    }

    /// <summary>
    /// Blank constructor, use <see cref="Setup"/> to setup the client
    /// </summary>
    public OpenShockApiLiveClient()
    {
    }

    public async ValueTask Setup(ApiLiveClientOptions apiLiveClientOptions)
    {
        if (_connection != null) await _connection.DisposeAsync().ConfigureAwait(false);

        var url = new Uri(apiLiveClientOptions.Server, "/1/hubs/user");
        var connectionBuilder = new HubConnectionBuilder()
            .WithUrl(url, HttpTransportType.WebSockets, options =>
            {
                options.Headers.Add("OpenShockToken", apiLiveClientOptions.Token);
                options.Headers.Add("User-Agent", GetUserAgent());
            })
            .WithAutomaticReconnect(new OpenShockRetryPolicy())
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.Converters.Add(new CustomJsonStringEnumConverter());
            });

        if (apiLiveClientOptions.ConfigureLogging != null)
            connectionBuilder.ConfigureLogging(apiLiveClientOptions.ConfigureLogging);

        _connection = connectionBuilder.Build();


        _connection.Closed += Closed.Raise;
        _connection.Reconnecting += Reconnecting.Raise;
        _connection.Reconnected += Reconnected.Raise;

        _connection.On<ControlLogSender, ICollection<ControlLog>>("Log", OnLog.Raise);
        _connection.On<string>("Welcome", OnWelcome.Raise);
        _connection.On<Guid, DeviceUpdateType>("DeviceUpdate", OnDeviceUpdate.Raise);
        _connection.On<IEnumerable<DeviceOnlineState>>("DeviceStatus", OnDeviceStatus.Raise);
    }

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

        var entryAssembly = Assembly.GetEntryAssembly();
        var entryAssemblyName = entryAssembly!.GetName();
        var entryAssemblyVersion = entryAssemblyName.Version;

        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        if (string.IsNullOrEmpty(runtimeVersion)) runtimeVersion = "Unknown Runtime";

        return
            $"OpenShock.SDK.CSharp.Live/{liveClientVersion.Major}.{liveClientVersion.Minor}.{liveClientVersion.Build} " +
            $"({runtimeVersion}; {UserAgentUtils.GetOs()}; SignalR {signalRVersion.Major}.{signalRVersion.Minor}.{signalRVersion.Build}; " +
            $"{entryAssemblyName.Name} {entryAssemblyVersion!.Major}.{entryAssemblyVersion.Minor}.{entryAssemblyVersion.Build})";
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_connection != null) await _connection.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    ~OpenShockApiLiveClient()
    {
        DisposeAsync().AsTask().Wait();
    }
}