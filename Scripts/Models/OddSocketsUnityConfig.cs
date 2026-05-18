using UnityEngine;

namespace OddSockets.Unity
{
    /// <summary>
    /// Configuration class for OddSockets Unity client.
    /// </summary>
    [System.Serializable]
    public class OddSocketsUnityConfig
    {
        [Header("Required Settings")]
        [Tooltip("Your OddSockets API key")]
        public string ApiKey;

        [Header("Optional Settings")]
        [Tooltip("User identifier (auto-generated if empty)")]
        public string UserId;

        [Tooltip("Automatically connect on initialization")]
        public bool AutoConnect = true;

        [Header("Connection Settings")]
        [Tooltip("Maximum reconnection attempts")]
        [Range(0, 10)]
        public int ReconnectAttempts = 5;

        [Tooltip("Connection timeout in seconds")]
        [Range(5, 60)]
        public int Timeout = 10;

        [Tooltip("Heartbeat interval in seconds")]
        [Range(10, 300)]
        public int HeartbeatInterval = 30;

        [Header("Logging")]
        [Tooltip("Logging level for SDK operations")]
        public LogLevel LogLevel = LogLevel.Info;

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                throw new System.ArgumentException("API key is required", nameof(ApiKey));
            }

            if (Timeout <= 0)
            {
                throw new System.ArgumentException("Timeout must be greater than 0", nameof(Timeout));
            }

            if (ReconnectAttempts < 0)
            {
                throw new System.ArgumentException("ReconnectAttempts cannot be negative", nameof(ReconnectAttempts));
            }

            if (HeartbeatInterval < 0)
            {
                throw new System.ArgumentException("HeartbeatInterval cannot be negative", nameof(HeartbeatInterval));
            }
        }

        /// <summary>
        /// Creates a copy of this configuration.
        /// </summary>
        /// <returns>A new configuration instance with the same values.</returns>
        public OddSocketsUnityConfig Clone()
        {
            return new OddSocketsUnityConfig
            {
                ApiKey = ApiKey,
                UserId = UserId,
                AutoConnect = AutoConnect,
                ReconnectAttempts = ReconnectAttempts,
                Timeout = Timeout,
                HeartbeatInterval = HeartbeatInterval,
                LogLevel = LogLevel
            };
        }
    }

    /// <summary>
    /// Logging levels for the SDK.
    /// </summary>
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    /// <summary>
    /// Connection states for the client.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed
    }
}
