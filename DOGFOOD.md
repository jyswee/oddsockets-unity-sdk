# OddSockets Unity SDK - Dogfood Backlog (unity-demo-game)

Internal collaboration doc (not shipped in the Asset Store package - exclude on upload).

The unity-demo-game team is building a **multiplayer demo game** to dogfood the
OddSockets Unity SDK. This is the complete list of SDK capabilities we need
exercised, each mapped to a concrete in-game mechanic so it lands naturally in a
real game rather than a synthetic test harness.

Contract: `ctr_9c0f71e8c0f46c923b541595dc18339a` (outbound, we file features in
their project). **Pending their acceptance** - file the items below once active.

Every feature must be proven the honest way: **two independent clients** (two
players / two SDK instances), so a received event provably travelled through the
worker, not a local echo. Report SDK version, Unity version, platform, and any
gap/bug back on the contract.

---

## Coverage map (SDK capability -> game mechanic)

| # | SDK area | Game mechanic to build | Priority |
|---|---|---|---|
| 1 | Core connect + channel pub/sub | Realtime state sync + lobby channel | P1 |
| 2 | Presence + channel state | Lobby roster / ready-up / team assignment | P1 |
| 3 | Message history | Match log + chat scrollback on join | P1 |
| 4 | Channel management | Parties / rooms / guild lobbies | P1 |
| 5 | Reconnect + hardening | Survive drop mid-match; size limits; load | P1 |
| 6 | Reactions | Quick emotes on messages / events | P2 |
| 7 | Typing indicators | "Player is typing" in team chat | P2 |
| 8 | Read receipts / unread | Chat read state + unread badges | P2 |
| 9 | Direct messages | Private whispers + party invites | P2 |
| 10 | Notifications | Kill feed / achievements / invites | P2 |
| 11 | Message editing + pins | Edit/delete chat; pinned announcements | P3 |
| 12 | File uploads | Share screenshots / replays / loadouts | P3 |
| 13 | Raw event API | Custom game events via On/EmitAsync | P3 |

---

## Ready-to-file feature items

Each block is one `bgz` command. Fire on contract acceptance.

### 1. Core realtime sync + lobby (P1)
Client: `Initialize`, `ConnectAsync`, `Channel`, `PublishAsync`, `PublishBulkAsync`, `Disconnect`, `State`. Channel: `SubscribeAsync`, `UnsubscribeAsync`, events `OnMessage`/`OnSubscribed`/`OnPublished`.
Mechanic: every player joins a shared lobby channel; broadcast position/state each tick; `PublishBulkAsync` for batched state. Two clients must see each other's messages.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Realtime sync + lobby channel (core pub/sub)" -p P1 -d "Use OddSocketsClient.Initialize/ConnectAsync, Channel(name).SubscribeAsync + PublishAsync (and PublishBulkAsync for batched ticks) to sync player state across two clients in a shared lobby. Verify OnMessage/OnSubscribed/OnPublished fire and State transitions Connected. Two independent players must see each other. Report latency + any dropped messages."
```

### 2. Presence + channel state (P1)
Channel: `GetPresenceAsync`, `UpdateStateAsync`, `SubscribeAsync(EnablePresence=true)`, events `OnPresence`/`OnPresenceChange`.
Mechanic: lobby roster showing who is online; ready-up flag + team via `UpdateStateAsync`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Presence roster + ready-up (channel state)" -p P1 -d "Subscribe with SubscriptionOptions.EnablePresence=true. Show a live lobby roster via OnPresence/OnPresenceChange and GetPresenceAsync. Use UpdateStateAsync to broadcast per-player state (ready flag, team, character). Verify a second client sees the first join/leave and state changes."
```

### 3. Message history (P1)
Channel: `GetHistoryAsync(HistoryOptions.Count)`, `SubscribeAsync(MaxHistory, RetainHistory)`, event `OnHistory`.
Mechanic: late-joiner sees recent match events + chat scrollback.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Match log + chat scrollback (history)" -p P1 -d "On join, hydrate recent events via GetHistoryAsync and SubscribeAsync with MaxHistory/RetainHistory; render via OnHistory. Verify a client that connects AFTER messages were published still receives them (cross-connection history, not local)."
```

### 4. Channel management (P1)
Enhanced: `CreateChannelAsync`, `UpdateChannelAsync`, `ArchiveChannelAsync`, `InviteToChannelAsync`, `RemoveFromChannelAsync`, `JoinChannelAsync`, `LeaveChannelAsync`, `GetChannelMembersAsync`. Events: `OnChannelCreated`/`OnChannelUpdated`/`OnChannelArchived`/`OnUserInvited`/`OnUserJoinedChannel`/`OnUserLeftChannel`/`OnUserRemoved`/`OnChannelMembers`.
Mechanic: parties / rooms / guild lobbies with invites and membership.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Parties/rooms lifecycle (channel management)" -p P1 -d "Build party/room lobbies on client.Enhanced: CreateChannelAsync, InviteToChannelAsync, JoinChannelAsync, LeaveChannelAsync, RemoveFromChannelAsync, GetChannelMembersAsync, UpdateChannelAsync, ArchiveChannelAsync. Wire OnChannelCreated/Updated/Archived/UserInvited/UserJoinedChannel/UserLeftChannel/UserRemoved/ChannelMembers. Two clients: host creates + invites, guest joins, roster updates on both."
```

