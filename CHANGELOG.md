# Changelog

All notable changes to the OddSockets Realtime Unity SDK are documented here.
This project adheres to [Semantic Versioning](https://semver.org).

## [1.0.3] - 2026-10-05

This tag exists primarily so the keyless token path is fetchable again: the
previously pinnable commit (`37e72a64`, v1.0.1-era) was removed in a history
rewrite, and v1.0.2 was published before these notes called the keyless API
out explicitly. Pin `#v1.0.3`.

### Added
- Keyless authentication: `OddSocketsUnityConfig.TokenProvider` and the
  `OddSocketsToken` type. A game exchanges its own player JWT for a short-lived
  realtime token at the OddSockets front door, so App Store / shipping builds
  carry no API key. The client auto-refreshes the token before expiry.
- `OddSocketsUnityConfig.ManagerUrl` so a build can be pointed at a self-hosted or staging
  manager. It falls back to the `ODDSOCKETS_MANAGER_URL` environment variable and then to
  the public endpoint, and must be an absolute `http://` or `https://` URL.

### Fixed
- Manager discovery no longer ignores the configured manager and return the public endpoint
  unconditionally. A configured manager is now used verbatim, and if it is unreachable the
  connection fails with the underlying error instead of silently connecting to production.

### Changed
- `ManagerDiscovery.TestConnectivityAsync` is replaced by `VerifyConnectivityAsync`, which
  throws when the manager cannot be reached instead of returning a `bool` that a caller can
  ignore and read as success.
- `ManagerDiscovery.DiscoverManagerUrlAsync`, `DiscoverManagerUrl` and `GetManagerInfoAsync`
  now take the configured manager URL as an argument.

## [1.0.2] - 2026-09-05

### Changed
- The OddSockets runtime now ships as a precompiled, obfuscated managed plugin
  (`Runtime/OddSockets.Unity.dll`) instead of source. The public API, Inspector-serialized
  fields, and wire protocol are unchanged; internal members are renamed and string literals
  are encrypted. `ThirdParty/SocketIOUnity` and the `com.unity.nuget.newtonsoft-json`
  dependency are unchanged and continue to resolve the plugin's assembly references by name.

## [1.0.1] - 2026-07-27

### Fixed
- Channel and enhanced-feature callbacks now fire on Unity's main thread. Previously they
  ran on the SocketIOUnity background receive thread, so any Unity API call inside a handler
  (GameObject/Component access) threw `UnityException: ... can only be called from the main
  thread`. Handlers are now marshaled through a queue drained in `Update()`. (BUG-2026-0723-0007)
- Exceptions thrown inside a subscriber callback are now caught and logged with event context
  via `Debug.LogException`, instead of being silently swallowed. (BUG-2026-0723-0008)

## [1.0.0] - 2026-07-22

Initial public release.

### Added
- Core realtime client (`OddSocketsClient`) with automatic manager discovery and worker assignment.
- Channel pub/sub (`OddSocketsChannel`): subscribe, publish, presence, and message history.
- Enhanced-feature surface (`client.Enhanced`) covering reactions, threads, message editing,
  read receipts, presence and custom status, typing indicators, file uploads, direct messages,
  notifications, and channel management.
- Raw event API: `client.On(event, handler)` and `client.EmitAsync(event, payload)` for any
  worker event, with reconnect-safe listener re-binding.
- Samples: Basic Usage and Two-Client Round Trip.
- UPM package layout with automatic Newtonsoft Json dependency resolution.
