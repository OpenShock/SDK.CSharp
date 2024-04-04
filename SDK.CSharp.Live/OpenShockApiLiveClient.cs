using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.SDK.CSharp.Live.Models;
using OpenShock.SDK.CSharp.Serialization;
using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp.Live;

public class OpenShockApiLiveClient : IOpenShockApiLiveClient, IAsyncDisposable
{
    private readonly ApiLiveClientOptions _apiLiveClientOptions;
    private bool _disposed = false;

    public HubConnection Connection { get; }

    public IDisposable OnLog(Func<ControlLogSender, ICollection<ControlLog>, Task> handler) => Connection.On("Log", handler);
    public IDisposable OnWelcome(Func<string, Task> handler) => Connection.On("Welcome", handler);

    public Task StartAsync() => Connection.StartAsync();

    public OpenShockApiLiveClient(ApiLiveClientOptions apiLiveClientOptions)
    {
        _apiLiveClientOptions = apiLiveClientOptions;

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

        Connection = connectionBuilder.Build();
    }

    public Task Control(IEnumerable<Control> shocks, string? customName = null) =>
        Connection.SendAsync("ControlV2", shocks, customName);


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

        await Connection.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    ~OpenShockApiLiveClient()
    {
        DisposeAsync().AsTask().Wait();
    }
}