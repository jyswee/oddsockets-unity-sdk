using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using Newtonsoft.Json;

namespace OddSockets.Unity
{
    /// <summary>
    /// OddSockets Unity SDK
    /// 
    /// Provides a simple interface to the OddSockets real-time messaging platform.
    /// Automatically handles manager discovery and Worker load balancing internally.
    /// </summary>
    public class OddSocketsClient : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private OddSocketsUnityConfig config;
        
        // Events
        public event Action OnConnecting;
        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<Exception> OnError;
        public event Action<WorkerAssignmentInfo> OnWorkerAssigned;
        public event Action<ReconnectInfo> OnReconnecting;
        public event Action OnMaxReconnectAttemptsReached;

        // Private fields
        private SocketIOUnity socket;
        private string workerUrl;
        private string workerId;
        private Dictionary<string, OddSocketsChannel> channels;
        private ConnectionState connectionState = ConnectionState.Disconnected;
        private int reconnectAttempts = 0;
        private int reconnectDelay = 1000; // Start with 1 second
        private string clientIdentifier;
        private SessionInfo sessionInfo;

        /// <summary>
        /// Current connection state
        /// </summary>
        public ConnectionState State => connectionState;

        /// <summary>
        /// Current configuration
        /// </summary>
        public OddSocketsUnityConfig Config => config;

        /// <summary>
        /// Client identifier used for session stickiness
        /// </summary>
        public string ClientIdentifier => clientIdentifier;

        /// <summary>
        /// Current session information
        /// </summary>
        public SessionInfo Session => sessionInfo;

        /// <summary>
        /// Worker information
        /// </summary>
        public WorkerInfo Worker => workerUrl != null && workerId != null 
            ? new WorkerInfo(workerId, workerUrl) 
            : null;

        private void Awake()
        {
            channels = new Dictionary<string, OddSocketsChannel>();
            
            if (config != null)
            {
                Initialize(config);
            }
        }

