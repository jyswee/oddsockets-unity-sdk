using System;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OddSockets.Unity;

namespace OddSockets.Unity.Demo
{
    /// <summary>
    /// Honest two-client end-to-end round-trip for the OddSockets Unity SDK.
    ///
    /// This is the PlayMode acceptance gate. Instead of a single client echoing to
    /// itself, it stands up TWO independent OddSocketsClient connections:
    ///
    ///   alice - subscriber (presence enabled)
    ///   bob   - publisher
    ///
    /// Because alice and bob are separate connections, a message that reaches alice
    /// can only have travelled through the assigned OddSockets worker - so this is a
    /// genuine cross-client regression, not a local echo. On Start() this component:
    ///
    ///   1. Connects alice and bob (the manager assigns each a worker transparently).
    ///   2. alice subscribes to a unique channel "demo-&lt;random&gt;" with presence on.
    ///   3. bob publishes a { text, nonce } message to that channel.
    ///   4. alice receives bob's message; a matching nonce proves a real round-trip.
    ///   5. alice reads presence, unsubscribes, and both clients disconnect.
    ///
    /// A coroutine watchdog fails the run if the round-trip does not complete in time.
    /// Success logs "OK - cross-client round-trip verified".
    ///
    /// The API key is read from the ODDSOCKETS_API_KEY environment variable. As a
    /// fallback for Editor workflows where env vars are impractical, set the serialized
    /// <see cref="apiKeyOverride"/> field in the Inspector. The env var takes precedence.
    /// NEVER hardcode a key.
    ///
    /// When run in batch mode (-batchmode), the component quits the player with a
    /// non-zero exit code on failure so it can gate CI.
    ///
    /// Target manager: https://connect.oddsockets.tyga.network
    /// </summary>
    public class DemoRoundTrip : MonoBehaviour
    {
        [Header("API Key")]
        [Tooltip("Optional fallback if ODDSOCKETS_API_KEY is not set. Leave empty to require the env var. Never commit a real key.")]
        [SerializeField] private string apiKeyOverride = "";

        [Header("Demo Settings")]
        [Tooltip("Seconds to wait for the cross-client round-trip before declaring failure.")]
        [SerializeField] private float timeoutSeconds = 20f;

        private OddSocketsClient alice;
        private OddSocketsClient bob;
        private OddSocketsChannel aliceChannel;
        private OddSocketsChannel bobChannel;
        private string channelName;
        private string nonce;
        private bool verified;
        private bool failed;
        private Coroutine timeoutRoutine;

        private void Start()
        {
            string apiKey = Environment.GetEnvironmentVariable("ODDSOCKETS_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = apiKeyOverride;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Fail("no API key. Set the ODDSOCKETS_API_KEY environment variable " +
                     "or the apiKeyOverride field in the Inspector.");
                return;
            }

            // Unique channel per run so bob's message only lands on this alice.
            channelName = "demo-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            nonce = Guid.NewGuid().ToString("N");

            alice = gameObject.AddComponent<OddSocketsClient>();
            alice.Initialize(new OddSocketsUnityConfig
            {
                ApiKey = apiKey,
                UserId = "alice",
                AutoConnect = false
            });

            bob = gameObject.AddComponent<OddSocketsClient>();
            bob.Initialize(new OddSocketsUnityConfig
            {
                ApiKey = apiKey,
                UserId = "bob",
                AutoConnect = false
            });

            alice.OnError += (ex) => Fail("alice error: " + ex.Message);
            bob.OnError += (ex) => Fail("bob error: " + ex.Message);

            timeoutRoutine = StartCoroutine(TimeoutWatchdog());

            Debug.Log($"[connect] connecting both clients, channel '{channelName}', nonce '{nonce}'...");
            _ = RunAsync();
        }

        private async System.Threading.Tasks.Task RunAsync()
        {
            try
            {
                // Connect both independent clients.
                await alice.ConnectAsync();
                await bob.ConnectAsync();

                if (alice.State != ConnectionState.Connected || bob.State != ConnectionState.Connected)
                {
                    Fail("clients did not connect");
                    return;
                }

                Debug.Log($"[alice] worker {alice.Worker?.WorkerId}");
                Debug.Log($"[bob]   worker {bob.Worker?.WorkerId}");
                Debug.Log("[connect] alice = connected, bob = connected");

                // alice subscribes (presence on) BEFORE bob publishes.
                aliceChannel = alice.Channel(channelName);
                await aliceChannel.SubscribeAsync(OnAliceMessage, new SubscriptionOptions
                {
                    EnablePresence = true
                });
                Debug.Log($"[alice] subscribed to {channelName} (presence on)");

                // bob publishes on his own connection - this is what makes it honest.
                bobChannel = bob.Channel(channelName);
                await bobChannel.PublishAsync(new { text = "hello from bob", nonce = nonce });
                Debug.Log($"[bob] published to {channelName}");
                Debug.Log("[alice] waiting for bob's message...");
            }
            catch (Exception ex)
            {
                Fail("round-trip setup error: " + ex.Message);
            }
        }

        private async void OnAliceMessage(ChannelMessageData data)
        {
            if (verified || failed) return;

            string received = ExtractNonce(data.Message);
            if (received != nonce) return;

            verified = true;
            Debug.Log("[alice] received bob's message (nonce matched) - real round-trip.");

            try
            {
                var presence = await aliceChannel.GetPresenceAsync();
                Debug.Log($"[alice] presence: {presence.Count} user(s).");
                await aliceChannel.UnsubscribeAsync();
                Debug.Log("[alice] unsubscribed.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[alice] post-verify step failed: " + ex.Message);
            }

            if (timeoutRoutine != null)
            {
                StopCoroutine(timeoutRoutine);
                timeoutRoutine = null;
            }

            Teardown();
            Debug.Log("\nOK - cross-client round-trip verified");
            Quit(0);
        }

        /// <summary>
        /// The message payload arrives as a deserialized object. Normalize it to JSON
        /// and read the nonce field regardless of the concrete runtime type.
        /// </summary>
        private static string ExtractNonce(object message)
        {
            try
            {
                if (message == null) return null;

                JToken token = message as JToken ?? JToken.Parse(JsonConvert.SerializeObject(message));

                // Handle a payload delivered as a JSON string.
                if (token.Type == JTokenType.String)
                {
                    token = JToken.Parse(token.Value<string>());
                }

                return token["nonce"]?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private IEnumerator TimeoutWatchdog()
        {
            yield return new WaitForSeconds(timeoutSeconds);
            if (!verified)
            {
                Fail($"cross-client round-trip not verified within {timeoutSeconds} seconds");
            }
        }

        private void Fail(string reason)
        {
            if (failed || verified) return;
            failed = true;
            Debug.LogError("FAIL - " + reason);
            Teardown();
            Quit(1);
        }

        private void Teardown()
        {
            try { alice?.Disconnect(); } catch { }
            try { bob?.Disconnect(); } catch { }
        }

        private static void Quit(int code)
        {
            // Only force-quit in headless/batch CI runs; leave the Editor alive otherwise.
            if (Application.isBatchMode)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.Exit(code);
#else
                Application.Quit(code);
#endif
            }
        }

        private void OnDestroy()
        {
            Teardown();
        }
    }
}
