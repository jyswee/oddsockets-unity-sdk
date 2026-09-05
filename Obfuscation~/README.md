# Obfuscation pipeline (maintainers only)

This folder reproduces the shipped, obfuscated plugin at
`Runtime/OddSockets.Unity.dll`. It is **dev tooling, not package content** — the
`~` suffix makes Unity ignore the folder, and it is excluded from the Asset Store
export.

The SDK ships as an obfuscated DLL (not `.cs`) so the proprietary wire protocol,
manager endpoints, and enhanced-feature/challenge API cannot be read off the
Asset Store / public registry. The pre-obfuscation C# source is not kept in this
branch on purpose; it lives in git history and in the private build environment.

## Stages

1. **Compile** the unobfuscated `OddSockets.Unity.dll` from the C# source with
   Unity in batchmode (this is what task #238 does — it also produces the
   dependency DLLs used as `--refs`). Output lands in the project's
   `Library/ScriptAssemblies` / package cache.
2. **Obfuscar** (`obfuscar.xml`) renames private members/types and, most
   importantly, encrypts string literals (`HideStrings`) — this is the MOAT win:
   it hides the wire event names and manager endpoints. Skips are configured for:
   - Unity magic methods (`Awake/Start/Update/OnDestroy`) — the engine calls them
     by name; renaming silently breaks lifecycle (`channels` never initialised →
     NRE in `Channel()`).
   - Wire/Inspector DTO fields (`OddSocketsMessage`, `PresenceInfo`, etc.) —
     Newtonsoft/JsonUtility key off exact field names. These names are observable
     on the wire anyway, so hiding them buys ~no MOAT.
   - Anonymous types (`*AnonymousType*`) — Newtonsoft matches their ctor to
     properties by parameter name.
3. **corlib-patch** (`corlib-patch/`, Mono.Cecil) fixes two Obfuscar artefacts:
   - Obfuscar strips **all** ctor parameter names globally (even for skipped
     types), so it restores anonymous-type ctor param names from their properties
     (else Newtonsoft throws *"A member with the name '' already exists"* the first
     time a wire payload is (de)serialised).
   - Obfuscar's string-decryptor references `System.Private.CoreLib` (.NET Core
     corlib), which Unity's Mono can't resolve. It repoints those type refs onto
     `netstandard` and removes the dangling assembly ref.
4. **Verify**: 0 MOAT strings visible, 0 `System.Private.CoreLib` refs.

## Run

```bash
# Requirements: dotnet (net10.0), Obfuscar.GlobalTool 2.2.50, Unity 6000.0.79f1.
dotnet tool install -g Obfuscar.GlobalTool   # once

./build.sh \
  --in   /path/to/Library/ScriptAssemblies/OddSockets.Unity.dll \
  --refs /path/to/dir/with/Newtonsoft.Json.dll+SocketIOUnityAssembly.dll+System.*.dll \
  --install
```

- `--in` is the Unity-compiled **unobfuscated** DLL from stage 1.
- `--refs` is a directory holding the DLLs Obfuscar needs to resolve the type
  graph: `Newtonsoft.Json.dll`, `SocketIOUnityAssembly.dll`,
  `Microsoft.Bcl.AsyncInterfaces.dll`, and the `System.*` support assemblies
  (all produced alongside the input DLL by the Unity compile).
- `--install` copies the verified result to `../Runtime/OddSockets.Unity.dll`.
  Omit it to inspect the output under `work/out/` first.

If your Unity Editor version differs from `6000.0.79f1`, update the
`<AssemblySearchPath>` entries in `obfuscar.xml`.

## Verifying the shipped DLL

The pipeline is proven end-to-end by the headless PlayMode rig (the keyless
minted-token E2E test connects + subscribes + publishes a round-trip over the
live worker against the obfuscated DLL). See the docs demo rig for that pattern.
