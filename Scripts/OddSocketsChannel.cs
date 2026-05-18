using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SocketIOClient;
using Newtonsoft.Json;

namespace OddSockets.Unity
{
    /// <summary>
    /// Message size limits (industry standard - matches PubNub)
    /// </summary>
    public static class MessageSizeLimits
    {
        public const int MAX_MESSAGE_SIZE = 32768; // 32KB in bytes
        public const int MAX_MESSAGE_SIZE_KB = 32;
    }

    /// <summary>
    /// Channel class for pub/sub messaging
    /// 
    /// Provides methods for subscribing, publishing, and managing presence
    /// on a specific channel within the OddSockets platform.
    /// </summary>
    public class OddSocketsChannel
    {
        // Events
        public event Action<ChannelMessageData> OnMessage;
        public event Action<ChannelSubscriptionData> OnSubscribed;
        public event Action<ChannelSubscriptionData> OnUnsubscribed;
        public event Action<ChannelPublishData> OnPublished;
        public event Action<ChannelPresenceData> OnPresence;
        public event Action<ChannelPresenceChangeData> OnPresenceChange;
        public event Action<ChannelHistoryData> OnHistory;

        // Private fields
        private readonly string name;
        private readonly OddSocketsClient client;
        private bool subscribed = false;
        private bool subscribing = false;
        private SubscriptionOptions options;
        private Dictionary<string, PresenceUser> presence;
        private List<ChannelMessageData> messageHistory;
        private int maxHistorySize = 100;

        /// <summary>
        /// Channel name
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Whether channel is subscribed
        /// </summary>
        public bool IsSubscribed => subscribed;

        /// <summary>
        /// Current presence map
        /// </summary>
        public Dictionary<string, PresenceUser> PresenceMap => new Dictionary<string, PresenceUser>(presence);

        /// <summary>
        /// Cached message history
        /// </summary>
        public List<ChannelMessageData> CachedHistory => new List<ChannelMessageData>(messageHistory);

        /// <summary>
        /// Create a Channel instance
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="oddSocketsClient">Parent OddSockets client</param>
        internal OddSocketsChannel(string channelName, OddSocketsClient oddSocketsClient)
        {
            name = channelName;
            client = oddSocketsClient;
            presence = new Dictionary<string, PresenceUser>();
            messageHistory = new List<ChannelMessageData>();
        }

        /// <summary>
        /// Subscribe to the channel
        /// </summary>
        /// <param name="messageCallback">Message callback function</param>
        /// <param name="subscriptionOptions">Subscription options</param>
        /// <returns>Task representing the subscription operation</returns>
        public async Task SubscribeAsync(Action<ChannelMessageData> messageCallback = null, SubscriptionOptions subscriptionOptions = null)
        {
            if (subscribed || subscribing)
            {
                // Add callback to existing subscription
                if (messageCallback != null)
                {
                    OnMessage += messageCallback;
                }
                return;
            }

            if (!client.IsConnected())
            {
                throw new InvalidOperationException("Client is not connected");
            }

            subscribing = true;
            options = subscriptionOptions ?? new SubscriptionOptions();
            maxHistorySize = options.MaxHistory;

            var tcs = new TaskCompletionSource<bool>();
            var socket = client.GetSocket();

            void OnSubscribedHandler(ChannelSubscriptionData data)
            {
                if (data.Channel == name)
                {
                    subscribed = true;
                    subscribing = false;
                    
                    if (messageCallback != null)
                    {
                        OnMessage += messageCallback;
                    }

                    OnSubscribed?.Invoke(data);
                    tcs.TrySetResult(true);
                }
            }

            void OnErrorHandler(string error)
            {
                subscribing = false;
                tcs.TrySetException(new Exception(error));
            }

            // Set up temporary listeners
            OnSubscribed += OnSubscribedHandler;
            client.OnError += (ex) => OnErrorHandler(ex.Message);

            // Send subscription request
            await socket.EmitAsync("subscribe", new
            {
                channel = name,
                options = new
                {
                    maxHistory = options.MaxHistory,
                    retainHistory = options.RetainHistory,
                    enablePresence = options.EnablePresence
                }
            });

            // Wait for subscription or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            // Clean up temporary listeners
            OnSubscribed -= OnSubscribedHandler;

            if (completedTask == timeoutTask)
            {
                subscribing = false;
                throw new TimeoutException("Subscription timeout");
            }

            await tcs.Task;
        }

