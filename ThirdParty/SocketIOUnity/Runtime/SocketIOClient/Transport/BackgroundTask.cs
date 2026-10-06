using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Transport
{
    /// <summary>
    /// Starts the library's long-lived pump loops (receive, ping, connect).
    ///
    /// On most platforms these run on the thread pool, exactly as before. Unity
    /// WebGL has no thread pool: work queued to the default TaskScheduler never
    /// executes in the browser, so there the loops are scheduled onto the Unity
    /// main-thread synchronization context instead, where awaits yield back to
    /// the browser event loop between steps. (FEAT-2026-1006-0012)
    /// </summary>
    internal static class BackgroundTask
    {
        public static void Run(Func<Task> loop, CancellationToken cancellationToken, bool longRunning = false)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var scheduler = SynchronizationContext.Current != null
                ? TaskScheduler.FromCurrentSynchronizationContext()
                : TaskScheduler.Current;
            Task.Factory.StartNew(loop, cancellationToken, TaskCreationOptions.None, scheduler);
#else
            Task.Factory.StartNew(loop, cancellationToken,
                longRunning ? TaskCreationOptions.LongRunning : TaskCreationOptions.None,
                TaskScheduler.Default);
#endif
        }

        /// <summary>
        /// Task.Delay that completes on every platform. Unity WebGL has no
        /// timer thread, so System.Threading.Tasks timers never fire and a
        /// plain Task.Delay awaits forever; there the wait is a frame-yield
        /// loop on the main-thread context instead. (FEAT-2026-1006-0012)
        /// </summary>
        public static async Task Delay(int milliseconds, CancellationToken cancellationToken = default)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var end = DateTime.UtcNow.AddMilliseconds(milliseconds);
            while (DateTime.UtcNow < end)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
#else
            await Task.Delay(milliseconds, cancellationToken).ConfigureAwait(OSAwait.Continue);
#endif
        }

        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            return Delay((int)delay.TotalMilliseconds, cancellationToken);
        }
    }
}
