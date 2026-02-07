using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Websocket.Client;

namespace HRtoVRChat;

public class WebsocketTemplate {
    private WebsocketClient? _client;
    public Action<string>? OnMessage;
    public Action? OnReconnect;

    public string wsUri;

    public WebsocketTemplate(string wsUri) {
        this.wsUri = wsUri;
    }

    public bool IsAlive => _client?.IsRunning ?? false;

    public async Task<bool> Start() {
        try {
            if (_client == null)
            {
                _client = new WebsocketClient(new Uri(wsUri));
                _client.MessageReceived.Subscribe(msg => OnMessage?.Invoke(msg.Text));
                _client.ReconnectionHappened.Subscribe(info => OnReconnect?.Invoke());
            }

            await _client.Start();
            return true;
        }
        catch (Exception e) {
            LogHelper.Error("Failed to connect to WebSocket server! Exception: ", e);
            return false;
        }
    }

    public async Task SendMessage(string message) {
        if (_client != null && _client.IsRunning) {
            _client.Send(message);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> Stop() {
        if (_client != null) {
            if (_client.IsRunning) {
                try {
                    await _client.Stop(WebSocketCloseStatus.NormalClosure, string.Empty);
                    _client.Dispose();
                    _client = null;
                    return true;
                }
                catch (Exception e) {
                    LogHelper.Error("Failed to close connection to WebSocket Server!", e);
                    return false;
                }
            }
            else
                return false;
        }

        return false;
    }
}