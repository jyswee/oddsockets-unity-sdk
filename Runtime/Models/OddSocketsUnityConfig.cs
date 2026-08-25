using System.Threading.Tasks;
using UnityEngine;

namespace OddSockets.Unity
{
    /// <summary>
    /// A freshly minted realtime token returned by an <see cref="OddSocketsTokenProvider"/>.
    /// Mirrors the OddSockets control-plane /v1/token mint response: the game exchanges a
    /// player JWT for this short-lived, scoped token. (FEAT-2026-0824-0040)
    /// </summary>
    public class OddSocketsToken
    {
        /// <summary>The minted realtime token (a signed JWT). Required.</summary>
        public string Token;

        /// <summary>
        /// Optional expiry as ISO-8601 (e.g. "2026-08-24T12:00:00Z"). If both this and
        /// <see cref="Exp"/> are unset, the client reads `exp` out of the token JWT.
        /// </summary>
        public string ExpiresAt;

        /// <summary>Optional expiry as a JWT-style epoch-seconds `exp` claim.</summary>
        public long? Exp;

        /// <summary>
        /// Optional front-door base URL the mint says to connect through. Informational —
        /// the client routes via the configured ManagerUrl (same host), matching the
        /// nodejs reference which does not re-route off the token.
        /// </summary>
        public string BaseUrl;
    }

    /// <summary>
    /// Async callback that returns a fresh minted realtime token. Use INSTEAD of an
    /// API key for game clients that exchange a player JWT for a short-lived scoped
    /// token. The client calls this before every (re)connect and again shortly before
    /// the token expires. (FEAT-2026-0824-0040)
    /// </summary>
    public delegate Task<OddSocketsToken> OddSocketsTokenProvider();

    /// <summary>
    /// Configuration class for OddSockets Unity client.
    /// </summary>
    [System.Serializable]
    public class OddSocketsUnityConfig
    {
        [Header("Required Settings")]
        [Tooltip("Your OddSockets API key. Leave empty when authenticating with a " +
                 "TokenProvider instead (game clients that mint short-lived tokens).")]
        public string ApiKey;

        /// <summary>
        /// Async callback returning a fresh minted realtime token. Set this INSTEAD of
        /// <see cref="ApiKey"/> for game clients that exchange a player JWT for a
        /// short-lived scoped token via the OddSockets /v1/token front door. Not
        /// serialized (cannot be assigned in the Inspector) — wire it in code before
        /// calling Initialize()/ConnectAsync(). (FEAT-2026-0824-0040)
        /// </summary>
        [System.NonSerialized]
        public OddSocketsTokenProvider TokenProvider;

        [Header("Optional Settings")]
        [Tooltip("User identifier (auto-generated if empty)")]
        public string UserId;

        [Tooltip("Refresh a minted token this many milliseconds before it expires.")]
        public int TokenRefreshLeadMs = 120000;

        [Tooltip("Manager URL. Leave empty to use the ODDSOCKETS_MANAGER_URL environment " +
                 "variable, or the public endpoint when that is unset. A URL set here is " +
                 "always used verbatim; the client never falls back to the public endpoint.")]
        public string ManagerUrl;

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
            // Either a static API key OR a token-minting callback must be supplied.
            // Token clients (game front-door auth) carry no API key. (FEAT-2026-0824-0040)
            if (string.IsNullOrWhiteSpace(ApiKey) && TokenProvider == null)
            {
                throw new System.ArgumentException(
                    "Either an ApiKey or a TokenProvider callback is required",
                    nameof(ApiKey));
            }

            // Resolve now so a bad manager URL is rejected at Initialize() instead of
            // surfacing much later as an obscure connection failure.
            ManagerDiscovery.ResolveManagerUrl(ManagerUrl);

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
                TokenProvider = TokenProvider,
                TokenRefreshLeadMs = TokenRefreshLeadMs,
                UserId = UserId,
                ManagerUrl = ManagerUrl,
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
