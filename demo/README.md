# OddSockets Unity SDK - Two-Client Round-Trip Demo

A runnable PlayMode demo that proves a real real-time round-trip against OddSockets
using **two independent clients**: **connect -> subscribe -> publish -> receive**.

Because the subscriber (`alice`) and the publisher (`bob`) are separate connections,
a message that reaches alice can only have travelled through the assigned OddSockets
worker - so this doubles as an honest end-to-end regression (no mocks, no local echo).
The SDK speaks genuine Socket.IO (Engine.IO v4) over a WebSocket to the worker; the
manager assigns each client a worker transparently.

Target manager: `https://connect.oddsockets.tyga.network`

## Get a free API key

No credit card required. Sign up in two steps with `curl`.

1. Request a verification code:

```bash
curl -X POST https://oddsockets.com/api/agent-signup \
  -H "Content-Type: application/json" \
  -d '{"email": "you@example.com", "agentName": "unity-demo", "platform": "unity"}'
```

2. Check your inbox for the code, then verify to receive your API key:

```bash
curl -X POST https://oddsockets.com/api/agent-signup/verify \
  -H "Content-Type: application/json" \
  -d '{"email": "you@example.com", "code": "123456", "agentName": "unity-demo"}'
```

The verify response contains your API key (starts with `ak_`).

## Run it in the Editor

Unity is GUI-driven, so the demo is a scene component you attach and Play.

1. Copy the SDK `Scripts/` folder into your project's `Assets/` directory (see the repo
   root README). The SDK depends on Newtonsoft Json and a Socket.IO Unity client.
2. Copy `demo/DemoRoundTrip.cs` into your project's `Assets/` directory.
3. Provide your API key. Preferred: set the `ODDSOCKETS_API_KEY` environment variable
   before launching the Unity Editor so the process inherits it:

   ```bash
   export ODDSOCKETS_API_KEY="ak_your_key_here"
   # launch the Unity Editor from this same shell
   ```

   If setting an environment variable for the Editor is impractical on your platform,
   select the `DemoRoundTrip` component in the Inspector and paste the key into the
   `Api Key Override` field. The environment variable takes precedence. Never commit a key.
4. In the Unity Editor, create an empty GameObject in your scene (GameObject > Create Empty).
5. With that GameObject selected, click Add Component and add `Demo Round Trip`.
6. Press Play. Watch the Console window.

On success the Console logs:

```
[connect] connecting both clients...
[alice] worker w002-oddsockets-1
[bob]   worker w002-oddsockets-1
[connect] alice = connected, bob = connected
[alice] subscribed to demo-1a2b3c4d (presence on)
[bob] published to demo-1a2b3c4d
[alice] received bob's message (nonce matched) - real round-trip.
[alice] presence: 1 user(s).
[alice] unsubscribed.

OK - cross-client round-trip verified
```

If the round-trip does not complete within 20 seconds, a watchdog logs a failure line
beginning with `FAIL`.

## Run it headless (CI gate)

The same component doubles as the acceptance test. In batch mode it force-quits the
player with a non-zero exit code on failure, so it can gate CI. From a scene that
contains a GameObject with the `DemoRoundTrip` component:

```bash
export ODDSOCKETS_API_KEY="ak_your_key_here"
"/path/to/Unity" -batchmode -nographics -projectPath "$(pwd)" \
  -executeMethod is not required - the component runs on scene Start
```

Load the demo scene in `-batchmode` (for example via a bootstrap scene set as the first
Build Settings scene) and the run exits `0` on a verified round-trip, non-zero otherwise.

## The code, step by step

Stand up two independent clients - a subscriber and a publisher - each its own
`OddSocketsClient` MonoBehaviour and its own worker connection:

```csharp
var alice = gameObject.AddComponent<OddSocketsClient>();
alice.Initialize(new OddSocketsUnityConfig { ApiKey = apiKey, UserId = "alice", AutoConnect = false });

var bob = gameObject.AddComponent<OddSocketsClient>();
bob.Initialize(new OddSocketsUnityConfig { ApiKey = apiKey, UserId = "bob", AutoConnect = false });

await alice.ConnectAsync();
await bob.ConnectAsync();
```

Subscribe on the subscriber (presence enabled). A message only lands in the callback if
it came back through the worker:

```csharp
var aliceChannel = alice.Channel(channelName);
await aliceChannel.SubscribeAsync(OnAliceMessage, new SubscriptionOptions { EnablePresence = true });
```

Publish from the *other* client - this is what makes the test honest:

```csharp
var bobChannel = bob.Channel(channelName);
await bobChannel.PublishAsync(new { text = "hello from bob", nonce = nonce });
```

Inspect presence, then tear down cleanly:

```csharp
var presence = await aliceChannel.GetPresenceAsync();
await aliceChannel.UnsubscribeAsync();
alice.Disconnect();
bob.Disconnect();
```

## What it demonstrates

- Manager discovery + automatic worker assignment (fully transparent)
- `client.Channel()` -> `channel.SubscribeAsync()` -> `channel.PublishAsync()`
- **Cross-client delivery**: a message published by `bob` is delivered to `alice`'s
  subscription in real time - provably through the worker, not a local echo
- Presence tracking, unsubscribe, and graceful disconnect
- A watchdog timeout so a stalled handshake or round-trip is reported as a failure
- Reading the API key from an environment variable so no key is hardcoded
