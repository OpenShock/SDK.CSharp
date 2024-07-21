using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using OpenShock.SDK.CSharp.Live.LiveControlModels;
using OpenShock.SDK.CSharp.Live.Utils;
using OpenShock.SDK.CSharp.Serialization;
using OpenShock.SDK.CSharp.Updatables;
using OpenShock.SDK.CSharp.Utils;

namespace OpenShock.SDK.CSharp.Live;

public sealed class OpenShockLiveControlClient : IOpenShockLiveControlClient, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new CustomJsonStringEnumConverter() }
    };

    public string Gateway { get; }
    public Guid DeviceId { get; }
    
    private readonly string _authToken;
    private readonly ILogger<OpenShockLiveControlClient> _logger;
    private readonly ApiClientOptions.ProgramInfo? _programInfo;
    private ClientWebSocket? _clientWebSocket = null;

    public event Func<Task>? OnDeviceNotConnected;
    public event Func<Task>? OnDeviceConnected;
    public event Func<Task>? OnDispose;

    private readonly CancellationTokenSource _dispose;
    private CancellationTokenSource _linked;
    private CancellationTokenSource? _currentConnectionClose = null;

    private Channel<BaseRequest<LiveRequestType>>
        _channel = Channel.CreateUnbounded<BaseRequest<LiveRequestType>>();

    public OpenShockLiveControlClient(string gateway, Guid deviceId, string authToken,
        ILogger<OpenShockLiveControlClient> logger, ApiClientOptions.ProgramInfo? programInfo = null)
    {
        Gateway = gateway;
        DeviceId = deviceId;
        _authToken = authToken;
        _logger = logger;
        _programInfo = programInfo;

        _dispose = new CancellationTokenSource();
        _linked = _dispose;
    }

    public Task InitializeAsync() => ConnectAsync();

    private ValueTask QueueMessage(BaseRequest<LiveRequestType> data) =>
        _channel.Writer.WriteAsync(data, _dispose.Token);

    private readonly AsyncUpdatableVariable<WebsocketConnectionState> _state =
        new(WebsocketConnectionState.Disconnected);

    public IAsyncUpdatable<WebsocketConnectionState> State => _state;

    private async Task MessageLoop()
    {
        try
        {
            await foreach (var msg in _channel.Reader.ReadAllAsync(_linked.Token))
                await JsonWebSocketUtils.SendFullMessage(msg, _clientWebSocket!, _linked.Token, JsonSerializerOptions);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in message loop");
        }
    }

    public struct Shutdown;

    public struct Reconnecting;

    private async Task<OneOf<Success, NotFound, Shutdown, Reconnecting>> ConnectAsync()
    {
        if (_dispose.IsCancellationRequested)
        {
            _logger.LogWarning("Dispose requested, not connecting");
            return new Shutdown();
        }

        _state.Value = WebsocketConnectionState.Connecting;
#if NETSTANDARD2_1
        _currentConnectionClose?.Cancel();
#else
        if (_currentConnectionClose != null) await _currentConnectionClose.CancelAsync();
#endif
        _currentConnectionClose = new CancellationTokenSource();
        _linked = CancellationTokenSource.CreateLinkedTokenSource(_dispose.Token, _currentConnectionClose.Token);

        _clientWebSocket?.Abort();
        _clientWebSocket?.Dispose();

        _channel = Channel.CreateUnbounded<BaseRequest<LiveRequestType>>();
        _clientWebSocket = new ClientWebSocket();

        _clientWebSocket.Options.SetRequestHeader("OpenShockToken", _authToken);
        _clientWebSocket.Options.SetRequestHeader("User-Agent", GetUserAgent());
        _logger.LogInformation("Connecting to websocket....");
        try
        {
            await _clientWebSocket.ConnectAsync(new Uri($"wss://{Gateway}/1/ws/live/{DeviceId}"), _linked.Token);

            _logger.LogInformation("Connected to websocket");
            _state.Value = WebsocketConnectionState.Connected;

            Run(ReceiveLoop, _linked.Token);
            Run(MessageLoop, _linked.Token);

            return new Success();
        }
        catch (WebSocketException e)
        {
            if (e.Message.Contains("404"))
            {
                _logger.LogError("Device not found, shutting down");
                _dispose.Dispose();
                await OnDispose.Raise();
                return new NotFound();
            }

            _logger.LogError(e, "Error while connecting, retrying in 3 seconds");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while connecting, retrying in 3 seconds");
        }

        _state.Value = WebsocketConnectionState.Reconnecting;
        _clientWebSocket.Abort();
        _clientWebSocket.Dispose();
        await Task.Delay(3000, _dispose.Token);
        Run(ConnectAsync, _dispose.Token);
        return new Reconnecting();
    }

    private string GetUserAgent()
    {
        var liveClientAssembly = GetType().Assembly;
        var liveClientVersion = liveClientAssembly.GetName().Version!;

        string programName;
        Version programVersion;
        
        if (_programInfo == null)
        {
            (programName, programVersion) = UserAgentUtils.GetAssemblyInfo();
        }
        else
        {
            programName = _programInfo.Name;
            programVersion = _programInfo.Version;
        }

        var runtimeVersion = RuntimeInformation.FrameworkDescription;
        if (string.IsNullOrEmpty(runtimeVersion)) runtimeVersion = "Unknown Runtime";

        return
            $"OpenShock.SDK.CSharp.Live/{liveClientVersion.Major}.{liveClientVersion.Minor}.{liveClientVersion.Build} " +
            $"({runtimeVersion}; {UserAgentUtils.GetOs()}; " +
            $"{programName} {programVersion}.{programVersion}.{programVersion})";
    }


    private async Task ReceiveLoop()
    {
        while (!_linked.Token.IsCancellationRequested)
        {
            try
            {
                if (_clientWebSocket!.State == WebSocketState.Aborted)
                {
                    _logger.LogWarning("Websocket connection aborted, closing loop");
                    break;
                }

                var message =
                    await JsonWebSocketUtils.ReceiveFullMessageAsyncNonAlloc<BaseResponse<LiveResponseType>>(
                        _clientWebSocket, _linked.Token, JsonSerializerOptions);

                if (message.IsT2)
                {
                    if (_clientWebSocket.State != WebSocketState.Open)
                    {
                        _logger.LogWarning("Client sent closure, but connection state is not open");
                        break;
                    }

                    try
                    {
                        await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close",
                            _linked.Token);
                    }
                    catch (OperationCanceledException e)
                    {
                        _logger.LogError(e, "Error during close handshake");
                    }

                    _logger.LogInformation("Closing websocket connection");
                    break;
                }

                message.Switch(wsRequest => { Run(HandleMessage(wsRequest)); },
                    failed =>
                    {
                        _logger.LogWarning("Deserialization failed for websocket message [{Message}] \n\n {Exception}",
                            failed.Message,
                            failed.Exception);
                    },
                    _ => { });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket connection terminated due to close or shutdown");
                break;
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                    _logger.LogError(e, "Error in receive loop, websocket exception");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while processing websocket request");
            }
        }