        private void Start()
        {
            if (config != null && config.AutoConnect)
            {
                ConnectAsync();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        /// <summary>
        /// Initialize the client with configuration
        /// </summary>
        /// <param name="configuration">Configuration to use</param>
        public void Initialize(OddSocketsUnityConfig configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.Validate();
            config = configuration.Clone();
            clientIdentifier = GenerateClientIdentifier();
        }

        /// <summary>
        /// Connect to the OddSockets platform
        /// Handles the Manager → Worker assignment internally
        /// </summary>
        public async Task ConnectAsync()
        {
            if (connectionState == ConnectionState.Connecting || connectionState == ConnectionState.Connected)
            {
                return;
            }

            if (config == null)
            {
                throw new InvalidOperationException("Client not initialized. Call Initialize() first.");
            }

            connectionState = ConnectionState.Connecting;
            OnConnecting?.Invoke();

            try
            {
                // Step 1: Get worker assignment from manager
                await GetWorkerAssignment();

                // Step 2: Connect to assigned worker
                await ConnectToWorker();

                connectionState = ConnectionState.Connected;
                reconnectAttempts = 0;
                reconnectDelay = 1000;
                OnConnected?.Invoke();
            }
            catch (Exception error)
            {
                connectionState = ConnectionState.Disconnected;
                OnError?.Invoke(error);

                // Auto-reconnect with exponential backoff
                if (reconnectAttempts < config.ReconnectAttempts)
                {
                    ScheduleReconnect();
                }
                else
                {
                    OnMaxReconnectAttemptsReached?.Invoke();
                }
            }
        }

        /// <summary>
        /// Disconnect from the platform
        /// </summary>
        public void Disconnect()
        {
            connectionState = ConnectionState.Disconnected;

            if (socket != null)
            {
                socket.Disconnect();
                socket.Dispose();
                socket = null;
            }

            workerUrl = null;
            workerId = null;
            OnDisconnected?.Invoke("Manual disconnect");
        }

        /// <summary>
        /// Get or create a channel
        /// </summary>
        /// <param name="channelName">Name of the channel</param>
        /// <returns>Channel instance</returns>
        public OddSocketsChannel Channel(string channelName)
        {
            if (string.IsNullOrWhiteSpace(channelName))
            {
                throw new ArgumentException("Channel name must be a non-empty string", nameof(channelName));
            }

            if (!channels.ContainsKey(channelName))
            {
                var channel = new OddSocketsChannel(channelName, this);
                channels[channelName] = channel;
            }

            return channels[channelName];
        }

        /// <summary>
        /// Publish multiple messages at once
        /// </summary>
        /// <param name="messages">Array of message objects</param>
        /// <returns>Array of publish results</returns>
        public async Task<PublishResult[]> PublishBulkAsync(BulkMessage[] messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (!IsConnected())
            {
                throw new InvalidOperationException("Not connected to OddSockets");
            }

            var results = new List<PublishResult>();

            foreach (var msg in messages)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(msg.Channel) || msg.Message == null)
                    {
                        results.Add(new PublishResult
                        {
                            Success = false,
                            Error = "Missing channel or message"
                        });
                        continue;
                    }

                    var channel = Channel(msg.Channel);
                    var result = await channel.PublishAsync(msg.Message, msg.Options ?? new PublishOptions());
                    results.Add(new PublishResult
                    {
                        Success = true,
                        Result = result
                    });
                }
                catch (Exception error)
                {
                    results.Add(new PublishResult
                    {
                        Success = false,
                        Error = error.Message
                    });
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Internal: Get worker assignment from manager
        /// </summary>
        private async Task GetWorkerAssignment()
        {
            try
            {
                // Discover the optimal manager URL automatically
                var managerUrl = await ManagerDiscovery.Instance.DiscoverManagerUrlAsync(config.ApiKey);
                var selectWorkerUrl = $"{managerUrl}/api/cluster/select-worker";

                var requestData = new
                {
                    apiKey = config.ApiKey,
                    userId = config.UserId ?? clientIdentifier,
                    clientIdentifier = clientIdentifier
                };

                using (var www = new UnityEngine.Networking.UnityWebRequest(selectWorkerUrl, "GET"))
                {
                    var json = JsonConvert.SerializeObject(requestData);
                    var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                    www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                    www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    www.SetRequestHeader("Content-Type", "application/json");
                    www.SetRequestHeader("User-Agent", "OddSockets-Unity-SDK/1.0.0");
                    www.timeout = config.Timeout;

                    var operation = www.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var responseText = www.downloadHandler.text;
                        var response = JsonConvert.DeserializeObject<WorkerAssignmentResponse>(responseText);

                        if (response?.Url == null)
                        {
                            throw new Exception("Invalid worker assignment response");
                        }

                        workerUrl = response.Url;
                        workerId = response.WorkerId;
                        sessionInfo = response.Session;

                        OnWorkerAssigned?.Invoke(new WorkerAssignmentInfo
                        {
                            WorkerId = workerId,
                            WorkerUrl = workerUrl,
                            Session = sessionInfo,
                            ClientIdentifier = clientIdentifier,
                            ManagerUrl = managerUrl
                        });
                    }
                    else
                    {
                        throw new Exception($"Failed to get worker assignment: {www.error}");
                    }
                }
            }
            catch (Exception error)
            {
                // If manager is offline, try fallback logic
                if (error.Message.Contains("ECONNREFUSED") || error.Message.Contains("ENOTFOUND"))
                {
                    throw new Exception("Manager is offline. Cannot assign worker without session stickiness.");
                }
                throw;
            }
        }

        /// <summary>
        /// Internal: Connect to assigned worker
        /// </summary>
        private async Task ConnectToWorker()
        {
            if (string.IsNullOrEmpty(workerUrl))
            {
                throw new Exception("No worker URL available");
            }

            var uri = new Uri(workerUrl);
            socket = new SocketIOUnity(uri, new SocketIOOptions
            {
                Auth = new Dictionary<string, string>
                {
                    ["apiKey"] = config.ApiKey,
                    ["userId"] = config.UserId ?? clientIdentifier
                },
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Timeout = TimeSpan.FromSeconds(config.Timeout)
            });

            SetupSocketEventHandlers();

            var tcs = new TaskCompletionSource<bool>();

            void OnConnect()
            {
                tcs.TrySetResult(true);
            }

            void OnConnectError(string error)
            {
                tcs.TrySetException(new Exception($"Failed to connect to worker: {error}"));
            }

            socket.OnConnected += OnConnect;
            socket.OnError += OnConnectError;

            await socket.ConnectAsync();

            // Wait for connection or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(config.Timeout + 5));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            socket.OnConnected -= OnConnect;
            socket.OnError -= OnConnectError;

            if (completedTask == timeoutTask)
            {
                throw new Exception("Connection timeout");
            }

            await tcs.Task;
        }

        /// <summary>
        /// Internal: Setup socket event handlers
        /// </summary>
        private void SetupSocketEventHandlers()
        {
            if (socket == null) return;

            // Handle disconnection
            socket.OnDisconnected += (reason) =>
            {
                connectionState = ConnectionState.Disconnected;
                OnDisconnected?.Invoke(reason);

                // Auto-reconnect unless manually disconnected
                if (reason != "io client disconnect")
                {
                    ScheduleReconnect();
                }
            };

            // Handle errors
            socket.OnError += (error) =>
            {
                OnError?.Invoke(new Exception(error));
            };

            // Forward channel-related events to appropriate channels
            socket.On("message", (data) =>
            {
                var messageData = data.GetValue<ChannelMessageData>();
                if (channels.ContainsKey(messageData.Channel))
                {
                    channels[messageData.Channel].HandleMessage(messageData);
                }
            });

            socket.On("subscribed", (data) =>
            {
                var subData = data.GetValue<ChannelSubscriptionData>();
                if (channels.ContainsKey(subData.Channel))
                {
                    channels[subData.Channel].HandleSubscribed(subData);
                }
            });

            socket.On("unsubscribed", (data) =>
            {
                var unsubData = data.GetValue<ChannelSubscriptionData>();
                if (channels.ContainsKey(unsubData.Channel))
                {
                    channels[unsubData.Channel].HandleUnsubscribed(unsubData);
                }
            });

            socket.On("published", (data) =>
            {
                var pubData = data.GetValue<ChannelPublishData>();
                if (channels.ContainsKey(pubData.Channel))
                {
                    channels[pubData.Channel].HandlePublished(pubData);
                }
            });

            socket.On("presence", (data) =>
            {
                var presenceData = data.GetValue<ChannelPresenceData>();
                if (channels.ContainsKey(presenceData.Channel))
                {
                    channels[presenceData.Channel].HandlePresence(presenceData);
                }
            });

            socket.On("presence_change", (data) =>
            {
                var presenceChangeData = data.GetValue<ChannelPresenceChangeData>();
                if (channels.ContainsKey(presenceChangeData.Channel))
                {
                    channels[presenceChangeData.Channel].HandlePresenceChange(presenceChangeData);
                }
            });

            socket.On("history", (data) =>
            {
                var historyData = data.GetValue<ChannelHistoryData>();
                if (channels.ContainsKey(historyData.Channel))
                {
                    channels[historyData.Channel].HandleHistory(historyData);
                }
            });
        }

        /// <summary>
        /// Internal: Schedule reconnection with exponential backoff
        /// </summary>
        private async void ScheduleReconnect()
        {
            if (connectionState == ConnectionState.Connected) return;

            connectionState = ConnectionState.Reconnecting;
            reconnectAttempts++;

            var delay = Mathf.Min(reconnectDelay * Mathf.Pow(2, reconnectAttempts - 1), 30000);

            OnReconnecting?.Invoke(new ReconnectInfo
            {
                Attempt = reconnectAttempts,
                MaxAttempts = config.ReconnectAttempts,
                Delay = (int)delay
            });

            await Task.Delay((int)delay);

            if (connectionState == ConnectionState.Reconnecting)
            {
                await ConnectAsync();
            }
        }

        /// <summary>
        /// Internal: Get socket instance (for Channel class)
        /// </summary>
        internal SocketIOUnity GetSocket()
        {
            return socket;
        }

        /// <summary>
        /// Internal: Check if connected (for Channel class)
        /// </summary>
        internal bool IsConnected()
        {
            return connectionState == ConnectionState.Connected && socket != null && socket.Connected;
        }

        /// <summary>
        /// Internal: Generate consistent client identifier for session stickiness
        /// </summary>
        private string GenerateClientIdentifier()
        {
            // Create a consistent identifier based on API key and user ID
            var baseId = config.UserId ?? "default";
            var apiKeyHash = HashString(config.ApiKey);
            return $"{apiKeyHash}_{baseId}";
        }

        /// <summary>
        /// Internal: Simple hash function for API key
        /// </summary>
        private string HashString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "0";
            
            int hash = 0;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                hash = ((hash << 5) - hash) + c;
                hash = hash & hash; // Convert to 32-bit integer
            }
            return Mathf.Abs(hash).ToString("x");
        }
    }

    // Supporting data structures
    [Serializable]
    public class WorkerAssignmentResponse
    {
        [JsonProperty("url")] public string Url;
        [JsonProperty("workerId")] public string WorkerId;
        [JsonProperty("session")] public SessionInfo Session;
    }

    [Serializable]
    public class SessionInfo
    {
        [JsonProperty("sessionId")] public string SessionId;
        [JsonProperty("clientIdentifier")] public string ClientIdentifier;
        [JsonProperty("createdAt")] public DateTime CreatedAt;
        [JsonProperty("lastActivity")] public DateTime LastActivity;
    }

    [Serializable]
    public class WorkerInfo
    {
        public string WorkerId { get; }
        public string WorkerUrl { get; }

        public WorkerInfo(string workerId, string workerUrl)
        {
            WorkerId = workerId;
            WorkerUrl = workerUrl;
        }
    }

    [Serializable]
    public class WorkerAssignmentInfo
    {
        public string WorkerId;
        public string WorkerUrl;
        public SessionInfo Session;
        public string ClientIdentifier;
        public string ManagerUrl;
    }

    [Serializable]
    public class ReconnectInfo
    {
        public int Attempt;
        public int MaxAttempts;
        public int Delay;
    }

    [Serializable]
    public class BulkMessage
    {
        public string Channel;
        public object Message;
        public PublishOptions Options;
    }

    [Serializable]
    public class PublishResult
    {
        public bool Success;
        public object Result;
        public string Error;
    }

    [Serializable]
    public class PublishOptions
    {
        [JsonProperty("ttl")] public int? Ttl;
        [JsonProperty("metadata")] public Dictionary<string, object> Metadata;
    }

    // Channel event data structures
    [Serializable]
    public class ChannelMessageData
    {
        [JsonProperty("channel")] public string Channel;
        [JsonProperty("message")] public object Message;
        [JsonProperty("timestamp")] public DateTime Timestamp;
        [JsonProperty("sender")] public string Sender;
        [JsonProperty("metadata")] public Dictionary<string, object> Metadata;
    }

    [Serializable]
    public class ChannelSubscriptionData
    {
        [JsonProperty("channel")] public string Channel;
        [JsonProperty("success")] public bool Success;
        [JsonProperty("message")] public string Message;
    }

    [Serializable]
    public class ChannelPublishData
    {
        [JsonProperty("channel")] public string Channel;
        [JsonProperty("success")] public bool Success;
        [JsonProperty("messageId")] public string MessageId;
        [JsonProperty("timestamp")] public DateTime Timestamp;
    }

    [Serializable]
    public class ChannelPresenceData
    {
        [JsonProperty("channel")] public string Channel;
        [JsonProperty("occupants")] public PresenceUser[] Occupants;
        [JsonProperty("count")] public int Count;
    }

    [Serializable]
    public class ChannelPresenceChangeData
    {
        [JsonProperty("channel")] public string Channel;
        [JsonProperty("action")] public string Action; // "join" or "leave"
        [JsonProperty("user")] public PresenceUser User;
    }

    [Serializable]
    public class ChannelHistoryData
    {
        [JsonProperty("channel")] public string Channel;
        [JsonProperty("messages")] public ChannelMessageData[] Messages;
        [JsonProperty("count")] public int Count;
    }

    [Serializable]
    public class PresenceUser
    {
        [JsonProperty("userId")] public string UserId;
        [JsonProperty("state")] public Dictionary<string, object> State;
        [JsonProperty("joinedAt")] public DateTime JoinedAt;
    }
}
