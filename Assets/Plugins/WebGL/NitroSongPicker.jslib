// Selector de canciones locales para la build WebGL de NitroRhythm.
// El navegador decodifica el archivo (MP3/WAV/OGG/M4A/FLAC según el navegador), lo mezcla a mono
// y lo remuestrea con un OfflineAudioContext; C# consulta el estado cada frame (sin SendMessage)
// y copia las muestras al heap con NitroSong_Copy. Estados: 0 inactivo, 1 eligiendo,
// 2 decodificando, 3 listo, -1 error, -2 cancelado.
var NitroSongPicker = {
  $NitroSong: { state: 0, name: "", error: "", rate: 32000, samples: null },

  NitroSong_Open: function (maxSeconds, sampleRate) {
    NitroSong.state = 1;
    NitroSong.name = "";
    NitroSong.error = "";
    NitroSong.samples = null;
    NitroSong.rate = sampleRate;

    var input = document.createElement("input");
    input.type = "file";
    input.accept = "audio/*,.mp3,.wav,.ogg,.oga,.m4a,.aac,.flac,.opus,.webm";
    input.style.display = "none";
    document.body.appendChild(input);

    var done = false;
    var cleanup = function () { if (input.parentNode) input.parentNode.removeChild(input); };
    var fail = function (e) { NitroSong.state = -1; NitroSong.error = String((e && e.message) || e || "error"); };

    input.addEventListener("cancel", function () { if (!done) { done = true; NitroSong.state = -2; cleanup(); } });
    input.addEventListener("change", function () {
      done = true;
      var file = input.files && input.files[0];
      cleanup();
      if (!file) { NitroSong.state = -2; return; }
      NitroSong.name = file.name;
      NitroSong.state = 2;

      var readBuffer = file.arrayBuffer ? file.arrayBuffer() : new Promise(function (resolve, reject) {
        var reader = new FileReader();
        reader.onload = function () { resolve(reader.result); };
        reader.onerror = function () { reject(reader.error); };
        reader.readAsArrayBuffer(file);
      });

      readBuffer.then(function (buffer) {
        var decoder = new OfflineAudioContext(1, 1, 44100);
        return new Promise(function (resolve, reject) { decoder.decodeAudioData(buffer, resolve, reject); });
      }).then(function (decoded) {
        var seconds = Math.min(decoded.duration, maxSeconds);
        var length = Math.max(1, Math.ceil(seconds * sampleRate));
        var offline = new OfflineAudioContext(1, length, sampleRate);   // 1 canal = mezcla a mono
        var source = offline.createBufferSource();
        source.buffer = decoded;
        source.connect(offline.destination);
        source.start(0);
        return offline.startRendering();
      }).then(function (rendered) {
        NitroSong.samples = rendered.getChannelData(0);
        NitroSong.state = 3;
      }).catch(function (e) {
        fail(e && e.name === "EncodingError" ? "formato no soportado por este navegador" : e);
      });
    });

    // Los navegadores solo abren el selector durante un gesto del usuario. El clic de Unity llega
    // pocos milisegundos después del evento real; si la activación ya caducó, se abre en el
    // siguiente clic sobre el canvas.
    var open = function () { try { input.click(); return true; } catch (e) { return false; } };
    var active = !navigator.userActivation || navigator.userActivation.isActive;
    if (!active || !open()) {
      var canvas = (typeof Module !== "undefined" && Module.canvas) || document.querySelector("canvas") || document;
      var once = function () { canvas.removeEventListener("pointerup", once, true); if (!done) open(); };
      canvas.addEventListener("pointerup", once, true);
    }
  },

  NitroSong_State: function () { return NitroSong.state; },

  NitroSong_SampleCount: function () { return NitroSong.samples ? NitroSong.samples.length : 0; },

  NitroSong_SampleRate: function () { return NitroSong.rate; },

  NitroSong_Name: function () {
    var s = NitroSong.name || "";
    var size = lengthBytesUTF8(s) + 1;
    var buffer = _malloc(size);
    stringToUTF8(s, buffer, size);
    return buffer;
  },

  NitroSong_Error: function () {
    var s = NitroSong.error || "";
    var size = lengthBytesUTF8(s) + 1;
    var buffer = _malloc(size);
    stringToUTF8(s, buffer, size);
    return buffer;
  },

  NitroSong_Copy: function (bufferPtr, length) {
    if (!NitroSong.samples) return;
    var n = Math.min(length, NitroSong.samples.length);
    HEAPF32.set(NitroSong.samples.subarray(0, n), bufferPtr >> 2);
  },

  NitroSong_Reset: function () {
    NitroSong.state = 0;
    NitroSong.samples = null;
    NitroSong.name = "";
    NitroSong.error = "";
  }
};

autoAddDeps(NitroSongPicker, "$NitroSong");
mergeInto(LibraryManager.library, NitroSongPicker);
