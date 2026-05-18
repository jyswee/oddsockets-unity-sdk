using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OddSockets.Unity;

namespace OddSockets.Unity.Examples
{
    /// <summary>
    /// Basic usage example for the OddSockets Unity SDK.
    /// 
    /// This example demonstrates:
    /// - Client initialization and connection
    /// - Channel subscription and messaging
    /// - Presence tracking
    /// - Error handling
    /// - Proper cleanup
    /// </summary>
    public class BasicUsage : MonoBehaviour
    {
        [Header("OddSockets Configuration")]
        [SerializeField] private OddSocketsUnityConfig config;
        
        [Header("Example Settings")]
        [SerializeField] private string channelName = "unity-example";
        [SerializeField] private bool enablePresence = true;
        [SerializeField] private bool retainHistory = true;
        
        [Header("UI References")]
        [SerializeField] private UnityEngine.UI.Button connectButton;
        [SerializeField] private UnityEngine.UI.Button disconnectButton;
        [SerializeField] private UnityEngine.UI.Button sendMessageButton;
        [SerializeField] private UnityEngine.UI.InputField messageInput;
        [SerializeField] private UnityEngine.UI.Text statusText;
        [SerializeField] private UnityEngine.UI.Text messagesText;
        
        // Private fields
        private OddSocketsClient client;
        private OddSocketsChannel channel;
        private List<string> messageHistory = new List<string>();

        private void Start()
        {
            InitializeClient();
            SetupUI();
        }

        private void OnDestroy()
        {
            CleanupClient();
        }

        /// <summary>
        /// Initialize the OddSockets client
        /// </summary>
        private void InitializeClient()
        {
            // Create client component
            client = gameObject.AddComponent<OddSocketsClient>();
            
            // Initialize with configuration
            if (config != null)
            {
                client.Initialize(config);
            }
            else
            {
                // Create default configuration
                var defaultConfig = new OddSocketsUnityConfig
                {
                    ApiKey = "your-api-key-here",
                    UserId = "unity-user-" + UnityEngine.Random.Range(1000, 9999),
                    AutoConnect = false,
                    ReconnectAttempts = 5,
                    Timeout = 10,
                    HeartbeatInterval = 30,
                    LogLevel = LogLevel.Info
                };
                
                client.Initialize(defaultConfig);
            }

            // Setup event handlers
            SetupClientEvents();
        }

