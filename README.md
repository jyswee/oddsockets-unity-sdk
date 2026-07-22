# OddSockets Realtime for Unity

Realtime messaging for Unity: pub/sub channels, presence, message history, and a
full enhanced-feature surface (reactions, threads, typing indicators, read
receipts, direct messages, notifications, channel management), backed by the
OddSockets managed worker fleet.

OddSockets - A division of Tyga.Cloud Ltd

## Requirements

- Unity 2021.3 or newer
- [Newtonsoft Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) (`com.unity.nuget.newtonsoft-json`) - resolved automatically as a package dependency

The Socket.IO transport ([SocketIOUnity](https://github.com/itisnajim/SocketIOUnity),
MIT) is bundled with this package under `ThirdParty/SocketIOUnity` - nothing extra
to install.

## Install

### Asset Store

Import the package from the Asset Store. Newtonsoft Json is pulled in automatically
as a dependency; the Socket.IO transport is bundled. No further setup needed.

### Package Manager (git URL)

In `Window > Package Manager > + > Add package from git URL`:
```
https://github.com/jyswee/oddsockets-unity-sdk.git
```

## Quick Start

```csharp
using OddSockets.Unity;

// Attach OddSocketsClient to a GameObject
var client = gameObject.AddComponent<OddSocketsClient>();
client.Initialize(new OddSocketsUnityConfig { ApiKey = "YOUR_API_KEY", UserId = "my-agent" });
await client.ConnectAsync();

var channel = client.Channel("my-channel");
await channel.SubscribeAsync(msg => Debug.Log($"Received: {msg.Message}"));
await channel.PublishAsync(new { text = "Hello from Unity" });
```

Call the async methods from an `async` context (for example an `async void Start`).

## Enhanced Features

Everything beyond core pub/sub is on `client.Enhanced`. Wire the event you care
about, then call the matching request method. Payloads arrive as Newtonsoft
`JToken` so you can read any shape the worker sends.

```csharp
// Reactions
client.Enhanced.OnReactionAdded += p => Debug.Log($"reaction: {p["emoji"]}");
await client.Enhanced.AddReactionAsync("msg-1", "my-channel", ":thumbsup:", "my-agent");

// Threads
client.Enhanced.OnThreadReply += p => Debug.Log($"reply in {p["channel"]}");
await client.Enhanced.ThreadReplyAsync("my-channel", "parent-1", "nice!", "my-agent");

// Typing indicators
client.Enhanced.OnUserTyping += p => Debug.Log($"{p["userId"]} is typing");
await client.Enhanced.StartTypingAsync("my-agent", "my-channel");
```

The full enhanced surface (all backed by the worker):

| Area | Requests | Events |
|---|---|---|
| Reactions | `AddReactionAsync`, `RemoveReactionAsync`, `GetReactionsAsync` | `OnReactionAdded`, `OnReactionRemoved`, `OnReactionsData` |
| Threads | `ThreadReplyAsync`, `GetThreadAsync`, `SubscribeThreadAsync`, `MarkThreadReadAsync`, `FollowThreadAsync`, `UnfollowThreadAsync` | `OnThreadReply`, `OnThreadData`, `OnThreadSubscribed`, `OnThreadFollowed`, `OnThreadUnfollowed`, `OnThreadReadUpdated` |
| Message editing | `EditMessageAsync`, `DeleteMessageAsync`, `PinMessageAsync`, `UnpinMessageAsync`, `GetPinnedMessagesAsync` | `OnMessageEdited`, `OnMessageDeleted`, `OnMessagePinned`, `OnMessageUnpinned` |
| Read receipts | `MarkReadAsync`, `GetUnreadCountsAsync`, `MarkAllReadAsync` | `OnUserRead`, `OnUnreadCountUpdated`, `OnUnreadCounts`, `OnAllMarkedRead` |
| Presence & status | `SetStatusAsync`, `SetCustomStatusAsync`, `ClearCustomStatusAsync`, `SetDndAsync`, `ClearDndAsync`, `StartTypingAsync`, `StopTypingAsync`, `GetUserPresenceAsync` | `OnUserStatusChanged`, `OnCustomStatusUpdated`, `OnCustomStatusCleared`, `OnDndStatusChanged`, `OnUserTyping`, `OnUserStoppedTyping`, `OnUserPresenceData` |
| File uploads | `StartFileUploadAsync`, `UploadProgressAsync`, `UploadCompleteAsync`, `UploadFailedAsync`, `CancelUploadAsync`, `GetUploadStatusAsync`, `GetChannelFilesAsync` | `OnFileUploadStarted`, `OnUploadProgress`, `OnUploadCompleted`, `OnUploadFailed`, `OnUploadStatus`, `OnUploadCancelled`, `OnChannelFiles` |
| Direct messages | `CreateDmAsync`, `SendDmAsync`, `GetDmConversationsAsync`, `GetDmHistoryAsync`, `MuteDmAsync`, `ArchiveDmAsync` | `OnDmCreated`, `OnDmReceived`, `OnDmConversations`, `OnDmHistory`, `OnDmMuted`, `OnDmArchived` |
| Notifications | `SubscribeNotificationsAsync`, `MarkNotificationReadAsync`, `MarkAllNotificationsReadAsync`, `ClearNotificationsAsync`, `GetNotificationsAsync`, `GetUnreadCountAsync` | `OnNotification`, `OnNotificationsSubscribed`, `OnNotificationRead`, `OnAllNotificationsRead`, `OnNotificationsCleared`, `OnNotificationsData`, `OnUnreadCount` |
| Channel management | `CreateChannelAsync`, `UpdateChannelAsync`, `ArchiveChannelAsync`, `InviteToChannelAsync`, `RemoveFromChannelAsync`, `JoinChannelAsync`, `LeaveChannelAsync`, `GetChannelMembersAsync` | `OnChannelCreated`, `OnChannelUpdated`, `OnChannelArchived`, `OnUserInvited`, `OnUserJoinedChannel`, `OnUserLeftChannel`, `OnUserRemoved`, `OnChannelMembers` |

For any worker event not wrapped above, use the raw API: `client.On("event_name", p => ...)` and `client.EmitAsync("event_name", payload)`.

## Samples

Import from `Window > Package Manager > OddSockets Realtime > Samples`:

- **Basic Usage** - connect, subscribe, publish.
- **Two-Client Round Trip** - two independent clients proving an end-to-end round trip through the worker.

## Get a Free API Key

```bash
curl -X POST https://oddsockets.com/api/agent-signup \
  -H "Content-Type: application/json" \
  -d '{"email": "you@example.com", "agentName": "my-agent", "platform": "unity"}'
curl -X POST https://oddsockets.com/api/agent-signup/verify \
  -H "Content-Type: application/json" \
  -d '{"email": "you@example.com", "code": "123456", "agentName": "my-agent"}'
```

## Plans

| | Free | Starter | Pro |
|---|---|---|---|
| **Price** | $0/mo | $49.99/mo | $299/mo |
| **MAU** | 100 | 1,000 | 50,000 |
| **Concurrent connections** | 50 | 1,000 | Unlimited |
| **Messages/day** | 10,000 | 4,320,000 | Unlimited |
| **Channels** | 10 | Unlimited | Unlimited |
| **Storage** | 100MB (24h) | 50GB (6 months) | Unlimited |

## Support

- [Documentation](https://docs.oddsockets.com/sdks/unity)
- [Issue Tracker](https://github.com/jyswee/oddsockets-unity-sdk/issues)
- [Email Support](mailto:support@oddsockets.com)

## License

MIT License - Copyright (c) 2026 Joe Wee, Tyga.Cloud Ltd. See [LICENSE.md](LICENSE.md) for details.
Third-party components are listed in [Third Party Notices.md](Third%20Party%20Notices.md).
