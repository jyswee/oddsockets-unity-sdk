# Changelog

All notable changes to the OddSockets Realtime Unity SDK are documented here.
This project adheres to [Semantic Versioning](https://semver.org).

## [Unreleased]

### Added
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