### 5. Reconnect + hardening (P1)
Client auto-reconnect + `State`; message size validation; concurrency.
Mechanic: player survives a network drop mid-match and resumes; stress with many messages.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Reconnect survival + size/load hardening" -p P1 -d "Force a disconnect mid-session (airplane mode / kill socket); verify auto-reconnect restores subscriptions and enhanced listeners without app restart. Test message size limits (MessageSizeValidator) and sustained publish load (e.g. 20 msg/s across 4+ clients). Report reconnect time, any lost subscriptions, and the max payload accepted."
```

### 6. Reactions / emotes (P2)
Enhanced: `AddReactionAsync`, `RemoveReactionAsync`, `GetReactionsAsync`. Events: `OnReactionAdded`/`OnReactionRemoved`/`OnReactionsData`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Emotes/reactions on events" -p P2 -d "Add quick emotes over a message/event with AddReactionAsync/RemoveReactionAsync/GetReactionsAsync; render live counts via OnReactionAdded/OnReactionRemoved/OnReactionsData. Two clients: player A reacts, player B sees it."
```

### 7. Typing indicators (P2)
Enhanced: `StartTypingAsync`, `StopTypingAsync`. Events: `OnUserTyping`/`OnUserStoppedTyping`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Typing indicator in team chat" -p P2 -d "Show 'player is typing' in chat using StartTypingAsync/StopTypingAsync and OnUserTyping/OnUserStoppedTyping. Verify the other client sees the indicator appear and clear."
```

### 8. Read receipts / unread (P2)
Enhanced: `MarkReadAsync`, `MarkAllReadAsync`, `GetUnreadCountsAsync`. Events: `OnUserRead`/`OnUnreadCountUpdated`/`OnUnreadCounts`/`OnAllMarkedRead`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Chat read receipts + unread badges" -p P2 -d "Track read state with MarkReadAsync/MarkAllReadAsync/GetUnreadCountsAsync; render unread badges + 'seen' via OnUserRead/OnUnreadCountUpdated/OnUnreadCounts/OnAllMarkedRead. Verify counts update across two clients."
```

### 9. Direct messages (P2)
Enhanced: `CreateDmAsync`, `SendDmAsync`, `GetDmConversationsAsync`, `GetDmHistoryAsync`, `MuteDmAsync`, `ArchiveDmAsync`. Events: `OnDmCreated`/`OnDmReceived`/`OnDmConversations`/`OnDmHistory`/`OnDmMuted`/`OnDmArchived`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Private whispers (direct messages)" -p P2 -d "Player-to-player whispers + party invites via CreateDmAsync/SendDmAsync/GetDmConversationsAsync/GetDmHistoryAsync/MuteDmAsync/ArchiveDmAsync. Wire OnDmCreated/OnDmReceived/OnDmConversations/OnDmHistory/OnDmMuted/OnDmArchived. Verify a DM from A reaches only B (not other players in the lobby)."
```

### 10. Notifications (P2)
Enhanced: `SubscribeNotificationsAsync`, `GetNotificationsAsync`, `MarkNotificationReadAsync`, `MarkAllNotificationsReadAsync`, `ClearNotificationsAsync`, `GetUnreadCountAsync`. Events: `OnNotification`/`OnNotificationsSubscribed`/`OnNotificationRead`/`OnAllNotificationsRead`/`OnNotificationsCleared`/`OnNotificationsData`/`OnUnreadCount`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Kill feed / achievements (notifications)" -p P2 -d "Drive kill feed, achievements and invites through SubscribeNotificationsAsync + GetNotificationsAsync/MarkNotificationReadAsync/MarkAllNotificationsReadAsync/ClearNotificationsAsync/GetUnreadCountAsync. Wire OnNotification/OnNotificationsSubscribed/OnNotificationRead/OnAllNotificationsRead/OnNotificationsCleared/OnNotificationsData/OnUnreadCount. Verify a server-side event notifies the target player with a live unread badge."
```

