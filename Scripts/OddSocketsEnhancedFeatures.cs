using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OddSockets.Unity
{
    /// <summary>
    /// Enhanced real-time features backed by the OddSockets worker's enhanced
    /// event handlers: emoji reactions, threaded replies, message editing,
    /// read receipts, presence/status, file-upload progress, direct messages,
    /// notifications and channel management.
    ///
    /// Every request method emits the exact worker event with the payload shape
    /// the worker expects. Every server-emitted broadcast/response is surfaced as
    /// a C# event carrying the raw Newtonsoft <see cref="JToken"/> payload — the
    /// idiomatic type for Unity consumers, since enhanced-event shapes vary.
    /// Anything not surfaced here can still be reached via
    /// <see cref="OddSocketsClient.EmitAsync"/> / <see cref="OddSocketsClient.On"/>.
    ///
    /// Access via <see cref="OddSocketsClient.Enhanced"/>. Delivery of these
    /// events is tenant-scoped by the worker: a subscriber only receives events
    /// for channels/users within its own API-key account.
    /// </summary>
    public class OddSocketsEnhancedFeatures
    {
        private readonly OddSocketsClient client;

        // ── Reactions ──
        public event Action<JToken> OnReactionAdded;
        public event Action<JToken> OnReactionRemoved;
        public event Action<JToken> OnReactionsData;

        // ── Threads ──
        public event Action<JToken> OnThreadReply;
        public event Action<JToken> OnThreadData;
        public event Action<JToken> OnThreadSubscribed;
        public event Action<JToken> OnThreadFollowed;
        public event Action<JToken> OnThreadUnfollowed;
        public event Action<JToken> OnThreadReadUpdated;

        // ── Message editing ──
        public event Action<JToken> OnMessageEdited;
        public event Action<JToken> OnMessageDeleted;
        public event Action<JToken> OnMessagePinned;
        public event Action<JToken> OnMessageUnpinned;

        // ── Read receipts ──
        public event Action<JToken> OnUserRead;
        public event Action<JToken> OnUnreadCountUpdated;
        public event Action<JToken> OnUnreadCounts;
        public event Action<JToken> OnAllMarkedRead;

        // ── Presence / status ──
        public event Action<JToken> OnUserStatusChanged;
        public event Action<JToken> OnCustomStatusUpdated;
        public event Action<JToken> OnCustomStatusCleared;
        public event Action<JToken> OnDndStatusChanged;
        public event Action<JToken> OnUserTyping;
        public event Action<JToken> OnUserStoppedTyping;
        public event Action<JToken> OnUserPresenceData;

        // ── File uploads ──
        public event Action<JToken> OnFileUploadStarted;
        public event Action<JToken> OnUploadProgress;
        public event Action<JToken> OnFileUploadProgress;
        public event Action<JToken> OnUploadCompleted;
        public event Action<JToken> OnFileUploadCompleted;
        public event Action<JToken> OnUploadFailed;
        public event Action<JToken> OnUploadStatus;
        public event Action<JToken> OnUploadCancelled;
        public event Action<JToken> OnChannelFiles;

        // ── Direct messages ──
        public event Action<JToken> OnDmCreated;
        public event Action<JToken> OnDmReceived;
        public event Action<JToken> OnDmConversations;
        public event Action<JToken> OnDmHistory;
        public event Action<JToken> OnDmMuted;
        public event Action<JToken> OnDmArchived;

        // ── Notifications ──
        public event Action<JToken> OnNotification;
        public event Action<JToken> OnNotificationsSubscribed;
        public event Action<JToken> OnNotificationRead;
        public event Action<JToken> OnAllNotificationsRead;
        public event Action<JToken> OnNotificationsCleared;
        public event Action<JToken> OnNotificationsData;
        public event Action<JToken> OnUnreadCount;

        // ── Channel management ──
        public event Action<JToken> OnChannelCreated;
        public event Action<JToken> OnChannelUpdated;
        public event Action<JToken> OnChannelArchived;
        public event Action<JToken> OnUserInvited;
        public event Action<JToken> OnUserJoinedChannel;
        public event Action<JToken> OnUserLeftChannel;
        public event Action<JToken> OnUserRemoved;
        public event Action<JToken> OnChannelMembers;

        internal OddSocketsEnhancedFeatures(OddSocketsClient oddSocketsClient)
        {
            client = oddSocketsClient ?? throw new ArgumentNullException(nameof(oddSocketsClient));
            WireServerEvents();
        }

        /// <summary>
        /// Bind each worker-emitted event to its C# event. Listeners are held by
        /// the client and re-bound across reconnects, so this is safe to call once.
        /// </summary>
        private void WireServerEvents()
        {
            client.On("reaction_added", p => OnReactionAdded?.Invoke(p));
            client.On("reaction_removed", p => OnReactionRemoved?.Invoke(p));
            client.On("reactions_data", p => OnReactionsData?.Invoke(p));

            client.On("thread_reply", p => OnThreadReply?.Invoke(p));
            client.On("thread_data", p => OnThreadData?.Invoke(p));
            client.On("thread_subscribed", p => OnThreadSubscribed?.Invoke(p));
            client.On("thread_followed", p => OnThreadFollowed?.Invoke(p));
            client.On("thread_unfollowed", p => OnThreadUnfollowed?.Invoke(p));
            client.On("thread_read_updated", p => OnThreadReadUpdated?.Invoke(p));

            client.On("message_edited", p => OnMessageEdited?.Invoke(p));
            client.On("message_deleted", p => OnMessageDeleted?.Invoke(p));
            client.On("message_pinned", p => OnMessagePinned?.Invoke(p));
            client.On("message_unpinned", p => OnMessageUnpinned?.Invoke(p));

            client.On("user_read", p => OnUserRead?.Invoke(p));
            client.On("unread_count_updated", p => OnUnreadCountUpdated?.Invoke(p));
            client.On("unread_counts", p => OnUnreadCounts?.Invoke(p));
            client.On("all_marked_read", p => OnAllMarkedRead?.Invoke(p));

            client.On("user_status_changed", p => OnUserStatusChanged?.Invoke(p));
            client.On("custom_status_updated", p => OnCustomStatusUpdated?.Invoke(p));
            client.On("custom_status_cleared", p => OnCustomStatusCleared?.Invoke(p));
            client.On("dnd_status_changed", p => OnDndStatusChanged?.Invoke(p));
            client.On("user_typing", p => OnUserTyping?.Invoke(p));
            client.On("user_stopped_typing", p => OnUserStoppedTyping?.Invoke(p));
            client.On("user_presence_data", p => OnUserPresenceData?.Invoke(p));

            client.On("file_upload_started", p => OnFileUploadStarted?.Invoke(p));
            client.On("upload_progress_update", p => OnUploadProgress?.Invoke(p));
            client.On("file_upload_progress", p => OnFileUploadProgress?.Invoke(p));
            client.On("upload_completed", p => OnUploadCompleted?.Invoke(p));
            client.On("file_upload_completed", p => OnFileUploadCompleted?.Invoke(p));
            client.On("upload_failed", p => OnUploadFailed?.Invoke(p));
            client.On("upload_status", p => OnUploadStatus?.Invoke(p));
            client.On("upload_cancelled", p => OnUploadCancelled?.Invoke(p));
            client.On("channel_files", p => OnChannelFiles?.Invoke(p));

            client.On("dm_created", p => OnDmCreated?.Invoke(p));
            client.On("dm_received", p => OnDmReceived?.Invoke(p));
            client.On("dm_conversations", p => OnDmConversations?.Invoke(p));
            client.On("dm_history", p => OnDmHistory?.Invoke(p));
            client.On("dm_muted", p => OnDmMuted?.Invoke(p));
            client.On("dm_archived", p => OnDmArchived?.Invoke(p));

            client.On("notification", p => OnNotification?.Invoke(p));
            client.On("notifications_subscribed", p => OnNotificationsSubscribed?.Invoke(p));
            client.On("notification_read", p => OnNotificationRead?.Invoke(p));
            client.On("all_notifications_read", p => OnAllNotificationsRead?.Invoke(p));
            client.On("notifications_cleared", p => OnNotificationsCleared?.Invoke(p));
            client.On("notifications_data", p => OnNotificationsData?.Invoke(p));
            client.On("unread_count", p => OnUnreadCount?.Invoke(p));

            client.On("channel_created", p => OnChannelCreated?.Invoke(p));
            client.On("channel_updated", p => OnChannelUpdated?.Invoke(p));
            client.On("channel_archived", p => OnChannelArchived?.Invoke(p));
            client.On("user_invited", p => OnUserInvited?.Invoke(p));
            client.On("user_joined_channel", p => OnUserJoinedChannel?.Invoke(p));
            client.On("user_left_channel", p => OnUserLeftChannel?.Invoke(p));
            client.On("user_removed", p => OnUserRemoved?.Invoke(p));
            client.On("channel_members", p => OnChannelMembers?.Invoke(p));
        }

        // ─────────────────────────── Reactions ───────────────────────────

        /// <summary>Add an emoji reaction to a message.</summary>
        public Task AddReactionAsync(string messageId, string channel, string emoji,
            string userId, string emojiCode = null, string userName = null)
            => client.EmitAsync("add_reaction", new
            {
                messageId, channel, emoji, emojiCode, userId, userName
            });

        /// <summary>Remove an emoji reaction from a message.</summary>
        public Task RemoveReactionAsync(string messageId, string channel, string emoji, string userId)
            => client.EmitAsync("remove_reaction", new { messageId, channel, emoji, userId });

        /// <summary>Request the reaction counts for a message (see OnReactionsData).</summary>
        public Task GetReactionsAsync(string messageId)
            => client.EmitAsync("get_reactions", new { messageId });

        // ──────────────────────────── Threads ────────────────────────────

        /// <summary>Post a reply in the thread of a parent message.</summary>
        public Task ThreadReplyAsync(string channel, string parentMessageId, string message,
            string userId, string userName = null, object metadata = null)
            => client.EmitAsync("thread_reply", new
            {
                channel, parentMessageId, message, userId, userName,
                metadata = metadata ?? new { }
            });

        /// <summary>Fetch a thread's messages (see OnThreadData).</summary>
        public Task GetThreadAsync(string channel, string parentMessageId, int count = 50)
            => client.EmitAsync("get_thread", new { channel, parentMessageId, count });

        /// <summary>Subscribe to live updates for a thread (see OnThreadSubscribed).</summary>
        public Task SubscribeThreadAsync(string channel, string threadId)
            => client.EmitAsync("subscribe_thread", new { channel, threadId });

        /// <summary>Mark a thread read up to a message.</summary>
        public Task MarkThreadReadAsync(string channel, string threadId, string lastReadMessageId, string userId)
            => client.EmitAsync("mark_thread_read", new { channel, threadId, lastReadMessageId, userId });

        /// <summary>Follow a thread to receive reply notifications.</summary>
        public Task FollowThreadAsync(string threadId, string userId)
            => client.EmitAsync("follow_thread", new { threadId, userId });

        /// <summary>Stop following a thread.</summary>
        public Task UnfollowThreadAsync(string threadId, string userId)
            => client.EmitAsync("unfollow_thread", new { threadId, userId });

        // ───────────────────────── Message editing ───────────────────────

        /// <summary>Edit a previously published message.</summary>
        public Task EditMessageAsync(string messageId, string channel, string newContent, string userId)
            => client.EmitAsync("edit_message", new { messageId, channel, newContent, userId });

        /// <summary>Delete a message.</summary>
        public Task DeleteMessageAsync(string messageId, string channel, string userId)
            => client.EmitAsync("delete_message", new { messageId, channel, userId });

        /// <summary>Pin a message in a channel.</summary>
        public Task PinMessageAsync(string messageId, string channel, string userId)
            => client.EmitAsync("pin_message", new { messageId, channel, userId });

        /// <summary>Unpin a message.</summary>
        public Task UnpinMessageAsync(string messageId, string channel, string userId)
            => client.EmitAsync("unpin_message", new { messageId, channel, userId });

        /// <summary>Request the pinned messages for a channel.</summary>
        public Task GetPinnedMessagesAsync(string channel)
            => client.EmitAsync("get_pinned_messages", new { channel });

        // ────────────────────────── Read receipts ────────────────────────

        /// <summary>Mark a channel read up to a message (broadcasts OnUserRead).</summary>
        public Task MarkReadAsync(string channel, string userId, string lastReadMessageId)
            => client.EmitAsync("mark_read", new { channel, userId, lastReadMessageId });

        /// <summary>Request unread counts across channels (see OnUnreadCounts).</summary>
        public Task GetUnreadCountsAsync(string userId, string[] channels)
            => client.EmitAsync("get_unread_counts", new { userId, channels });

        /// <summary>Mark all listed channels read for a user.</summary>
        public Task MarkAllReadAsync(string userId, string[] channels)
            => client.EmitAsync("mark_all_read", new { userId, channels });

        // ───────────────────────── Presence / status ─────────────────────

        /// <summary>Set the user's presence status (online/away/dnd/offline).</summary>
        public Task SetStatusAsync(string userId, string status)
            => client.EmitAsync("set_status", new { userId, status });

        /// <summary>Set a custom status with an emoji and text.</summary>
        public Task SetCustomStatusAsync(string userId, string emoji, string text, string expiresAt = null)
            => client.EmitAsync("set_custom_status", new { userId, emoji, text, expiresAt });

        /// <summary>Clear the user's custom status.</summary>
        public Task ClearCustomStatusAsync(string userId)
            => client.EmitAsync("clear_custom_status", new { userId });

        /// <summary>Enable Do Not Disturb, optionally until a time.</summary>
        public Task SetDndAsync(string userId, string until = null)
            => client.EmitAsync("set_dnd", new { userId, until });

        /// <summary>Disable Do Not Disturb.</summary>
        public Task ClearDndAsync(string userId)
            => client.EmitAsync("clear_dnd", new { userId });

        /// <summary>Broadcast a typing indicator to a channel (OnUserTyping).</summary>
        public Task StartTypingAsync(string userId, string channel)
            => client.EmitAsync("start_typing", new { userId, channel });

        /// <summary>Broadcast that the user stopped typing (OnUserStoppedTyping).</summary>
        public Task StopTypingAsync(string userId, string channel)
            => client.EmitAsync("stop_typing", new { userId, channel });

        /// <summary>Request presence for a set of users (see OnUserPresenceData).</summary>
        public Task GetUserPresenceAsync(string[] userIds)
            => client.EmitAsync("get_user_presence", new { userIds });

        // ─────────────────────────── File uploads ────────────────────────

        /// <summary>Announce the start of a file upload (returns an uploadId via OnFileUploadStarted).</summary>
        public Task StartFileUploadAsync(string fileName, long fileSize, string mimeType,
            string channel, string userId, string userName = null)
            => client.EmitAsync("start_file_upload", new
            {
                fileName, fileSize, mimeType, channel, userId, userName
            });

        /// <summary>Report upload progress.</summary>
        public Task UploadProgressAsync(string uploadId, long bytesUploaded, string channel = null)
            => client.EmitAsync("upload_progress", new { uploadId, bytesUploaded, channel });

        /// <summary>Mark an upload complete with its stored file info.</summary>
        public Task UploadCompleteAsync(string uploadId, string fileId, object storageInfo,
            string channel = null, string messageId = null)
            => client.EmitAsync("upload_complete", new
            {
                uploadId, fileId, storageInfo, channel, messageId
            });

        /// <summary>Mark an upload as failed.</summary>
        public Task UploadFailedAsync(string uploadId, string errorCode, string errorMessage, string channel = null)
            => client.EmitAsync("upload_failed", new { uploadId, errorCode, errorMessage, channel });

        /// <summary>Cancel an in-progress upload.</summary>
        public Task CancelUploadAsync(string uploadId, string channel = null)
            => client.EmitAsync("cancel_upload", new { uploadId, channel });

        /// <summary>Request the status of an upload (see OnUploadStatus).</summary>
        public Task GetUploadStatusAsync(string uploadId)
            => client.EmitAsync("get_upload_status", new { uploadId });

        /// <summary>List files uploaded to a channel (see OnChannelFiles).</summary>
        public Task GetChannelFilesAsync(string channel, int? limit = null)
            => client.EmitAsync("get_channel_files", new { channel, limit });

        // ────────────────────────── Direct messages ──────────────────────

        /// <summary>Create or fetch a DM conversation between users.</summary>
        public Task CreateDmAsync(string[] userIds, string type = null, string groupName = null)
            => client.EmitAsync("create_dm", new { userIds, type, groupName });

        /// <summary>Send a message into a DM conversation (delivered via OnDmReceived).</summary>
        public Task SendDmAsync(string conversationId, string message, string userId, string userName = null)
            => client.EmitAsync("send_dm", new { conversationId, message, userId, userName });

        /// <summary>List a user's DM conversations (see OnDmConversations).</summary>
        public Task GetDmConversationsAsync(string userId, bool includeArchived = false)
            => client.EmitAsync("get_dm_conversations", new { userId, includeArchived });

        /// <summary>Fetch a DM conversation's history (see OnDmHistory).</summary>
        public Task GetDmHistoryAsync(string conversationId, int count = 50)
            => client.EmitAsync("get_dm_history", new { conversationId, count });

        /// <summary>Mute a DM conversation.</summary>
        public Task MuteDmAsync(string conversationId, string userId)
            => client.EmitAsync("mute_dm", new { conversationId, userId });

        /// <summary>Archive a DM conversation.</summary>
        public Task ArchiveDmAsync(string conversationId, string userId)
            => client.EmitAsync("archive_dm", new { conversationId, userId });

        // ─────────────────────────── Notifications ───────────────────────

        /// <summary>Subscribe to the user's notifications (see OnNotificationsSubscribed).</summary>
        public Task SubscribeNotificationsAsync(string userId)
            => client.EmitAsync("subscribe_notifications", new { userId });

        /// <summary>Mark a single notification read.</summary>
        public Task MarkNotificationReadAsync(string notificationId, string userId)
            => client.EmitAsync("mark_notification_read", new { notificationId, userId });

        /// <summary>Mark all of the user's notifications read.</summary>
        public Task MarkAllNotificationsReadAsync(string userId)
            => client.EmitAsync("mark_all_notifications_read", new { userId });

        /// <summary>Clear all of the user's notifications.</summary>
        public Task ClearNotificationsAsync(string userId)
            => client.EmitAsync("clear_notifications", new { userId });

        /// <summary>Fetch the user's notifications (see OnNotificationsData).</summary>
        public Task GetNotificationsAsync(string userId, int? limit = null, string status = null)
            => client.EmitAsync("get_notifications", new { userId, limit, status });

        /// <summary>Fetch the user's unread notification count (see OnUnreadCount).</summary>
        public Task GetUnreadCountAsync(string userId)
            => client.EmitAsync("get_unread_count", new { userId });

        // ───────────────────────── Channel management ────────────────────

        /// <summary>Create a channel (creator becomes owner; members are notified).</summary>
        public Task CreateChannelAsync(string name, string createdBy, string createdByName = null,
            string type = null, string description = null, string topic = null, object[] members = null)
            => client.EmitAsync("create_channel", new
            {
                name, type, description, topic, createdBy, createdByName,
                members = members ?? Array.Empty<object>()
            });

        /// <summary>Update a channel's details (owner/admin only).</summary>
        public Task UpdateChannelAsync(string channelId, object updates, string userId)
            => client.EmitAsync("update_channel", new { channelId, updates, userId });

        /// <summary>Archive a channel (owner only).</summary>
        public Task ArchiveChannelAsync(string channelId, string userId)
            => client.EmitAsync("archive_channel", new { channelId, userId });

        /// <summary>Invite a user to a channel (owner/admin only).</summary>
        public Task InviteToChannelAsync(string channelId, string invitedUserId,
            string invitedUserName, string invitedBy)
            => client.EmitAsync("invite_to_channel", new
            {
                channelId, invitedUserId, invitedUserName, invitedBy
            });

        /// <summary>Remove a user from a channel (owner/admin only).</summary>
        public Task RemoveFromChannelAsync(string channelId, string removedUserId, string removedBy)
            => client.EmitAsync("remove_from_channel", new { channelId, removedUserId, removedBy });

        /// <summary>Join a public channel.</summary>
        public Task JoinChannelAsync(string channelId, string userId, string userName = null)
            => client.EmitAsync("join_channel", new { channelId, userId, userName });

        /// <summary>Leave a channel.</summary>
        public Task LeaveChannelAsync(string channelId, string userId)
            => client.EmitAsync("leave_channel", new { channelId, userId });

        /// <summary>List a channel's members (see OnChannelMembers).</summary>
        public Task GetChannelMembersAsync(string channelId)
            => client.EmitAsync("get_channel_members", new { channelId });
    }
}
