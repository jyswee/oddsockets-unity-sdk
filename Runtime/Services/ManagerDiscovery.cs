using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace OddSockets.Unity
{
    /// <summary>
    /// Simple Manager Discovery Service for Unity.
    /// 
    /// Always connects to the main manager endpoint which handles
    /// all routing and load balancing transparently.
    /// </summary>
    public class ManagerDiscovery
    {
        private static ManagerDiscovery _instance;
        private readonly string _managerUrl = "https://connect.oddsockets.tyga.network";

        /// <summary>
        /// Singleton instance of the ManagerDiscovery service.
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
        /// Get the manager URL (always returns the main endpoint).
        /// </summary>
        /// <param name="apiKey">The OddSockets API key (not used, kept for compatibility)</param>
        /// <returns>The manager URL</returns>
        public async Task<string> DiscoverManagerUrlAsync(string apiKey)
        {
            // In the simplified version, we always return the main endpoint
            // In a more complex implementation, this could do actual discovery
            await Task.Yield(); // Make it properly async
            return _managerUrl;
        }

        /// <summary>
        /// Get the manager URL synchronously (always returns the main endpoint).
        /// </summary>
        /// <param name="apiKey">The OddSockets API key (not used, kept for compatibility)</param>
        /// <returns>The manager URL</returns>
        public string DiscoverManagerUrl(string apiKey)
        {
            return _managerUrl;
        }

        /// <summary>
        /// Clear cache (no-op, kept for compatibility).
        /// </summary>
        public void ClearCache()
        {
            // No cache to clear in simplified version
        }

        /// <summary>
        /// Test connectivity to the manager endpoint.
        /// </summary>
        /// <param name="apiKey">The API key to test with</param>
        /// <returns>True if the manager is reachable, false otherwise</returns>
        public async Task<bool> TestConnectivityAsync(string apiKey)
        {
            try
            {
                var managerUrl = await DiscoverManagerUrlAsync(apiKey);
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

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"Manager connectivity test failed: {request.error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Manager connectivity test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get manager information including version and status.
        /// </summary>
        /// <param name="apiKey">The API key to use</param>
        /// <returns>Manager information or null if unavailable</returns>
        public async Task<ManagerInfo> GetManagerInfoAsync(string apiKey)
        {
            try
            {
                var managerUrl = await DiscoverManagerUrlAsync(apiKey);
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
