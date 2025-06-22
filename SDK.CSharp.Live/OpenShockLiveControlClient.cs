using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using LucHeart.WebsocketLibrary;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using OpenShock.MinimalEvents;
using OpenShock.SDK.CSharp.Live.LiveControlModels;
using OpenShock.SDK.CSharp.Live.Utils;
using OpenShock.SDK.CSharp.Models;
using OpenShock.SDK.CSharp.Serialization;
using OpenShock.SDK.CSharp.Updatables;
using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp.Live;

public sealed class OpenShockLiveControlClient : IOpenShockLiveControlClient, IAsyncDisposable
{
    private readonly OpenShockApiClient? _apiClient = null;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new CustomJsonStringEnumConverter() }
    };

    public string? Gateway { get; private set; } = null;
    public Guid HubId { get; }

    private readonly ILogger<OpenShockLiveControlClient>? _logger;

    private readonly JsonWebsocketClient<LiveControlModels.BaseResponse<LiveResponseType>, BaseRequest<LiveRequestType>>
        _jsonWebsocketClient;

    private sealed class ShockerState
    {
        public ControlType LastType { get; set; } = ControlType.Stop;
        [Range(0, 100)] public byte LastIntensity { get; set; } = 0;

        /// <summary>
        /// Active until time for the shocker, determined by client TPS interval + current time
        /// </summary>
        public DateTimeOffset ActiveUntil = DateTimeOffset.MinValue;
    }

    private readonly Timer _managedFrameTimer;

    private readonly ConcurrentDictionary<Guid, ShockerState> _shockerStates = new();

    private byte _tps = 0;

    public byte Tps
    {
        get => _tps;
        private set
        {
            _tps = value;

            if (_tps == 0)
            {
                _managedFrameTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            var interval = TimeSpan.FromMilliseconds(1000d / _tps);
            _managedFrameTimer.Change(interval, interval);
            _logger?.LogDebug("Managed frame timer interval set {Tps} TPS / {Interval}ms interval", _tps,
                interval.Milliseconds);
        }
    }

    public IAsyncUpdatable<WebsocketConnectionState> State =>
        new WebsocketAsyncUpdatableWrap<WebsocketConnectionState>(_jsonWebsocketClient.State);

    public IAsyncMinimalEventObservable OnHubNotConnected => _onHubNotConnected;
    private readonly AsyncMinimalEvent _onHubNotConnected = new();

    public IAsyncMinimalEventObservable OnHubConnected => _onHubConnected;
    private readonly AsyncMinimalEvent _onHubConnected = new();

    public IAsyncMinimalEventObservable OnDispose => _onDispose;
    private readonly AsyncMinimalEvent _onDispose = new();

    private readonly CancellationTokenSource _dispose;

    /// <summary>
    /// Provide a gateway to connect to
    /// </summary>
    /// <param name="gateway">Gateway fqdn</param>
    /// <param name="hubId">Hub GUID</param>
    /// <param name="authToken">Auth Token for the websocket connection</param>
    /// <param name="loggerFactory">Logger factor for logging</param>
    /// <param name="programInfo"></param>
    /// <param name="headers">Extra headers</param>
    public OpenShockLiveControlClient(string gateway, Guid hubId, string authToken,
        ILoggerFactory? loggerFactory = null, ApiClientOptions.ProgramInfo? programInfo = null,
        IEnumerable<KeyValuePair<string, string>>? headers = null) : this(hubId, loggerFactory)
    {
        Gateway = gateway;

        var uri = new Uri($"wss://{Gateway}/1/ws/live/{HubId}");

        var websocketClientOptions = GetWebsocketOptions(authToken, programInfo, headers, loggerFactory?.CreateLogger("JsonWebsocketClient"));
        _jsonWebsocketClient =
            new JsonWebsocketClient<LiveControlModels.BaseResponse<LiveResponseType>, BaseRequest<LiveRequestType>>(uri,
                websocketClientOptions);

        _onMessageSubscription = _jsonWebsocketClient.OnMessage.SubscribeAsync(HandleMessage).AsTask().Result;
    }

    /// <summary>
    /// Just provide the hub ID and a pre-configured api client to get the gateway automatically on every connection attempt.
    /// </summary>
    /// <param name="hubId">Hub GUID</param>
    /// <param name="authToken">Auth Token for the websocket connection</param>
    /// <param name="apiClient">Custom API client, needs to be pre-configured</param>
    /// <param name="loggerFactory">Logger factor for logging</param>
    /// <param name="programInfo"></param>
    /// <param name="headers">Extra headers</param>
    public OpenShockLiveControlClient(Guid hubId,
        string authToken,
        OpenShockApiClient apiClient,
        ILoggerFactory? loggerFactory,
        ApiClientOptions.ProgramInfo? programInfo = null,
        IEnumerable<KeyValuePair<string, string>>? headers = null) : this(hubId, loggerFactory)
    {
        _apiClient = apiClient;

        var websocketClientOptions = GetWebsocketOptions(authToken, programInfo, headers, loggerFactory?.CreateLogger("JsonWebsocketClient"));
        _jsonWebsocketClient =
            new JsonWebsocketClient<LiveControlModels.BaseResponse<LiveResponseType>, BaseRequest<LiveRequestType>>(
                ConnectHook, websocketClientOptions);
        _onMessageSubscription = _jsonWebsocketClient.OnMessage.SubscribeAsync(HandleMessage).AsTask().Result;
    }

    private async Task<OneOf<WebsocketConnectOptions, Error>> ConnectHook()
    {
        var hubGatewayResult = await _apiClient!.GetHubGateway(HubId);
        var gateway = hubGatewayResult.Match<string?>(
            success => success.Value.Gateway,
            notFound =>
            {
                _logger?.LogWarning("Hub [{HubId}] not found while getting Hub Gateway from API", HubId);
                return null;
            },
            offline =>
            {
                _logger?.LogInformation("Hub [{HubId}] is offline", HubId);
                return null;
            },
            unauthenticated =>
            {
                _logger?.LogError("Unauthenticated while getting Hub Gateway from API [{HubId}]", HubId);
                return null;
            });

        Gateway = gateway;

        if (string.IsNullOrEmpty(gateway)) return new Error();

        var uri = new Uri($"wss://{gateway}/1/ws/live/{HubId}");
        return new WebsocketConnectOptions
        {
            Uri = uri
        };
    }

    private OpenShockLiveControlClient(Guid hubId,
        ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<OpenShockLiveControlClient>();
        HubId = hubId;
        _dispose = new CancellationTokenSource();

        _managedFrameTimer = new Timer(FrameTimerTick);
    }

    private WebsocketClientOptions GetWebsocketOptions(string authToken, ApiClientOptions.ProgramInfo? programInfo,
        IEnumerable<KeyValuePair<string, string>>? headers, ILogger? logger)
    {
        var options = new WebsocketClientOptions()
        {
            JsonSerializerOptions = JsonSerializerOptions,
            Headers =
            {
                { "OpenShockToken", authToken },
                { "User-Agent", GetUserAgent(programInfo) }
            },
            Logger = logger
        };
        if (headers is null) return options;
        
        foreach (var (key, value) in headers)
        {
            options.Headers[key] = value;
        }

        return options;
    }

    public bool Start()
    {
        return _jsonWebsocketClient.Start();
    }

    private ValueTask QueueMessage(BaseRequest<LiveRequestType> data) =>
        _jsonWebsocketClient.QueueMessage(data);

    private string GetUserAgent(ApiClientOptions.ProgramInfo? programInfo)
    {
        var liveClientAssembly = GetType().Assembly;
        var liveClientVersion = liveClientAssembly.GetName().Version!;

        string programName;
        Version programVersion;

        if (programInfo == null)
        {
            (programName, programVersion) = UserAgentUtils.GetAssemblyInfo();
        }
        else
        {
            programName = programInfo.Name;
            programVersion = programInfo.Version;
        }

        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        if (string.IsNullOrEmpty(runtimeVersion)) runtimeVersion = "Unknown Runtime";

        return
            $"OpenShock.SDK.CSharp.Live/{liveClientVersion.Major}.{liveClientVersion.Minor}.{liveClientVersion.Build} " +
            $"({runtimeVersion}; {UserAgentUtils.GetOs()}; " +
            $"{programName} {programVersion.Major}.{programVersion.Minor}.{programVersion.Build})";
    }

    private async Task HandleMessage(LiveControlModels.BaseResponse<LiveResponseType>? wsRequest)
    {
        if (wsRequest == null) return;
        switch (wsRequest.ResponseType)
        {
            case LiveResponseType.Ping:
                if (wsRequest.Data == null)
                {
                    _logger?.LogWarning("Ping response data is null");
                    return;
                }

                var pingResponse = wsRequest.Data.Deserialize<PingResponse>(JsonSerializerOptions);
                if (pingResponse == null)
                {
                    _logger?.LogWarning("Ping response data failed to deserialize");
                    return;
                }

                await QueueMessage(new BaseRequest<LiveRequestType>
                {
                    RequestType = LiveRequestType.Pong,
                    Data = pingResponse
                });
                break;
            case LiveResponseType.LatencyAnnounce:
                if (wsRequest.Data == null)
                {
                    _logger?.LogWarning("Latency announce response data is null");
                    return;
                }

                var latencyAnnounceResponse = wsRequest.Data.Deserialize<LatencyAnnounceData>(JsonSerializerOptions);
                if (latencyAnnounceResponse == null)
                {
                    _logger?.LogWarning("Latency announce response data failed to deserialize");
                    return;
                }

                _latency.Value = latencyAnnounceResponse.OwnLatency;
                break;

            case LiveResponseType.DeviceNotConnected:
#pragma warning disable CS4014
                Run(async () => await _onHubNotConnected.InvokeAsyncParallel());
#pragma warning restore CS4014
                break;
            case LiveResponseType.DeviceConnected:
#pragma warning disable CS4014
                Run(async () => await _onHubConnected.InvokeAsyncParallel());
#pragma warning restore CS4014
                break;

            case LiveResponseType.TPS:
                if (wsRequest.Data == null)
                {
                    _logger?.LogWarning("TPS response data is null");
                    return;
                }

                var tpsDataResponse = wsRequest.Data.Deserialize<TpsData>(JsonSerializerOptions);
                if (tpsDataResponse == null)
                {
                    _logger?.LogWarning("TPS response data failed to deserialize");
                    return;
                }

                _logger?.LogDebug("Received TPS: {Tps}", tpsDataResponse.Client);

                Tps = tpsDataResponse.Client;
                break;
        }
    }

    private async void FrameTimerTick(object? state)
    {
        try
        {
            if (_shockerStates.IsEmpty) return;

            if (_jsonWebsocketClient.State.Value != WebsocketConnectionState.Connected)
            {
                _logger?.LogWarning("Frame timer ticked, but websocket is not open");
                _managedFrameTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            IList<ClientLiveFrame>? data = null;

            var cur = DateTimeOffset.UtcNow;

            foreach (var pair in _shockerStates)
            {
                if (pair.Value.ActiveUntil < cur) continue;
                data ??= new List<ClientLiveFrame>();

                data.Add(new ClientLiveFrame
                {
                    Shocker = pair.Key,
                    Type = pair.Value.LastType,
                    Intensity = pair.Value.LastIntensity
                });
            }

            if (data == null) return;

            await QueueMessage(new BaseRequest<LiveRequestType>
            {
                RequestType = LiveRequestType.BulkFrame,
                Data = data
            });
        }
        catch (Exception? e)
        {
            _logger?.LogError(e, "Error in managed frame timer callback");
        }
    }

    /// <inheritdoc />
    public void IntakeFrame(Guid shocker, ControlType type, byte intensity)
    {
        if (_tps == 0)
        {
            _logger?.LogWarning("Intake frame called, but TPS is 0");
            return;
        }

        var activeUntil = DateTimeOffset.UtcNow.AddMilliseconds(1000d / Tps * 2.5);

        _shockerStates.AddOrUpdate(shocker, new ShockerState()
        {
            LastIntensity = intensity,
            ActiveUntil = activeUntil,
            LastType = type
        }, (guid, shockerState) =>
        {
            shockerState.LastIntensity = intensity;
            shockerState.ActiveUntil = activeUntil;
            shockerState.LastType = type;
            return shockerState;
        });
    }


    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

#if NET7_0_OR_GREATER
        await _dispose.CancelAsync();
#else
        _dispose.Cancel();
#endif

        await _onMessageSubscription.DisposeAsync();
        await _jsonWebsocketClient.DisposeAsync();
        await _onDispose.InvokeAsyncParallel();
    }

    private Task Run(Func<Task?> function, CancellationToken cancellationToken = default,
        [CallerFilePath] string file = "",
        [CallerMemberName] string? member = "", [CallerLineNumber] int line = -1)
        => Task.Run(function, cancellationToken).ContinueWith(
            t =>
            {
                if (!t.IsFaulted) return;
                var index = file.LastIndexOf('\\');
                if (index == -1) index = file.LastIndexOf('/');
                _logger?.LogError(t.Exception,
                    "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
                    file.Substring(index + 1, file.Length - index - 1), member, line, t.Exception?.StackTrace);
            }, TaskContinuationOptions.OnlyOnFaulted);

    private Task Run(Task? function, CancellationToken cancellationToken = default, [CallerFilePath] string file = "",
        [CallerMemberName] string? member = "", [CallerLineNumber] int line = -1)
        => Task.Run(() => function, cancellationToken).ContinueWith(
            t =>
            {
                if (!t.IsFaulted) return;
                var index = file.LastIndexOf('\\');
                if (index == -1) index = file.LastIndexOf('/');
                _logger?.LogError(t.Exception,
                    "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
                    file.Substring(index + 1, file.Length - index - 1), member, line, t.Exception?.StackTrace);
            }, TaskContinuationOptions.OnlyOnFaulted);

    private readonly AsyncUpdatableVariable<ulong> _latency = new(0);
    private readonly IAsyncDisposable _onMessageSubscription;

    public IAsyncUpdatable<ulong> Latency => _latency;
}