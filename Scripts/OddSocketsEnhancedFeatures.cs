using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OddSockets
{
    /// <summary>
    /// Enhanced Features for OddSockets Unity SDK
    /// Provides 67 new Slack-like events with C# async/await and Unity coroutines
    /// </summary>
    public class OddSocketsEnhancedFeatures
    {
        private readonly OddSocketsClient client;
        private readonly int timeout = 10000;

        public OddSocketsEnhancedFeatures(OddSocketsClient client)
        {
            this.client = client;
        }

        // ==================== THREAD EVENTS ====================

        public async Task<string> ThreadReply(string channel, string parentMessageId, string message, string userId, string userName)
        {
            var data = new Dictionary<string, object>
            {
                { "channel", channel },
                { "parentMessageId", parentMessageId },
                { "message", message },
                { "userId", userId },
                { "userName", userName }
            };
            return await EmitWithResponse("thread_reply", data, "thread_reply_success");
        }

        public async Task<string> GetThread(string threadId)
        {
            return await EmitWithResponse("get_thread", new Dictionary<string, object> { { "threadId", threadId } }, "thread_data");
        }

        public async Task<string> SubscribeThread(string threadId, string userId)
        {
            var data = new Dictionary<string, object> { { "threadId", threadId }, { "userId", userId } };
            return await EmitWithResponse("subscribe_thread", data, "thread_subscribed");
        }

        public void MarkThreadRead(string threadId, string userId)
        {
            client.Emit("mark_thread_read", new Dictionary<string, object> { { "threadId", threadId }, { "userId", userId } });
        }

        public void FollowThread(string threadId, string userId)
        {
            client.Emit("follow_thread", new Dictionary<string, object> { { "threadId", threadId }, { "userId", userId } });
        }

        public void UnfollowThread(string threadId, string userId)
        {
            client.Emit("unfollow_thread", new Dictionary<string, object> { { "threadId", threadId }, { "userId", userId } });
        }

        // ==================== REACTION EVENTS ====================

        public void AddReaction(string messageId, string channel, string emoji, string userId, string userName)
        {
            var data = new Dictionary<string, object>
            {
                { "messageId", messageId },
                { "channel", channel },
                { "emoji", emoji },
                { "userId", userId },
                { "userName", userName }
            };
            client.Emit("add_reaction", data);
        }

        public void RemoveReaction(string messageId, string channel, string emoji, string userId)
        {
            var data = new Dictionary<string, object>
            {
                { "messageId", messageId },
                { "channel", channel },
                { "emoji", emoji },
                { "userId", userId }
            };
            client.Emit("remove_reaction", data);
        }

        public async Task<string> GetReactions(string messageId)
        {
            return await EmitWithResponse("get_reactions", new Dictionary<string, object> { { "messageId", messageId } }, "message_reactions");
        }

        // ==================== READ RECEIPT EVENTS ====================

        public void MarkRead(string messageId, string channel, string userId, string userName)
        {
            var data = new Dictionary<string, object>
            {
                { "messageId", messageId },
                { "channel", channel },
                { "userId", userId },
                { "userName", userName }
            };
            client.Emit("mark_read", data);
        }

        public async Task<string> GetUnreadCounts(string userId, List<string> channels)
        {
            var data = new Dictionary<string, object> { { "userId", userId }, { "channels", channels } };
            return await EmitWithResponse("get_unread_counts", data, "unread_counts");
        }

        public void MarkAllRead(string channel, string userId)
        {
            client.Emit("mark_all_read", new Dictionary<string, object> { { "channel", channel }, { "userId", userId } });
        }

        // ==================== CHANNEL EVENTS ====================

        public async Task<string> CreateChannel(string name, string type, string description, string topic, string createdBy, string createdByName)
        {
            var data = new Dictionary<string, object>
            {
                { "name", name },
                { "type", type },
                { "description", description },
                { "topic", topic },
                { "createdBy", createdBy },
                { "createdByName", createdByName },
                { "members", new List<string>() }
            };
            return await EmitWithResponse("create_channel", data, "channel_create_success");
        }

        public void UpdateChannel(string channelId, Dictionary<string, object> updates, string userId)
        {
            var data = new Dictionary<string, object>
            {
                { "channelId", channelId },
                { "updates", updates },
                { "userId", userId }
            };
            client.Emit("update_channel", data);
        }

        public void ArchiveChannel(string channelId, string userId)
        {
            client.Emit("archive_channel", new Dictionary<string, object> { { "channelId", channelId }, { "userId", userId } });
        }

        public void InviteToChannel(string channelId, string invitedUserId, string invitedUserName, string invitedBy)
        {
            var data = new Dictionary<string, object>
            {
                { "channelId", channelId },
                { "invitedUserId", invitedUserId },
                { "invitedUserName", invitedUserName },
                { "invitedBy", invitedBy }
            };
            client.Emit("invite_to_channel", data);
        }

        public void RemoveFromChannel(string channelId, string removedUserId, string removedBy)
        {
            var data = new Dictionary<string, object>
            {
                { "channelId", channelId },
                { "removedUserId", removedUserId },
                { "removedBy", removedBy }
            };
            client.Emit("remove_from_channel", data);
        }

        public void JoinChannel(string channelId, string userId, string userName)
        {
            var data = new Dictionary<string, object>
            {
                { "channelId", channelId },
                { "userId", userId },
                { "userName", userName }
            };
            client.Emit("join_channel", data);
        }

        public void LeaveChannel(string channelId, string userId)
        {
            client.Emit("leave_channel", new Dictionary<string, object> { { "channelId", channelId }, { "userId", userId } });
        }

        public async Task<string> GetChannelMembers(string channelId)
        {
            return await EmitWithResponse("get_channel_members", new Dictionary<string, object> { { "channelId", channelId } }, "channel_members");
        }

        // ==================== DIRECT MESSAGE EVENTS ====================

        public async Task<string> CreateDM(List<string> userIds, string type)
        {
            var data = new Dictionary<string, object> { { "userIds", userIds }, { "type", type } };
            return await EmitWithResponse("create_dm", data, "dm_create_success");
        }

        public void SendDM(string conversationId, string message, string userId, string userName)
        {
            var data = new Dictionary<string, object>
            {
                { "conversationId", conversationId },
                { "message", message },
                { "userId", userId },
                { "userName", userName }
            };
            client.Emit("send_dm", data);
        }

        public async Task<string> GetDMConversations(string userId, bool includeArchived)
        {
            var data = new Dictionary<string, object> { { "userId", userId }, { "includeArchived", includeArchived } };
            return await EmitWithResponse("get_dm_conversations", data, "dm_conversations");
        }

        // ==================== NOTIFICATION EVENTS ====================

        public void SubscribeNotifications(string userId)
        {
            client.Emit("subscribe_notifications", new Dictionary<string, object> { { "userId", userId } });
        }

        public void MarkNotificationRead(string notificationId, string userId)
        {
            client.Emit("mark_notification_read", new Dictionary<string, object> { { "notificationId", notificationId }, { "userId", userId } });
        }

        public void MarkAllNotificationsRead(string userId)
        {
            client.Emit("mark_all_notifications_read", new Dictionary<string, object> { { "userId", userId } });
        }

        public void ClearNotifications(string userId)
        {
            client.Emit("clear_notifications", new Dictionary<string, object> { { "userId", userId } });
        }

        public async Task<string> GetNotifications(string userId, int limit, string status = "all")
        {
            var data = new Dictionary<string, object> { { "userId", userId }, { "limit", limit }, { "status", status } };
            return await EmitWithResponse("get_notifications", data, "notifications_data");
        }

        // ==================== PRESENCE EVENTS ====================

        public void SetStatus(string userId, string status)
        {
            client.Emit("set_status", new Dictionary<string, object> { { "userId", userId }, { "status", status } });
        }

        public void SetCustomStatus(string userId, string emoji, string text, string expiresAt = null)
        {
            var data = new Dictionary<string, object> { { "userId", userId }, { "emoji", emoji }, { "text", text } };
            if (expiresAt != null) data["expiresAt"] = expiresAt;
            client.Emit("set_custom_status", data);
        }

        public void ClearCustomStatus(string userId)
        {
            client.Emit("clear_custom_status", new Dictionary<string, object> { { "userId", userId } });
        }

        public void SetDND(string userId, string until = null)
        {
            var data = new Dictionary<string, object> { { "userId", userId } };
            if (until != null) data["until"] = until;
            client.Emit("set_dnd", data);
        }

        public void ClearDND(string userId)
        {
            client.Emit("clear_dnd", new Dictionary<string, object> { { "userId", userId } });
        }

        public void StartTyping(string userId, string channel)
        {
            client.Emit("start_typing", new Dictionary<string, object> { { "userId", userId }, { "channel", channel } });
        }

        public void StopTyping(string userId, string channel)
        {
            client.Emit("stop_typing", new Dictionary<string, object> { { "userId", userId }, { "channel", channel } });
        }

        public async Task<string> GetUserPresence(List<string> userIds)
        {
            return await EmitWithResponse("get_user_presence", new Dictionary<string, object> { { "userIds", userIds } }, "user_presence_data");
        }

        // ==================== MESSAGE EDITING EVENTS ====================

        public void EditMessage(string messageId, string channel, string newContent, string userId)
        {
            var data = new Dictionary<string, object>
            {
                { "messageId", messageId },
                { "channel", channel },
                { "newContent", newContent },
                { "userId", userId }
            };
            client.Emit("edit_message", data);
        }

        public void DeleteMessage(string messageId, string channel, string userId)
        {
            var data = new Dictionary<string, object>
            {
                { "messageId", messageId },
                { "channel", channel },
                { "userId", userId }
            };
            client.Emit("delete_message", data);
        }

        public void PinMessage(string messageId, string channel, string userId)
        {
            var data = new Dictionary<string, object>
            {
                { "messageId", messageId },
                { "channel", channel },
                { "userId", userId }
            };
            client.Emit("pin_message", data);
        }

        public void UnpinMessage(string messageId, string channel, string userId)
        {
            var data = new Dictionary<string, object>
            {
                { "messageId", messageId },
                { "channel", channel },
                { "userId", userId }
            };
            client.Emit("unpin_message", data);
        }

        public async Task<string> GetPinnedMessages(string channel)
        {
            return await EmitWithResponse("get_pinned_messages", new Dictionary<string, object> { { "channel", channel } }, "pinned_messages");
        }

        // ==================== SEARCH EVENTS ====================

        public async Task<string> SearchMessages(string query, string userId, int limit)
        {
            var data = new Dictionary<string, object> { { "query", query }, { "userId", userId }, { "limit", limit } };
            return await EmitWithResponse("search_messages", data, "search_results");
        }

        public async Task<string> FilterMessages(Dictionary<string, object> filters)
        {
            return await EmitWithResponse("filter_messages", filters, "filter_results");
        }

        public async Task<string> SearchInChannel(string channel, string query, int limit)
        {
            var data = new Dictionary<string, object> { { "channel", channel }, { "query", query }, { "limit", limit } };
            return await EmitWithResponse("search_in_channel", data, "channel_search_results");
        }

        public async Task<string> SearchByUser(string userId, string query, int limit)
        {
            var data = new Dictionary<string, object> { { "userId", userId }, { "limit", limit } };
            if (!string.IsNullOrEmpty(query)) data["query"] = query;
            return await EmitWithResponse("search_by_user", data, "user_search_results");
        }

        // ==================== PRIVATE METHODS ====================

        private async Task<string> EmitWithResponse(string eventName, Dictionary<string, object> data, string responseEvent)
        {
            var tcs = new TaskCompletionSource<string>();
            var timeoutTask = Task.Delay(timeout);

            Action<string> handler = null;
            handler = (response) =>
            {
                client.Off(responseEvent, handler);
                tcs.TrySetResult(response);
            };

            client.On(responseEvent, handler);
            client.Emit(eventName, data);

            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                client.Off(responseEvent, handler);
                throw new TimeoutException($"Timeout waiting for {responseEvent}");
            }

            return await tcs.Task;
        }
    }
}