        /// <summary>
        /// Unsubscribe from the channel
        /// </summary>
        /// <returns>Task representing the unsubscription operation</returns>
        public async Task UnsubscribeAsync()
        {
            if (!subscribed)
            {
                return;
            }

            if (!client.IsConnected())
            {
                throw new InvalidOperationException("Client is not connected");
            }

            var tcs = new TaskCompletionSource<bool>();
            var socket = client.GetSocket();

            void OnUnsubscribedHandler(ChannelSubscriptionData data)
            {
                if (data.Channel == name)
                {
                    subscribed = false;
                    OnMessage = null; // Clear all message listeners

                    OnUnsubscribed?.Invoke(data);
                    tcs.TrySetResult(true);
                }
            }

            void OnErrorHandler(string error)
            {
                tcs.TrySetException(new Exception(error));
            }

            // Set up temporary listeners
            OnUnsubscribed += OnUnsubscribedHandler;
            client.OnError += (ex) => OnErrorHandler(ex.Message);

            // Send unsubscription request
            await socket.EmitAsync("unsubscribe", new
            {
                channel = name
            });

            // Wait for unsubscription or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            // Clean up temporary listeners
            OnUnsubscribed -= OnUnsubscribedHandler;

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Unsubscription timeout");
            }

            await tcs.Task;
        }

