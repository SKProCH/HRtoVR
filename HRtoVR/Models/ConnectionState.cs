namespace HRtoVRChat.Models;

public enum ConnectionState {
    Disconnected,
    Connecting,
    Active
}

public static class ConnectionStateExtensions {
    extension(ConnectionState) {
        public static ConnectionState FromListenerState(bool isConnected, int heartRate) {
            if (!isConnected) return ConnectionState.Disconnected;
            return heartRate > 0 ? ConnectionState.Active : ConnectionState.Connecting;
        }
    }
}