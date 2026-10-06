// OddSockets WebGL WebSocket bridge (FEAT-2026-1006-0012).
// Backs SocketIOClient.Transport.WebSockets.WebGLClientWebSocket with the
// browser's native WebSocket API - System.Net sockets do not exist in the
// WebGL sandbox.
var OddSocketsWebSocketLibrary = {
  $OSWS: {
    instances: {},
    nextId: 1,
    onOpen: 0,
    onText: 0,
    onBinary: 0,
    onError: 0,
    onClose: 0
  },

  OddSocketsWS_SetCallbacks: function (onOpen, onText, onBinary, onError, onClose) {
    OSWS.onOpen = onOpen;
    OSWS.onText = onText;
    OSWS.onBinary = onBinary;
    OSWS.onError = onError;
    OSWS.onClose = onClose;
  },

  OddSocketsWS_Create: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    var socket;
    try {
      socket = new WebSocket(url);
    } catch (e) {
      return -1;
    }
    var id = OSWS.nextId++;
    socket.binaryType = 'arraybuffer';
    socket.onopen = function () {
      {{{ makeDynCall('vi', 'OSWS.onOpen') }}}(id);
    };
    socket.onmessage = function (ev) {
      if (typeof ev.data === 'string') {
        var len = lengthBytesUTF8(ev.data);
        var ptr = _malloc(len + 1);
        stringToUTF8(ev.data, ptr, len + 1);
        {{{ makeDynCall('viii', 'OSWS.onText') }}}(id, ptr, len);
        _free(ptr);
      } else {
        var bytes = new Uint8Array(ev.data);
        var ptr = _malloc(bytes.length);
        HEAPU8.set(bytes, ptr);
        {{{ makeDynCall('viii', 'OSWS.onBinary') }}}(id, ptr, bytes.length);
        _free(ptr);
      }
    };
    socket.onerror = function () {
      {{{ makeDynCall('vi', 'OSWS.onError') }}}(id);
    };
    socket.onclose = function (ev) {
      {{{ makeDynCall('viii', 'OSWS.onClose') }}}(id, ev.code, ev.wasClean ? 1 : 0);
    };
    OSWS.instances[id] = socket;
    return id;
  },

  OddSocketsWS_State: function (id) {
    var socket = OSWS.instances[id];
    return socket ? socket.readyState : 3;
  },

  OddSocketsWS_SendText: function (id, textPtr) {
    var socket = OSWS.instances[id];
    if (socket && socket.readyState === 1) {
      socket.send(UTF8ToString(textPtr));
    }
  },

  OddSocketsWS_SendBinary: function (id, dataPtr, length) {
    var socket = OSWS.instances[id];
    if (socket && socket.readyState === 1) {
      socket.send(HEAPU8.buffer.slice(dataPtr, dataPtr + length));
    }
  },

  OddSocketsWS_Close: function (id, code, reasonPtr) {
    var socket = OSWS.instances[id];
    if (socket) {
      try { socket.close(code, UTF8ToString(reasonPtr)); } catch (e) { }
    }
  },

  OddSocketsWS_Destroy: function (id) {
    var socket = OSWS.instances[id];
    if (socket) {
      socket.onopen = socket.onmessage = socket.onerror = socket.onclose = null;
      try { socket.close(); } catch (e) { }
      delete OSWS.instances[id];
    }
  }
};

autoAddDeps(OddSocketsWebSocketLibrary, '$OSWS');
mergeInto(LibraryManager.library, OddSocketsWebSocketLibrary);