#if NETSTANDARD2_1
        _currentConnectionClose?.Cancel();
#else
        await _currentConnectionClose!.CancelAsync();
#endif

        if (_dispose.IsCancellationRequested)
        {
            _logger.LogDebug("Dispose requested, not reconnecting");
            return;
        }

        _logger.LogWarning("Lost websocket connection, trying to reconnect in 3 seconds");
        _state.Value = WebsocketConnectionState.Reconnecting;

        _clientWebSocket?.Abort();
        _clientWebSocket?.Dispose();

        await Task.Delay(3000, _dispose.Token);

        Run(ConnectAsync, _dispose.Token);
    }


    private async Task HandleMessage(BaseResponse<LiveResponseType>? wsRequest)
    {
        if (wsRequest == null) return;
        switch (wsRequest.ResponseType)
        {
            case LiveResponseType.Ping:
                if (wsRequest.Data == null)
                {
                    _logger.LogWarning("Ping response data is null");
                    return;
                }

                var pingResponse = wsRequest.Data.Deserialize<PingResponse>(JsonSerializerOptions);
                if (pingResponse == null)
                {
                    _logger.LogWarning("Ping response data failed to deserialize");
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
                    _logger.LogWarning("Latency announce response data is null");
                    return;
                }

                var latencyAnnounceResponse = wsRequest.Data.Deserialize<LatencyAnnounceData>(JsonSerializerOptions);
                if (latencyAnnounceResponse == null)
                {
                    _logger.LogWarning("Latency announce response data failed to deserialize");
                    return;
                }

                _latency.Value = latencyAnnounceResponse.OwnLatency;
                break;

            case LiveResponseType.DeviceNotConnected:
                await OnDeviceNotConnected.Raise();
                break;
            case LiveResponseType.DeviceConnected:
                await OnDeviceConnected.Raise();
                break;
        }
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
        await OnDispose.Raise();
        _clientWebSocket?.Dispose();
    }

    public Task Run(Func<Task?> function, CancellationToken cancellationToken = default,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1)
        => Task.Run(function, cancellationToken).ContinueWith(
            t =>
            {
                if (!t.IsFaulted) return;
                var index = file.LastIndexOf('\\');
                if (index == -1) index = file.LastIndexOf('/');
                _logger.LogError(t.Exception,
                    "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
                    file.Substring(index + 1, file.Length - index - 1), member, line, t.Exception?.StackTrace);
            }, TaskContinuationOptions.OnlyOnFaulted);

    public Task Run(Task? function, CancellationToken cancellationToken = default, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1)
        => Task.Run(() => function, cancellationToken).ContinueWith(
            t =>
            {
                if (!t.IsFaulted) return;
                var index = file.LastIndexOf('\\');
                if (index == -1) index = file.LastIndexOf('/');
                _logger.LogError(t.Exception,
                    "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
                    file.Substring(index + 1, file.Length - index - 1), member, line, t.Exception?.StackTrace);
            }, TaskContinuationOptions.OnlyOnFaulted);

    private readonly AsyncUpdatableVariable<ulong> _latency = new(0);

    public IAsyncUpdatable<ulong> Latency => _latency;

    public async Task SendFrame(ClientLiveFrame frame)
    {
        await QueueMessage(new BaseRequest<LiveRequestType>()
        {
            RequestType = LiveRequestType.Frame,
            Data = frame
        });
    }
}