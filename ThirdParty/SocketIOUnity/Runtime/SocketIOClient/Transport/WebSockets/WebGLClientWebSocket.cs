#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AOT;

namespace SocketIOClient.Transport.WebSockets
{
    /// <summary>
    /// IClientWebSocket backed by the browser's native WebSocket API via the
    /// OddSocketsWebSocket.jslib bridge. System.Net.WebSockets does not exist
    /// in the WebGL sandbox, so this is the only transport that works in
    /// browser builds.
    ///
    /// Browser WebSockets cannot carry custom HTTP headers; AddHeader is a
    /// no-op. OddSockets authentication rides the Socket.IO CONNECT payload
    /// (handshake auth), so nothing is lost. (FEAT-2026-1006-0012)
    /// </summary>
    public class WebGLClientWebSocket : IClientWebSocket
    {
        private delegate void OpenCallback(int instanceId);
        private delegate void TextCallback(int instanceId, IntPtr dataPtr, int length);
        private delegate void BinaryCallback(int instanceId, IntPtr dataPtr, int length);
        private delegate void ErrorCallback(int instanceId);
        private delegate void CloseCallback(int instanceId, int code, int wasClean);

        [DllImport("__Internal")]
        private static extern void OddSocketsWS_SetCallbacks(
            OpenCallback onOpen, TextCallback onText, BinaryCallback onBinary,
            ErrorCallback onError, CloseCallback onClose);

        [DllImport("__Internal")]
        private static extern int OddSocketsWS_Create(string url);

        [DllImport("__Internal")]
        private static extern int OddSocketsWS_State(int instanceId);

        [DllImport("__Internal")]
        private static extern void OddSocketsWS_SendText(int instanceId, string text);

        [DllImport("__Internal")]
        private static extern void OddSocketsWS_SendBinary(int instanceId, byte[] data, int length);

        [DllImport("__Internal")]
        private static extern void OddSocketsWS_Close(int instanceId, int code, string reason);

        [DllImport("__Internal")]
        private static extern void OddSocketsWS_Destroy(int instanceId);

        private static readonly Dictionary<int, WebGLClientWebSocket> Instances =
            new Dictionary<int, WebGLClientWebSocket>();
        private static bool _callbacksBound;

        private class ReceivedMessage
        {
            public byte[] Data;
            public TransportMessageType Type;
        }

        private int _instanceId = -1;
        private bool _opened;
        private bool _connectErrored;
        private readonly Queue<ReceivedMessage> _received = new Queue<ReceivedMessage>();
        private ReceivedMessage _current;
        private int _currentOffset;
        private readonly List<byte> _sendBuffer = new List<byte>();
        private bool _closed;
        private bool _disposed;