        /// <summary>
        /// Publish a message to the channel
        /// </summary>
        /// <param name="message">Message to publish (string, object, or array)</param>
        /// <param name="publishOptions">Publishing options</param>
        /// <returns>Publication result</returns>
        public async Task<ChannelPublishData> PublishAsync(object message, PublishOptions publishOptions = null)
        {
            if (!client.IsConnected())
            {
                throw new InvalidOperationException("Client is not connected");
            }

            // Validate message size before publishing
            ValidateMessageSize(message);

            var tcs = new TaskCompletionSource<ChannelPublishData>();
            var socket = client.GetSocket();

            void OnPublishedHandler(ChannelPublishData data)
            {
                if (data.Channel == name)
                {
                    OnPublished?.Invoke(data);
                    tcs.TrySetResult(data);
                }
            }

            void OnErrorHandler(string error)
            {
                tcs.TrySetException(new Exception(error));
            }

            // Set up temporary listeners
            OnPublished += OnPublishedHandler;
            client.OnError += (ex) => OnErrorHandler(ex.Message);

            // Send publish request
            await socket.EmitAsync("publish", new
            {
                channel = name,
                message = message,
                options = publishOptions ?? new PublishOptions()
            });

            // Wait for publish confirmation or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            // Clean up temporary listeners
            OnPublished -= OnPublishedHandler;

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Publish timeout");
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Get message history for the channel
        /// </summary>
        /// <param name="historyOptions">History options</param>
        /// <returns>Message history</returns>
        public async Task<ChannelMessageData[]> GetHistoryAsync(HistoryOptions historyOptions = null)
        {
            if (!client.IsConnected())
            {
                throw new InvalidOperationException("Client is not connected");
            }

            var options = historyOptions ?? new HistoryOptions();
            var tcs = new TaskCompletionSource<ChannelMessageData[]>();
            var socket = client.GetSocket();

            void OnHistoryHandler(ChannelHistoryData data)
            {
                if (data.Channel == name)
                {
                    OnHistory?.Invoke(data);
                    tcs.TrySetResult(data.Messages ?? new ChannelMessageData[0]);
                }
            }

            void OnErrorHandler(string error)
            {
                tcs.TrySetException(new Exception(error));
            }

            // Set up temporary listeners
            OnHistory += OnHistoryHandler;
            client.OnError += (ex) => OnErrorHandler(ex.Message);

            // Send history request
            await socket.EmitAsync("get_history", new
            {
                channel = name,
                count = options.Count,
                start = options.Start?.ToString("O"),
                end = options.End?.ToString("O")
            });

            // Wait for history or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            // Clean up temporary listeners
            OnHistory -= OnHistoryHandler;

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("History request timeout");
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Get current presence information
        /// </summary>
        /// <returns>Presence information</returns>
        public async Task<ChannelPresenceData> GetPresenceAsync()
        {
            if (!client.IsConnected())
            {
                throw new InvalidOperationException("Client is not connected");
            }

            var tcs = new TaskCompletionSource<ChannelPresenceData>();
            var socket = client.GetSocket();

            void OnPresenceHandler(ChannelPresenceData data)
            {
                if (data.Channel == name)
                {
                    OnPresence?.Invoke(data);
                    tcs.TrySetResult(data);
                }
            }

            void OnErrorHandler(string error)
            {
                tcs.TrySetException(new Exception(error));
            }

            // Set up temporary listeners
            OnPresence += OnPresenceHandler;
            client.OnError += (ex) => OnErrorHandler(ex.Message);

            // Send presence request
            await socket.EmitAsync("get_presence", new
            {
                channel = name
            });

            // Wait for presence or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            // Clean up temporary listeners
            OnPresence -= OnPresenceHandler;

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Presence request timeout");
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Update user state
        /// </summary>
        /// <param name="state">User state object</param>
        /// <returns>Task representing the state update operation</returns>
        public async Task UpdateStateAsync(Dictionary<string, object> state)
        {
            if (!client.IsConnected())
            {
                throw new InvalidOperationException("Client is not connected");
            }

            var tcs = new TaskCompletionSource<bool>();
            var socket = client.GetSocket();

            void OnStateUpdatedHandler()
            {
                tcs.TrySetResult(true);
            }

            void OnErrorHandler(string error)
            {
                tcs.TrySetException(new Exception(error));
            }

            // Set up temporary listeners
            client.OnError += (ex) => OnErrorHandler(ex.Message);

            // Send state update request
            await socket.EmitAsync("update_state", new
            {
                state = state
            });

            // Wait for state update or timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("State update timeout");
            }

            await tcs.Task;
        }

        /// <summary>
        /// Validate message size
        /// </summary>
        /// <param name="message">Message to validate</param>
        /// <exception cref="ArgumentException">If message exceeds size limit</exception>
        private void ValidateMessageSize(object message)
        {
            string messageStr = message is string str ? str : JsonConvert.SerializeObject(message);
            int messageSize = System.Text.Encoding.UTF8.GetByteCount(messageStr);

            if (messageSize > MessageSizeLimits.MAX_MESSAGE_SIZE)
            {
                throw new ArgumentException(
                    $"Message size ({messageSize / 1024}KB) exceeds maximum allowed size of {MessageSizeLimits.MAX_MESSAGE_SIZE_KB}KB. " +
                    "This limit matches industry standards (PubNub, Socket.IO) for reliable real-time messaging."
                );
            }
        }

        /// <summary>
        /// Internal: Handle incoming message
        /// </summary>
        /// <param name="data">Message data</param>
        internal void HandleMessage(ChannelMessageData data)
        {
            // Add to history if enabled
            if (options?.RetainHistory == true)
            {
                messageHistory.Add(data);

                // Trim history if too large
                if (messageHistory.Count > maxHistorySize)
                {
                    messageHistory.RemoveRange(0, messageHistory.Count - maxHistorySize);
                }
            }

            OnMessage?.Invoke(data);
        }

        /// <summary>
        /// Internal: Handle subscription confirmation
        /// </summary>
        /// <param name="data">Subscription data</param>
        internal void HandleSubscribed(ChannelSubscriptionData data)
        {
            OnSubscribed?.Invoke(data);
        }

        /// <summary>
        /// Internal: Handle unsubscription confirmation
        /// </summary>
        /// <param name="data">Unsubscription data</param>
        internal void HandleUnsubscribed(ChannelSubscriptionData data)
        {
            OnUnsubscribed?.Invoke(data);
        }

        /// <summary>
        /// Internal: Handle publish confirmation
        /// </summary>
        /// <param name="data">Publish confirmation data</param>
        internal void HandlePublished(ChannelPublishData data)
        {
            OnPublished?.Invoke(data);
        }

        /// <summary>
        /// Internal: Handle presence information
        /// </summary>
        /// <param name="data">Presence data</param>
        internal void HandlePresence(ChannelPresenceData data)
        {
            // Update presence map
            if (data.Occupants != null)
            {
                presence.Clear();
                foreach (var occupant in data.Occupants)
                {
                    presence[occupant.UserId] = occupant;
                }
            }

            OnPresence?.Invoke(data);
        }

        /// <summary>
        /// Internal: Handle presence changes
        /// </summary>
        /// <param name="data">Presence change data</param>
        internal void HandlePresenceChange(ChannelPresenceChangeData data)
        {
            // Update presence map
            if (data.Action == "join")
            {
                presence[data.User.UserId] = data.User;
            }
            else if (data.Action == "leave")
            {
                presence.Remove(data.User.UserId);
            }

            OnPresenceChange?.Invoke(data);
        }

        /// <summary>
        /// Internal: Handle message history
        /// </summary>
        /// <param name="data">History data</param>
        internal void HandleHistory(ChannelHistoryData data)
        {
            OnHistory?.Invoke(data);
        }
    }

    // Supporting option classes
    [Serializable]
    public class SubscriptionOptions
    {
        /// <summary>
        /// Maximum history messages to retain
        /// </summary>
        public int MaxHistory { get; set; } = 100;

        /// <summary>
        /// Whether to retain message history
        /// </summary>
        public bool RetainHistory { get; set; } = true;

        /// <summary>
        /// Whether to enable presence tracking
        /// </summary>
        public bool EnablePresence { get; set; } = false;
    }

    [Serializable]
    public class HistoryOptions
    {
        /// <summary>
        /// Number of messages to retrieve
        /// </summary>
        public int Count { get; set; } = 50;

        /// <summary>
        /// Start time (ISO string)
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// End time (ISO string)
        /// </summary>
        public DateTime? End { get; set; }
    }
}
