using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HRtoVRChat.Listeners.PulsoidSocket;

internal class PulsoidSocketListener : IHrListener {
    private int HR;
    private WebsocketClient? _client;
    private readonly ILogger<PulsoidSocketListener> _logger;
    private readonly PulsoidSocketOptions _options;

    public PulsoidSocketListener(ILogger<PulsoidSocketListener> logger, IOptions<PulsoidSocketOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void Start() {
        var pubUrl = "wss://dev.pulsoid.net/api/v1/data/real_time?access_token=" + _options.Key;

        var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
        {
            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
        });

        _client = new WebsocketClient(new Uri(pubUrl), factory);
        _client.ReconnectTimeout = TimeSpan.FromSeconds(30);

        _client.MessageReceived.Subscribe(msg =>
        {
            var message = msg.Text;
            if (!string.IsNullOrEmpty(message))
            {
                try
                {
                    var jo = JObject.Parse(message);
                    HR = jo["data"]?["heart_rate"]?.Value<int>() ?? 0;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to parse Pulsoid message");
                }
            }
        });

        _client.Start();
        _logger.LogInformation("PulsoidSocketListener started.");
    }

    public void Stop() {
        _client?.Dispose();
        _client = null;
        HR = 0;
        _logger.LogInformation("PulsoidSocketListener stopped.");
    }

    public string Name => "PulsoidSocket";

    public int GetHR() {
        return HR;
    }

    public bool IsOpen() {
        return (_client?.IsRunning ?? false) && HR > 0;
    }

    public bool IsActive() {
        return _client?.IsRunning ?? false;
    }
}
