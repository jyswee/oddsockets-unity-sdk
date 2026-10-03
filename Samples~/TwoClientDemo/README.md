# OddSockets Unity SDK - Two-Client Round-Trip Demo

A runnable PlayMode demo that proves a real real-time round-trip against OddSockets
using **two independent clients**: **connect -> subscribe -> publish -> receive**.

Because the subscriber (`alice`) and the publisher (`bob`) are separate connections,
a message that reaches alice can only have travelled through the live OddSockets
service - so this doubles as an honest end-to-end regression (no mocks, no local
echo). Connection setup and routing are resolved by the SDK; this sample configures
nothing but an API key.

## Get an API key

No free tier - every plan starts with a 7-day free trial (nothing is charged
during the trial). Signup issues a working API key instantly; the key runs
keyless for 48 hours, and adding a card within that window extends it through
the trial.

```bash
npm i -g oddsockets-cli
oddsockets plans                                  # list live plan ids
oddsockets signup you@studio.com --plan oddsockets-starter
```

AI agents can also self-provision via MCP: connect to
`https://mcp.oddsockets.ai/sse` and call `oddsockets_signup`.

Your API key starts with `ak_`.

## Run it in the Editor

Unity is GUI-driven, so the demo is a scene component you attach and Play.

1. Install the SDK package (see the repo root README for the Package Manager URL).
2. In Package Manager, select the OddSockets package and import the **TwoClientDemo**
   sample. That drops `DemoRoundTrip.cs` into your project's `Assets/Samples/` directory.
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
[connect] connecting both clients, channel 'demo-1a2b3c4d', nonce '...'...
[connect] alice = connected, bob = connected
[alice] subscribed to demo-1a2b3c4d (presence on)
[bob] published to demo-1a2b3c4d
[alice] waiting for bob's message...
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
"/path/to/Unity" -batchmode -nographics -projectPath "$(pwd)"
```

No `-executeMethod` is needed - the component runs on scene `Start`. Make the demo scene
the first entry in Build Settings so `-batchmode` loads it, and the run exits `0` on a
verified round-trip, non-zero otherwise.

## The code, step by step

Stand up two independent clients - a subscriber and a publisher - each its own
`OddSocketsClient` MonoBehaviour with its own connection:

```csharp
var alice = gameObject.AddComponent<OddSocketsClient>();
alice.Initialize(new OddSocketsUnityConfig { ApiKey = apiKey, UserId = "alice", AutoConnect = false });

var bob = gameObject.AddComponent<OddSocketsClient>();
bob.Initialize(new OddSocketsUnityConfig { ApiKey = apiKey, UserId = "bob", AutoConnect = false });

await alice.ConnectAsync();
await bob.ConnectAsync();
```

Subscribe on the subscriber (presence enabled). A message only lands in the callback if
it came back through the service:

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

- Zero-config connection: the SDK resolves routing itself, you supply only an API key
- `client.Channel()` -> `channel.SubscribeAsync()` -> `channel.PublishAsync()`
- **Cross-client delivery**: a message published by `bob` is delivered to `alice`'s
  subscription in real time - provably through the service, not a local echo
- Presence tracking, unsubscribe, and graceful disconnect
- A watchdog timeout so a stalled handshake or round-trip is reported as a failure
- Reading the API key from an environment variable so no key is hardcoded
