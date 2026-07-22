# Changelog

All notable changes to the OddSockets Realtime Unity SDK are documented here.
This project adheres to [Semantic Versioning](https://semver.org).

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
