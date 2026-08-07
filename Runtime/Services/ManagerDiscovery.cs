using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace OddSockets.Unity
{
    /// <summary>
    /// Manager Discovery Service for Unity.
    ///
    /// Resolves the manager endpoint that a client should talk to. The manager
    /// handles all worker routing and load balancing transparently.
    ///
    /// Resolution order is: explicit configuration, then the ODDSOCKETS_MANAGER_URL
    /// environment variable, then the public default endpoint. The default is only
    /// ever used when nothing was configured at all - a configured manager that is
    /// unreachable must surface that failure, because silently redirecting a
    /// self-hosted or staging build to production makes a misconfigured deployment
    /// look healthy.
    /// </summary>
    public class ManagerDiscovery
    {
        private static ManagerDiscovery _instance;

        /// <summary>
        /// The public manager endpoint, used only when no manager URL was configured.
        /// </summary>
        public const string DefaultManagerUrl = "https://connect.oddsockets.tyga.network";

        /// <summary>
        /// Environment variable consulted when no manager URL was configured explicitly.
        /// </summary>
        public const string ManagerUrlEnvironmentVariable = "ODDSOCKETS_MANAGER_URL";

        /// <summary>
        /// Singleton instance of the ManagerDiscovery service. It holds no per-client
        /// state: the manager URL is always supplied by the caller so that one client's
        /// configuration cannot leak into another's.
        /// </summary>
        public static ManagerDiscovery Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ManagerDiscovery();
                }
                return _instance;
            }
        }

        private ManagerDiscovery()
        {
            // Private constructor for singleton
        }

        /// <summary>
        /// Resolve the manager URL for a client.
        /// </summary>
        /// <param name="apiKey">The OddSockets API key</param>
        /// <param name="configuredManagerUrl">The manager URL from the client configuration, may be empty</param>
        /// <returns>The resolved manager URL, without any trailing slash</returns>
        /// <exception cref="ArgumentException">Thrown when the resolved URL is not an absolute http(s) URL</exception>
        public async Task<string> DiscoverManagerUrlAsync(string apiKey, string configuredManagerUrl)
        {
            await Task.Yield(); // Make it properly async
            return ResolveManagerUrl(configuredManagerUrl);
        }

        /// <summary>
        /// Resolve the manager URL for a client synchronously.
        /// </summary>
        /// <param name="apiKey">The OddSockets API key</param>
        /// <param name="configuredManagerUrl">The manager URL from the client configuration, may be empty</param>
        /// <returns>The resolved manager URL, without any trailing slash</returns>
        /// <exception cref="ArgumentException">Thrown when the resolved URL is not an absolute http(s) URL</exception>
        public string DiscoverManagerUrl(string apiKey, string configuredManagerUrl)
        {
            return ResolveManagerUrl(configuredManagerUrl);
        }

        /// <summary>
        /// Resolve and validate a manager URL.
        /// </summary>
        /// <param name="configuredManagerUrl">The manager URL from the client configuration, may be empty</param>
        /// <returns>The resolved manager URL, without any trailing slash</returns>
        /// <exception cref="ArgumentException">Thrown when the resolved URL is not an absolute http(s) URL</exception>
        public static string ResolveManagerUrl(string configuredManagerUrl)
        {
            var candidate = configuredManagerUrl;

            if (string.IsNullOrEmpty(candidate) || candidate.Trim().Length == 0)
            {
                candidate = Environment.GetEnvironmentVariable(ManagerUrlEnvironmentVariable);
            }

            if (string.IsNullOrEmpty(candidate) || candidate.Trim().Length == 0)
            {
                candidate = DefaultManagerUrl;
            }

            return ValidateManagerUrl(candidate);
        }

        private static string ValidateManagerUrl(string managerUrl)
        {
            var candidate = managerUrl.Trim();

            Uri uri;
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrEmpty(uri.Host))
            {
                throw new ArgumentException($"Invalid managerUrl: {managerUrl}");
            }

            return candidate.TrimEnd('/');
        }

        /// <summary>
        /// Clear cache (no-op, kept for compatibility).
        /// </summary>
        public void ClearCache()
        {
            // No cache to clear in simplified version
        }

        /// <summary>
        /// Verify that the configured manager endpoint is reachable.
        ///
        /// This throws rather than returning a status flag: a caller that forgets to
        /// inspect a returned bool would read an unreachable manager as a healthy one.
        /// </summary>
        /// <param name="apiKey">The API key to test with</param>
        /// <param name="configuredManagerUrl">The manager URL from the client configuration, may be empty</param>
        /// <exception cref="Exception">Thrown when the manager cannot be reached</exception>
        public async Task VerifyConnectivityAsync(string apiKey, string configuredManagerUrl)
        {
            var managerUrl = await DiscoverManagerUrlAsync(apiKey, configuredManagerUrl);
            var testUrl = $"{managerUrl}/api/health";

            using (var request = UnityWebRequest.Get(testUrl))
            {
                request.timeout = 10;
                request.SetRequestHeader("User-Agent", "OddSockets-Unity-SDK/1.0.0");

                var operation = request.SendWebRequest();

                // Wait for the request to complete
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Manager {managerUrl} is unreachable: {request.error}");
                }
            }
        }

        /// <summary>
        /// Get manager information including version and status.
        /// </summary>
        /// <param name="apiKey">The API key to use</param>
        /// <param name="configuredManagerUrl">The manager URL from the client configuration, may be empty</param>
        /// <returns>Manager information or null if unavailable</returns>
        public async Task<ManagerInfo> GetManagerInfoAsync(string apiKey, string configuredManagerUrl)
        {
            // Resolved outside the catch below: a misconfigured manager URL is a caller
            // error and must not be reported as "info unavailable".
            var managerUrl = await DiscoverManagerUrlAsync(apiKey, configuredManagerUrl);

            try
            {
                var infoUrl = $"{managerUrl}/api/info";

                using (var request = UnityWebRequest.Get(infoUrl))
                {
                    request.timeout = 10;
                    request.SetRequestHeader("User-Agent", "OddSockets-Unity-SDK/1.0.0");

                    var operation = request.SendWebRequest();
                    
                    // Wait for the request to complete
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var json = request.downloadHandler.text;
                        return JsonUtility.FromJson<ManagerInfo>(json);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to get manager info: {request.error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get manager info: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Information about the manager service.
    /// </summary>
    [Serializable]
    public class ManagerInfo
    {
        [SerializeField] private string version;
        [SerializeField] private string status;
        [SerializeField] private int activeWorkers;
        [SerializeField] private int totalConnections;
        [SerializeField] private DateTime timestamp;

        /// <summary>
        /// Manager version.
        /// </summary>
        public string Version => version;

        /// <summary>
        /// Manager status.
        /// </summary>
        public string Status => status;

        /// <summary>
        /// Number of active workers.
        /// </summary>
        public int ActiveWorkers => activeWorkers;

        /// <summary>
        /// Total number of connections.
        /// </summary>
        public int TotalConnections => totalConnections;

        /// <summary>
        /// Timestamp of the information.
        /// </summary>
        public DateTime Timestamp => timestamp;

        /// <summary>
        /// Creates new ManagerInfo.
        /// </summary>
        public ManagerInfo(string version, string status, int activeWorkers, int totalConnections, DateTime timestamp)
        {
            this.version = version;
            this.status = status;
            this.activeWorkers = activeWorkers;
            this.totalConnections = totalConnections;
            this.timestamp = timestamp;
        }
    }
}
