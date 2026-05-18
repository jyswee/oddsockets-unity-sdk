using System;
using System.Collections.Generic;
using UnityEngine;

namespace OddSockets.Unity
{
    /// <summary>
    /// Represents a message received from OddSockets.
    /// </summary>
    [Serializable]
    public class OddSocketsMessage
    {
        [SerializeField] private string channel;
        [SerializeField] private object data;
        [SerializeField] private string messageId;
        [SerializeField] private string userId;
        [SerializeField] private DateTime timestamp;
        [SerializeField] private Dictionary<string, object> metadata;

        /// <summary>
        /// The channel this message was received on.
        /// </summary>
        public string Channel => channel;

        /// <summary>
        /// The message data.
        /// </summary>
        public object Data => data;

        /// <summary>
        /// Unique message identifier.
        /// </summary>
        public string MessageId => messageId;

        /// <summary>
        /// User ID of the message sender.
        /// </summary>
        public string UserId => userId;

        /// <summary>
        /// Timestamp when the message was sent.
        /// </summary>
        public DateTime Timestamp => timestamp;

        /// <summary>
        /// Additional message metadata.
        /// </summary>
        public Dictionary<string, object> Metadata => metadata ?? new Dictionary<string, object>();

        /// <summary>
        /// Creates a new OddSocketsMessage.
        /// </summary>
        public OddSocketsMessage(string channel, object data, string messageId = null, string userId = null, DateTime? timestamp = null, Dictionary<string, object> metadata = null)
        {
            this.channel = channel;
            this.data = data;
            this.messageId = messageId ?? Guid.NewGuid().ToString();
            this.userId = userId;
            this.timestamp = timestamp ?? DateTime.UtcNow;
            this.metadata = metadata ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the message data as a specific type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The data converted to the specified type.</returns>
        public T GetData<T>()
        {
            if (data is T directCast)
            {
                return directCast;
            }

            try
            {
                if (data is string jsonString)
                {
                    return JsonUtility.FromJson<T>(jsonString);
                }

                // Try to serialize and deserialize for complex objects
                var json = JsonUtility.ToJson(data);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to convert message data to {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Gets a metadata value by key.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <returns>The metadata value, or null if not found.</returns>
        public object GetMetadata(string key)
        {
            return Metadata.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Gets a metadata value by key as a specific type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="key">The metadata key.</param>
        /// <returns>The metadata value converted to the specified type.</returns>
        public T GetMetadata<T>(string key)
        {
            var value = GetMetadata(key);
            if (value is T directCast)
            {
                return directCast;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        public override string ToString()
        {
            return $"OddSocketsMessage(Channel: {Channel}, MessageId: {MessageId}, UserId: {UserId}, Timestamp: {Timestamp})";
        }
    }

    /// <summary>
    /// Represents a bulk message for batch publishing.
    /// </summary>
    [Serializable]
    public class BulkMessage
    {
        [SerializeField] private string channel;
        [SerializeField] private object message;
        [SerializeField] private PublishOptions options;

        /// <summary>
        /// The channel to publish to.
        /// </summary>
        public string Channel => channel;

        /// <summary>
        /// The message to publish.
        /// </summary>
        public object Message => message;

        /// <summary>
        /// Publishing options.
        /// </summary>
        public PublishOptions Options => options;

        /// <summary>
        /// Creates a new BulkMessage.
        /// </summary>
        public BulkMessage(string channel, object message, PublishOptions options = null)
        {
            this.channel = channel;
            this.message = message;
            this.options = options ?? new PublishOptions();
        }
    }

    /// <summary>
    /// Options for publishing messages.
    /// </summary>
    [Serializable]
    public class PublishOptions
    {
        [SerializeField] private int ttl;
        [SerializeField] private Dictionary<string, object> metadata;
        [SerializeField] private bool storeInHistory = true;

        /// <summary>
        /// Time to live in seconds (0 = no expiration).
        /// </summary>
        public int TTL
        {
            get => ttl;
            set => ttl = value;
        }

        /// <summary>
        /// Additional message metadata.
        /// </summary>
        public Dictionary<string, object> Metadata
        {
            get => metadata ?? (metadata = new Dictionary<string, object>());
            set => metadata = value;
        }

        /// <summary>
        /// Whether to store this message in channel history.
        /// </summary>
        public bool StoreInHistory
        {
            get => storeInHistory;
            set => storeInHistory = value;
        }

        /// <summary>
        /// Creates new PublishOptions.
        /// </summary>
        public PublishOptions(int ttl = 0, Dictionary<string, object> metadata = null, bool storeInHistory = true)
        {
            this.ttl = ttl;
            this.metadata = metadata;
            this.storeInHistory = storeInHistory;
        }
    }

    /// <summary>
    /// Result of a bulk publish operation.
    /// </summary>
    [Serializable]
    public class BulkResult
    {
        [SerializeField] private bool success;
        [SerializeField] private string error;
        [SerializeField] private object result;

        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool Success => success;

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        public string Error => error;

        /// <summary>
        /// Result data if the operation succeeded.
        /// </summary>
        public object Result => result;

        /// <summary>
        /// Creates a new BulkResult.
        /// </summary>
        public BulkResult(bool success, string error = null, object result = null)
        {
            this.success = success;
            this.error = error;
            this.result = result;
        }
    }

    /// <summary>
    /// Subscription options for channels.
    /// </summary>
    [Serializable]
    public class SubscriptionOptions
    {
        [SerializeField] private int maxHistory = 100;
        [SerializeField] private bool retainHistory = true;
        [SerializeField] private bool enablePresence = false;

        /// <summary>
        /// Maximum number of history messages to retain.
        /// </summary>
        public int MaxHistory
        {
            get => maxHistory;
            set => maxHistory = value;
        }

        /// <summary>
        /// Whether to retain message history locally.
        /// </summary>
        public bool RetainHistory
        {
            get => retainHistory;
            set => retainHistory = value;
        }

        /// <summary>
        /// Whether to enable presence tracking.
        /// </summary>
        public bool EnablePresence
        {
            get => enablePresence;
            set => enablePresence = value;
        }

        /// <summary>
        /// Creates new SubscriptionOptions.
        /// </summary>
        public SubscriptionOptions(int maxHistory = 100, bool retainHistory = true, bool enablePresence = false)
        {
            this.maxHistory = maxHistory;
            this.retainHistory = retainHistory;
            this.enablePresence = enablePresence;
        }
    }

    /// <summary>
    /// Presence information for a channel.
    /// </summary>
    [Serializable]
    public class PresenceInfo
    {
        [SerializeField] private string channel;
        [SerializeField] private int occupancy;
        [SerializeField] private List<PresenceUser> occupants;

        /// <summary>
        /// The channel name.
        /// </summary>
        public string Channel => channel;

        /// <summary>
        /// Number of users in the channel.
        /// </summary>
        public int Occupancy => occupancy;

        /// <summary>
        /// List of users in the channel.
        /// </summary>
        public List<PresenceUser> Occupants => occupants ?? new List<PresenceUser>();

        /// <summary>
        /// Creates new PresenceInfo.
        /// </summary>
        public PresenceInfo(string channel, int occupancy, List<PresenceUser> occupants = null)
        {
            this.channel = channel;
            this.occupancy = occupancy;
            this.occupants = occupants ?? new List<PresenceUser>();
        }
    }

    /// <summary>
    /// Represents a user in presence information.
    /// </summary>
    [Serializable]
    public class PresenceUser
    {
        [SerializeField] private string userId;
        [SerializeField] private Dictionary<string, object> state;
        [SerializeField] private DateTime joinedAt;

        /// <summary>
        /// The user ID.
        /// </summary>
        public string UserId => userId;

        /// <summary>
        /// User state data.
        /// </summary>
        public Dictionary<string, object> State => state ?? new Dictionary<string, object>();

        /// <summary>
        /// When the user joined the channel.
        /// </summary>
        public DateTime JoinedAt => joinedAt;

        /// <summary>
        /// Creates a new PresenceUser.
        /// </summary>
        public PresenceUser(string userId, Dictionary<string, object> state = null, DateTime? joinedAt = null)
        {
            this.userId = userId;
            this.state = state ?? new Dictionary<string, object>();
            this.joinedAt = joinedAt ?? DateTime.UtcNow;
        }
    }
}