### 11. Message editing + pins (P3)
Enhanced: `EditMessageAsync`, `DeleteMessageAsync`, `PinMessageAsync`, `UnpinMessageAsync`, `GetPinnedMessagesAsync`. Events: `OnMessageEdited`/`OnMessageDeleted`/`OnMessagePinned`/`OnMessageUnpinned`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Edit/delete chat + pinned announcements" -p P3 -d "Edit/delete chat lines and pin server announcements via EditMessageAsync/DeleteMessageAsync/PinMessageAsync/UnpinMessageAsync/GetPinnedMessagesAsync. Wire OnMessageEdited/OnMessageDeleted/OnMessagePinned/OnMessageUnpinned. Verify edits/pins propagate to a second client."
```

### 12. File uploads (P3)
Enhanced: `StartFileUploadAsync`, `UploadProgressAsync`, `UploadCompleteAsync`, `UploadFailedAsync`, `CancelUploadAsync`, `GetUploadStatusAsync`, `GetChannelFilesAsync`. Events: `OnFileUploadStarted`/`OnUploadProgress`/`OnUploadCompleted`/`OnUploadFailed`/`OnUploadStatus`/`OnUploadCancelled`/`OnChannelFiles`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Share screenshots/replays (file uploads)" -p P3 -d "Share screenshots/loadouts via StartFileUploadAsync + UploadProgressAsync/UploadCompleteAsync/UploadFailedAsync/CancelUploadAsync/GetUploadStatusAsync/GetChannelFilesAsync. Wire the OnFileUpload*/OnUpload*/OnChannelFiles events. Verify a second client sees the shared file appear in the channel file list."
```

### 13. Threads (P3)
Enhanced: `ThreadReplyAsync`, `GetThreadAsync`, `SubscribeThreadAsync`, `MarkThreadReadAsync`, `FollowThreadAsync`, `UnfollowThreadAsync`. Events: `OnThreadReply`/`OnThreadData`/`OnThreadSubscribed`/`OnThreadFollowed`/`OnThreadUnfollowed`/`OnThreadReadUpdated`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Per-round sub-chats (threads)" -p P3 -d "Thread replies for per-round/topic sub-conversations via ThreadReplyAsync/GetThreadAsync/SubscribeThreadAsync/MarkThreadReadAsync/FollowThreadAsync/UnfollowThreadAsync. Wire OnThreadReply/OnThreadData/OnThreadSubscribed/OnThreadFollowed/OnThreadUnfollowed/OnThreadReadUpdated. Verify a reply reaches a second client following the thread."
```

### 14. Status / DND presence (P3)
Enhanced: `SetStatusAsync`, `SetCustomStatusAsync`, `ClearCustomStatusAsync`, `SetDndAsync`, `ClearDndAsync`, `GetUserPresenceAsync`. Events: `OnUserStatusChanged`/`OnCustomStatusUpdated`/`OnCustomStatusCleared`/`OnDndStatusChanged`/`OnUserPresenceData`.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Player status (online/away/DND/custom)" -p P3 -d "Player status chips: SetStatusAsync/SetCustomStatusAsync/ClearCustomStatusAsync/SetDndAsync/ClearDndAsync/GetUserPresenceAsync. Wire OnUserStatusChanged/OnCustomStatusUpdated/OnCustomStatusCleared/OnDndStatusChanged/OnUserPresenceData. Verify status changes show on a second client's roster."
```

### 15. Raw event API (P3)
Client: `On(event, handler)`, `EmitAsync(event, payload)`, `Off(event)`.
Mechanic: any custom game event not covered by the typed helpers.

```
bgz contract ctr_9c0f71e8c0f46c923b541595dc18339a file-feature "Custom game events (raw On/EmitAsync)" -p P3 -d "Prove the escape hatch: define a custom game event (e.g. 'objective_captured') and round-trip it with client.EmitAsync + client.On (JToken payload), Off to unbind. Verify two clients exchange the custom event through the worker."
```

---

## What to report back (per feature)

- Pass/fail with the two-client evidence (who sent, who received, matched id/nonce).
- SDK version, Unity version, target platform (Editor/iOS/Android/WebGL/Standalone).
- Any missing method, wrong payload shape, or event that never fired -> file as a **bug** on the contract with a minimal repro.
- Rough latency + any reconnect/After-drop issues.
