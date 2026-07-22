# OddSockets Unity SDK

Official Unity SDK for OddSockets real-time messaging platform. C# MonoBehaviour components for pub/sub, presence, message history.

OddSockets - A division of Tyga.Cloud Ltd

## Install

Copy the `Scripts/` folder into your Unity project's `Assets/` directory.

## Quick Start

```csharp
// Attach OddSocketsClient to a GameObject
var client = gameObject.AddComponent<OddSocketsClient>();
client.Initialize(new OddSocketsUnityConfig { ApiKey = "YOUR_API_KEY", UserId = "my-agent" });
await client.ConnectAsync();

var channel = client.Channel("my-channel");
await channel.SubscribeAsync(msg => Debug.Log($"Received: {msg.Message}"));
await channel.PublishAsync(new { text = "Hello from Unity" });
```

Call the async methods from an `async` context (for example an `async void Start`).

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

MIT License - Copyright (c) 2026 Joe Wee, Tyga.Cloud Ltd. See [LICENSE](LICENSE) for details.
