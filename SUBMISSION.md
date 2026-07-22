# Asset Store Submission Guide

Internal checklist for publishing OddSockets Realtime to the Unity Asset Store.
Not shipped to buyers (kept in the repo root; exclude from the uploaded package if
you prefer).

## 1. Package contents (what buyers get)

```
package.json                 UPM manifest (com.oddsockets.unity)
README.md                    Overview, install, quick start, enhanced API
CHANGELOG.md                 Versioned change history
LICENSE.md                   MIT (OddSockets)
Third Party Notices.md       SocketIOUnity (MIT) + Newtonsoft (MIT)
Runtime/                     SDK source + OddSockets.Unity.asmdef
ThirdParty/SocketIOUnity/    Bundled MIT Socket.IO transport (unmodified) + its LICENSE
Samples~/                    Basic Usage, Two-Client Round Trip (imported via Package Manager)
```

Dependencies:
- `com.unity.nuget.newtonsoft-json` resolves automatically (declared in package.json).
- SocketIOUnity is **bundled** under `ThirdParty/SocketIOUnity` - no external install.

## 2. Pre-flight checklist

- [x] Compiles with zero errors in a clean project (validated: rig consuming the package via `file:`; both `OddSockets.Unity` and `SocketIOUnityAssembly` build).
- [x] Live end-to-end proof against production (headless two-client PlayMode test: thread_reply / reaction_added / user_typing round-trip nonce-matched through prod worker).
- [x] All assets have `.meta` files with stable GUIDs (generated on import, committed).
- [x] No competitor names in any public copy (README, package.json, listing).
- [x] No internal hostnames or secrets in source (only public `connect.oddsockets.tyga.network`).
- [x] Third-party license retained (`ThirdParty/SocketIOUnity/LICENSE`) + Third Party Notices.
- [ ] Minimum Unity version confirmed on the listing: **2021.3** (matches package.json `unity`).
- [ ] Bump `version` in package.json + add a CHANGELOG entry for each release.

## 3. Listing metadata (fill in the Publisher Portal)

- **Title:** OddSockets Realtime
- **Category:** Tools > Network (or Tools > Integration)
- **Summary:** Realtime messaging for Unity - channels, presence, reactions, threads, typing, read receipts, DMs, notifications. Managed backend, no server to run.
- **Keywords:** realtime, websocket, socket.io, networking, messaging, chat, presence, multiplayer, pubsub
- **Price:** decide (free listing drives funnel to the hosted plans; paid asset is an alternative).
- **Publisher:** OddSockets - A division of Tyga.Cloud Ltd

## 4. Required media (produce separately)

- Key image / icon (per current Asset Store size specs).
- 3-5 screenshots: connect + subscribe code, a running chat/presence demo scene, the enhanced-features API table.
- Optional short demo video (the Two-Client Round Trip sample makes a clean clip).

## 5. Upload steps

1. Create a Unity project on the listing's minimum version (2021.3).
2. Add the package (git URL or local `file:`) so it imports with its Newtonsoft dependency; confirm no console errors.
3. Install the **Publisher** tooling: `Asset Store Publishing Tools` (Package Manager > My Assets) or the Package Manager upload flow.
4. Select the package folder and upload the draft to the Publisher Portal.
5. Fill metadata + media (sections 3-4), then submit for review.
6. Respond to any reviewer notes (common: missing `.meta`, dependency clarity, demo scene). Both are already addressed here.

## 6. Notes for reviewers (paste into submission notes)

- SocketIOUnity (MIT) is bundled unmodified under `ThirdParty/SocketIOUnity` with its
  original LICENSE; attribution is in `Third Party Notices.md`. No external download required.
- Newtonsoft Json is an official Unity package dependency, auto-resolved.
- A free API key is available at oddsockets.com; the SDK connects to the managed
  OddSockets worker fleet - no backend to stand up.