        public WebSocketState State
        {
            get
            {
                if (_instanceId < 0)
                {
                    return WebSocketState.None;
                }
                if (_closed)
                {
                    return WebSocketState.Closed;
                }
                switch (OddSocketsWS_State(_instanceId))
                {
                    case 0: return WebSocketState.Connecting;
                    case 1: return WebSocketState.Open;
                    case 2: return WebSocketState.CloseSent;
                    default: return WebSocketState.Closed;
                }
            }
        }

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            BindCallbacks();
            _instanceId = OddSocketsWS_Create(uri.ToString());
            if (_instanceId < 0)
            {
                throw new TransportException($"The browser refused a WebSocket to '{uri}'");
            }
            Instances[_instanceId] = this;
            // Poll with frame-yields rather than completing a TaskCompletionSource
            // from the JS event callback: continuations of a TCS resolved inside a
            // browser callback get queued to the (nonexistent) WebGL thread pool
            // and never run. Task.Yield posts to the Unity main-thread context,
            // which IS pumped. (FEAT-2026-1006-0012)
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_opened)
                {
                    return;
                }
                if (_connectErrored || _closed)
                {
                    throw new TransportException($"Could not open a WebSocket to '{uri}'");
                }
                await Task.Yield();
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            if (_instanceId >= 0 && !_closed)
            {
                OddSocketsWS_Close(_instanceId, 1000, "client disconnect");
            }
            _closed = true;
            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] bytes, TransportMessageType type, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            if (State != WebSocketState.Open)
            {
                return Task.FromException(
                    new InvalidOperationException("The WebSocket is not open"));
            }
            // The transport chunks outgoing payloads; the browser API only
            // sends whole messages, so reassemble until endOfMessage.
            _sendBuffer.AddRange(bytes);
            if (endOfMessage)
            {
                var whole = _sendBuffer.ToArray();
                _sendBuffer.Clear();
                if (type == TransportMessageType.Text)
                {
                    OddSocketsWS_SendText(_instanceId, Encoding.UTF8.GetString(whole));
                }
                else
                {
                    OddSocketsWS_SendBinary(_instanceId, whole, whole.Length);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<WebSocketReceiveResult> ReceiveAsync(int bufferSize,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_current == null)
                {
                    if (_received.Count > 0)
                    {
                        _current = _received.Dequeue();
                        _currentOffset = 0;
                    }
                    else if (_closed)
                    {
                        return new WebSocketReceiveResult
                        {
                            Buffer = new byte[0],
                            Count = 0,
                            EndOfMessage = true,
                            MessageType = TransportMessageType.Close
                        };
                    }
                    else
                    {
                        // Same frame-yield polling rationale as ConnectAsync.
                        await Task.Yield();
                        continue;
                    }
                }

                int remaining = _current.Data.Length - _currentOffset;
                int count = Math.Min(bufferSize, remaining);
                var slice = new byte[count];
                Buffer.BlockCopy(_current.Data, _currentOffset, slice, 0, count);
                _currentOffset += count;
                bool end = _currentOffset >= _current.Data.Length;
                var type = _current.Type;
                if (end)
                {
                    _current = null;
                }
                return new WebSocketReceiveResult
                {
                    Buffer = slice,
                    Count = count,
                    EndOfMessage = end,
                    MessageType = type
                };
            }
        }

        public void AddHeader(string key, string val)
        {
            // Browser WebSockets cannot set HTTP headers; auth is carried in
            // the Socket.IO CONNECT payload instead.
        }

        public void SetProxy(IWebProxy proxy)
        {
            // The browser owns proxying; nothing to configure here.
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_instanceId >= 0)
            {
                OddSocketsWS_Destroy(_instanceId);
                Instances.Remove(_instanceId);
            }
            _closed = true;
        }

        private void EnqueueReceived(byte[] data, TransportMessageType type)
        {
            _received.Enqueue(new ReceivedMessage { Data = data, Type = type });
        }

        private static void BindCallbacks()
        {
            if (_callbacksBound)
            {
                return;
            }
            OddSocketsWS_SetCallbacks(OnWsOpen, OnWsText, OnWsBinary, OnWsError, OnWsClose);
            _callbacksBound = true;
        }

        [MonoPInvokeCallback(typeof(OpenCallback))]
        private static void OnWsOpen(int instanceId)
        {
            if (Instances.TryGetValue(instanceId, out var ws))
            {
                ws._opened = true;
            }
        }

        [MonoPInvokeCallback(typeof(TextCallback))]
        private static void OnWsText(int instanceId, IntPtr dataPtr, int length)
        {
            if (!Instances.TryGetValue(instanceId, out var ws))
            {
                return;
            }
            var bytes = new byte[length];
            Marshal.Copy(dataPtr, bytes, 0, length);
            ws.EnqueueReceived(bytes, TransportMessageType.Text);
        }

        [MonoPInvokeCallback(typeof(BinaryCallback))]
        private static void OnWsBinary(int instanceId, IntPtr dataPtr, int length)
        {
            if (!Instances.TryGetValue(instanceId, out var ws))
            {
                return;
            }
            var bytes = new byte[length];
            Marshal.Copy(dataPtr, bytes, 0, length);
            ws.EnqueueReceived(bytes, TransportMessageType.Binary);
        }

        [MonoPInvokeCallback(typeof(ErrorCallback))]
        private static void OnWsError(int instanceId)
        {
            if (!Instances.TryGetValue(instanceId, out var ws))
            {
                return;
            }
            // The browser fires onerror without detail and follows with
            // onclose; flag a pending connect here, established sockets are
            // torn down by OnWsClose.
            ws._connectErrored = true;
        }

        [MonoPInvokeCallback(typeof(CloseCallback))]
        private static void OnWsClose(int instanceId, int code, int wasClean)
        {
            if (!Instances.TryGetValue(instanceId, out var ws))
            {
                return;
            }
            ws._closed = true;
        }
    }
}
#endif
