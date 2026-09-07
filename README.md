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

## Manager URL

The manager URL is resolved in this order:

1. `OddSocketsUnityConfig.ManagerUrl` (also editable in the inspector)
2. the `ODDSOCKETS_MANAGER_URL` environment variable
3. `https://connect.oddsockets.tyga.network`

It must be an absolute `http://` or `https://` URL, otherwise `Initialize` throws
`ArgumentException` with the message `Invalid managerUrl: <value>`. Point it at a
self-hosted or staging manager and the SDK will use that endpoint and nothing else: if it
is unreachable the connection fails with the underlying error rather than falling back to
the public endpoint.

```csharp
client.Initialize(new OddSocketsUnityConfig
{
    ApiKey = "YOUR_API_KEY",
    ManagerUrl = "https://manager.internal.example.com"
});
```

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

## Token auth for shipped builds

Shipped game clients must never embed the API key. Set a `TokenProvider` delegate
instead: your game backend signs a player JWT, and the client exchanges it for a
short-lived, channel-scoped OddSockets token at
`POST https://connect.oddsockets.tyga.network/v1/token`. The SDK silently refreshes
the token before expiry and re-mints it on every reconnect.

```csharp
client.Initialize(new OddSocketsUnityConfig
{
    UserId = "player-1",
    AutoConnect = false,
    TokenProvider = async () =>
    {
        // POST /v1/token with Authorization: Bearer <playerJwt>
        // body: { "channels": ["lobby"] }   → { "token": "...", "expiresAt": "..." }
        var json = await MyHttp.PostAsync(
            "https://connect.oddsockets.tyga.network/v1/token", playerJwt,
            "{\"channels\":[\"lobby\"]}");
        return JsonUtility.FromJson<OddSocketsToken>(json);
    }
});
```

Token minting requires a one-time app registration (your token issuer +
verification material) — contact us via [oddsockets.com](https://oddsockets.com).

## Get an API Key

No free tier — every plan starts with a 7-day free trial (nothing is charged
during the trial). Signup issues a working API key instantly; the key runs
keyless for 48 hours, and adding a card within that window extends it through
the trial.

Scriptable signup (CLI):

```bash
npm i -g oddsockets-cli
oddsockets plans                                  # list live plan ids
oddsockets signup you@studio.com --plan oddsockets-starter
oddsockets publish smoke-test '{"hello":"world"}' # verify in one line
```

AI agents can also self-provision via MCP: connect to
`https://mcp.oddsockets.ai/sse` and call `oddsockets_signup`.

## Plans

`oddsockets-starter` $29/mo · `oddsockets-pro` $99/mo · `oddsockets-scale` $299/mo · `oddsockets-enterprise` (contact us)

See [oddsockets.com/pricing](https://oddsockets.com/pricing) for current limits per tier.

## Get Accredited

<a href="https://tyga.games/accreditation"><img src="https://prodmedia.tyga.host/public/tyga.cloud/landing/tyga.games/tygagames-black-words.svg" alt="tyga.games accreditation" height="44"></a>

Prove you can build and operate real-time features on OddSockets — channels, presence, pub/sub, delivery guarantees and production liveops — on the stack itself. Three tiers (**TCU / TCA / TCP**), certified through **tyga.games** and delivered on ClassaaS.

[**Get accredited on tyga.games →**](https://tyga.games/accreditation)

## Support

- [Documentation](https://oddsockets.com/docs)
- [Issue Tracker](https://github.com/jyswee/oddsockets-unity-sdk/issues)
- [Email Support](mailto:support@oddsockets.com)

## License

MIT License - Copyright (c) 2026 Joe Wee, Tyga.Cloud Ltd. See [LICENSE.md](LICENSE.md) for details.
Third-party components are listed in [Third Party Notices.md](Third%20Party%20Notices.md).