        /// <summary>
        /// Setup client event handlers
        /// </summary>
        private void SetupClientEvents()
        {
            client.OnConnecting += OnConnecting;
            client.OnConnected += OnConnected;
            client.OnDisconnected += OnDisconnected;
            client.OnError += OnError;
            client.OnWorkerAssigned += OnWorkerAssigned;
            client.OnReconnecting += OnReconnecting;
            client.OnMaxReconnectAttemptsReached += OnMaxReconnectAttemptsReached;
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupUI()
        {
            if (connectButton != null)
                connectButton.onClick.AddListener(() => ConnectAsync());
                
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(Disconnect);
                
            if (sendMessageButton != null)
                sendMessageButton.onClick.AddListener(() => SendMessageAsync());

            UpdateUI();
        }

        /// <summary>
        /// Connect to OddSockets
        /// </summary>
        public async void ConnectAsync()
        {
            try
            {
                await client.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
                UpdateStatus($"Connection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Disconnect from OddSockets
        /// </summary>
        public void Disconnect()
        {
            client?.Disconnect();
        }

        /// <summary>
        /// Subscribe to a channel
        /// </summary>
        private async Task SubscribeToChannelAsync()
        {
            try
            {
                // Get or create channel
                channel = client.Channel(channelName);
                
                // Setup channel event handlers
                SetupChannelEvents();
                
                // Subscribe with options
                var options = new SubscriptionOptions
                {
                    MaxHistory = 50,
                    EnablePresence = enablePresence,
                    RetainHistory = retainHistory
                };
                
                await channel.SubscribeAsync(OnChannelMessage, options);
                
                Debug.Log($"Subscribed to channel: {channelName}");
                UpdateStatus($"Subscribed to channel: {channelName}");
                
                // Get initial presence if enabled
                if (enablePresence)
                {
                    var presence = await channel.GetPresenceAsync();
                    Debug.Log($"Channel has {presence.Count} users online");
                }
                
                // Get message history
                var history = await channel.GetHistoryAsync(new HistoryOptions { Count = 10 });
                Debug.Log($"Retrieved {history.Length} historical messages");
                
                foreach (var msg in history)
                {
                    AddMessageToHistory($"[History] {msg.Sender}: {msg.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Channel subscription failed: {ex.Message}");
                UpdateStatus($"Channel subscription failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup channel event handlers
        /// </summary>
        private void SetupChannelEvents()
        {
            if (channel == null) return;

            channel.OnMessage += OnChannelMessage;
            channel.OnSubscribed += OnChannelSubscribed;
            channel.OnUnsubscribed += OnChannelUnsubscribed;
            channel.OnPublished += OnChannelPublished;
            channel.OnPresence += OnChannelPresence;
            channel.OnPresenceChange += OnChannelPresenceChange;
            channel.OnHistory += OnChannelHistory;
        }

        /// <summary>
        /// Send a message to the channel
        /// </summary>
        public async void SendMessageAsync()
        {
            if (channel == null || !channel.IsSubscribed)
            {
                Debug.LogWarning("Not subscribed to any channel");
                UpdateStatus("Not subscribed to any channel");
                return;
            }

            try
            {
                string message = messageInput?.text ?? "Hello from Unity!";
                
                // Create message object
                var messageData = new
                {
                    text = message,
                    timestamp = DateTime.UtcNow,
                    sender = client.Config.UserId,
                    platform = "Unity"
                };

                // Publish message
                var result = await channel.PublishAsync(messageData);
                
                Debug.Log($"Message published: {result.MessageId}");
                
                // Clear input field
                if (messageInput != null)
                    messageInput.text = "";
                    
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send message: {ex.Message}");
                UpdateStatus($"Failed to send message: {ex.Message}");
            }
        }

        /// <summary>
        /// Send bulk messages example
        /// </summary>
        public async void SendBulkMessagesAsync()
        {
            if (channel == null || !channel.IsSubscribed)
            {
                Debug.LogWarning("Not subscribed to any channel");
                return;
            }

            try
            {
                var messages = new BulkMessage[]
                {
                    new BulkMessage
                    {
                        Channel = channelName,
                        Message = new { text = "Bulk message 1", type = "bulk" },
                        Options = new PublishOptions { Ttl = 3600 }
                    },
                    new BulkMessage
                    {
                        Channel = channelName,
                        Message = new { text = "Bulk message 2", type = "bulk" },
                        Options = new PublishOptions { Ttl = 3600 }
                    }
                };

                var results = await client.PublishBulkAsync(messages);
                
                Debug.Log($"Bulk publish completed: {results.Length} messages");
                
                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        Debug.Log($"Message published successfully");
                    }
                    else
                    {
                        Debug.LogError($"Message failed: {result.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Bulk publish failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Update user state example
        /// </summary>
        public async void UpdateUserStateAsync()
        {
            if (channel == null || !channel.IsSubscribed)
            {
                Debug.LogWarning("Not subscribed to any channel");
                return;
            }

            try
            {
                var state = new Dictionary<string, object>
                {
                    ["status"] = "online",
                    ["level"] = UnityEngine.Random.Range(1, 100),
                    ["location"] = new { x = transform.position.x, y = transform.position.y, z = transform.position.z },
                    ["lastActivity"] = DateTime.UtcNow
                };

                await channel.UpdateStateAsync(state);
                Debug.Log("User state updated");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update state: {ex.Message}");
            }
        }

        // Event Handlers

        private void OnConnecting()
        {
            Debug.Log("Connecting to OddSockets...");
            UpdateStatus("Connecting...");
            UpdateUI();
        }

        private async void OnConnected()
        {
            Debug.Log("Connected to OddSockets!");
            UpdateStatus("Connected! Subscribing to channel...");
            UpdateUI();
            
            // Auto-subscribe to channel after connection
            await SubscribeToChannelAsync();
        }

        private void OnDisconnected(string reason)
        {
            Debug.Log($"Disconnected from OddSockets: {reason}");
            UpdateStatus($"Disconnected: {reason}");
            UpdateUI();
        }

        private void OnError(Exception error)
        {
            Debug.LogError($"OddSockets error: {error.Message}");
            UpdateStatus($"Error: {error.Message}");
        }

        private void OnWorkerAssigned(WorkerAssignmentInfo info)
        {
            Debug.Log($"Assigned to worker: {info.WorkerId} at {info.WorkerUrl}");
            UpdateStatus($"Assigned to worker: {info.WorkerId}");
        }

        private void OnReconnecting(ReconnectInfo info)
        {
            Debug.Log($"Reconnecting... Attempt {info.Attempt}/{info.MaxAttempts} (delay: {info.Delay}ms)");
            UpdateStatus($"Reconnecting... ({info.Attempt}/{info.MaxAttempts})");
        }

        private void OnMaxReconnectAttemptsReached()
        {
            Debug.LogWarning("Max reconnect attempts reached");
            UpdateStatus("Connection failed - max attempts reached");
        }

        private void OnChannelMessage(ChannelMessageData data)
        {
            Debug.Log($"Message received from {data.Sender}: {data.Message}");
            AddMessageToHistory($"{data.Sender}: {data.Message}");
        }

        private void OnChannelSubscribed(ChannelSubscriptionData data)
        {
            Debug.Log($"Successfully subscribed to channel: {data.Channel}");
            UpdateStatus($"Subscribed to {data.Channel}");
        }

        private void OnChannelUnsubscribed(ChannelSubscriptionData data)
        {
            Debug.Log($"Unsubscribed from channel: {data.Channel}");
            UpdateStatus($"Unsubscribed from {data.Channel}");
        }

        private void OnChannelPublished(ChannelPublishData data)
        {
            Debug.Log($"Message published to {data.Channel}: {data.MessageId}");
        }

        private void OnChannelPresence(ChannelPresenceData data)
        {
            Debug.Log($"Presence update for {data.Channel}: {data.Count} users online");
            UpdateStatus($"Channel has {data.Count} users online");
        }

        private void OnChannelPresenceChange(ChannelPresenceChangeData data)
        {
            Debug.Log($"User {data.User.UserId} {data.Action}ed channel {data.Channel}");
            AddMessageToHistory($"[System] {data.User.UserId} {data.Action}ed the channel");
        }

        private void OnChannelHistory(ChannelHistoryData data)
        {
            Debug.Log($"Received {data.Messages.Length} historical messages for {data.Channel}");
        }

        // UI Helper Methods

        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = $"Status: {status}";
            }
        }

        private void AddMessageToHistory(string message)
        {
            messageHistory.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // Keep only last 20 messages
            if (messageHistory.Count > 20)
            {
                messageHistory.RemoveAt(0);
            }
            
            UpdateMessagesDisplay();
        }

        private void UpdateMessagesDisplay()
        {
            if (messagesText != null)
            {
                messagesText.text = string.Join("\n", messageHistory);
            }
        }

        private void UpdateUI()
        {
            bool isConnected = client?.State == ConnectionState.Connected;
            bool isConnecting = client?.State == ConnectionState.Connecting || client?.State == ConnectionState.Reconnecting;
            
            if (connectButton != null)
                connectButton.interactable = !isConnected && !isConnecting;
                
            if (disconnectButton != null)
                disconnectButton.interactable = isConnected;
                
            if (sendMessageButton != null)
                sendMessageButton.interactable = isConnected && channel?.IsSubscribed == true;
        }

        /// <summary>
        /// Cleanup client resources
        /// </summary>
        private void CleanupClient()
        {
            if (channel != null)
            {
                // Cleanup channel events
                channel.OnMessage -= OnChannelMessage;
                channel.OnSubscribed -= OnChannelSubscribed;
                channel.OnUnsubscribed -= OnChannelUnsubscribed;
                channel.OnPublished -= OnChannelPublished;
                channel.OnPresence -= OnChannelPresence;
                channel.OnPresenceChange -= OnChannelPresenceChange;
                channel.OnHistory -= OnChannelHistory;
            }

            if (client != null)
            {
                // Cleanup client events
                client.OnConnecting -= OnConnecting;
                client.OnConnected -= OnConnected;
                client.OnDisconnected -= OnDisconnected;
                client.OnError -= OnError;
                client.OnWorkerAssigned -= OnWorkerAssigned;
                client.OnReconnecting -= OnReconnecting;
                client.OnMaxReconnectAttemptsReached -= OnMaxReconnectAttemptsReached;
                
                // Disconnect if connected
                client.Disconnect();
            }
        }

        // Public methods for testing

        /// <summary>
        /// Test message size validation
        /// </summary>
        [ContextMenu("Test Message Size Validation")]
        public void TestMessageSizeValidation()
        {
            var smallMessage = "Hello World!";
            var largeMessage = new string('A', 40000); // 40KB message

            var smallInfo = MessageSizeValidator.GetMessageSizeInfo(smallMessage);
            var largeInfo = MessageSizeValidator.GetMessageSizeInfo(largeMessage);

            Debug.Log($"Small message: {smallInfo.SizeKB}KB, Valid: {smallInfo.IsValid}");
            Debug.Log($"Large message: {largeInfo.SizeKB}KB, Valid: {largeInfo.IsValid}");
        }

        /// <summary>
        /// Test manager connectivity
        /// </summary>
        [ContextMenu("Test Manager Connectivity")]
        public async void TestManagerConnectivity()
        {
            if (client?.Config?.ApiKey == null)
            {
                Debug.LogError("No API key configured");
                return;
            }

            var isReachable = await ManagerDiscovery.Instance.TestConnectivityAsync(client.Config.ApiKey);
            Debug.Log($"Manager reachable: {isReachable}");

            var managerInfo = await ManagerDiscovery.Instance.GetManagerInfoAsync(client.Config.ApiKey);
            if (managerInfo != null)
            {
                Debug.Log($"Manager info: {managerInfo.Version}, Status: {managerInfo.Status}, Workers: {managerInfo.ActiveWorkers}");
            }
        }
    }
}
