namespace SocketIOClient
{
    /// <summary>
    /// Platform-correct value for ConfigureAwait throughout the library.
    ///
    /// Off the browser this is false (classic library hygiene: don't bounce
    /// continuations through a caller's context). On Unity WebGL it must be
    /// true: when an awaited task completes asynchronously on the Unity main
    /// thread, a context-less continuation is not eligible for inlining
    /// (the thread has a non-default SynchronizationContext) and gets queued
    /// to the thread pool — which does not exist in the browser, so the
    /// continuation never runs and the client hangs after the WebSocket
    /// opens. Capturing the Unity context keeps every continuation on the
    /// frame-pumped main thread. (FEAT-2026-1006-0012)
    /// </summary>
    internal static class OSAwait
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        public const bool Continue = true;
#else
        public const bool Continue = false;
#endif
    }
}
