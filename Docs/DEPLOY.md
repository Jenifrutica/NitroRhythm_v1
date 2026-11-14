# DEPLOY.md — Cómo publicar NitroRhythm

Las builds salen de un solo comando (Linux, Windows, macOS y WebGL):

```bash
PROJECT="/home/jenifrutica/SENA/NitroRythm/My project"
UNITY="/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity"
flatpak run --command="$UNITY" com.unity.UnityHub -batchmode -nographics -quit \
  -projectPath "$PROJECT" \
  -executeMethod NitroRhythm.EditorTools.PrototypeSceneBuilder.BuildEverything \
  -logFile "$PROJECT/Logs/cli_build_all.log"
git -C "$PROJECT" checkout -- Assets/Scenes Data/Plugins/lib_burst_generated.wasm
```

Salida en `$PROJECT/Builds/`: `Linux/`, `Windows/`, `macOS/NitroRhythm.app`, `WebGL/`.
(Menús equivalentes: **NitroRhythm ▸ Build Linux / Windows / macOS / WebGL / Everything**.)

> Los módulos de Windows y macOS se instalaron a mano el 2026-11-14 (el Hub por CLI no
> reconoce el editor): se descargan los `.pkg` de
> `https://download.unity3d.com/download_unity/5cb7df797b7d/MacEditorTargetInstaller/UnitySetup-{Windows,Mac}-Mono-Support-for-Editor-6000.5.8f1.pkg`,
> se extraen con `7z x` + `gzip -dc Payload | cpio -idm` y el contenido va a
> `Editor/Data/PlaybackEngines/{WindowsStandaloneSupport,MacStandaloneSupport}`.

---

## 1. Juego en el navegador (WebGL) — lo más fácil de compartir

La build usa **Brotli con “decompression fallback”**: funciona en cualquier hosting estático,
sin configurar cabeceras. Antes de publicar, **jugarla** con `Docs/Tools/docs/webgl_song_test.js`.

### Opción A — GitHub Pages (gratis, mismo repositorio)
1. Crear una rama `gh-pages` solo con el contenido de `Builds/WebGL/`:
   ```bash
   cd "$PROJECT/Builds/WebGL"
   git init -b gh-pages && git add -A && git commit -m "web build"
   git remote add origin https://github.com/Jenifrutica/NitroRhythm_v1.git
   git push -f origin gh-pages
   ```
2. GitHub → *Settings ▸ Pages* → *Branch: gh-pages / root*.
3. Queda en **https://jenifrutica.github.io/NitroRhythm_v1/** (tarda 1–2 min).

### Opción B — itch.io (página de juego, comentarios, estadísticas)
1. Crear proyecto → *Kind of project: HTML* → subir `NitroRhythm_WebGL_vX.zip`
   (el `index.html` debe quedar en la raíz del zip) → marcar *This file will be played in the browser*.
2. *Embed options*: 1280×720 y botón de pantalla completa.
3. Se puede dejar como *Draft*, *Restricted* (con contraseña para el instructor) o *Public*.

### Opción C — Netlify / Cloudflare Pages
Arrastrar la carpeta `Builds/WebGL` a *app.netlify.com/drop*. Sin configuración.

---

## 2. Ejecutables de escritorio

| Plataforma | Qué se entrega | Cómo se abre |
| :-- | :-- | :-- |
| **Windows 64** | `NitroRhythm_Windows_vX.zip` (carpeta con `NitroRhythm.exe` + `_Data`) | Descomprimir y doble clic en `NitroRhythm.exe`. SmartScreen puede avisar (“Más información ▸ Ejecutar de todas formas”): el exe no está firmado. |
| **Linux 64** | `NitroRhythm_Linux_vX.zip` (`NitroRhythm.x86_64` + `_Data`) | `chmod +x NitroRhythm.x86_64 && ./NitroRhythm.x86_64` |
| **macOS** (Intel y Apple Silicon) | `NitroRhythm_macOS_vX.zip` (`NitroRhythm.app`) | No está firmada ni notarizada: clic derecho ▸ *Abrir* la primera vez, o `xattr -dr com.apple.quarantine NitroRhythm.app`. |

### Dónde publicarlos
- **GitHub Releases** (lo que usa el proyecto): `gh release create vX ...zip --title ... --notes ...`.
  Cada zip debe pesar menos de 2 GB (hoy ~30–60 MB).
- **itch.io**: subir cada zip marcando su plataforma (Windows/Linux/Mac); con `butler`
  se pueden publicar versiones nuevas desde la terminal:
  `butler push NitroRhythm_Windows_vX.zip usuario/nitrorhythm:windows`.

---

## 3. Recomendación

1. **Web (GitHub Pages o itch.io)** como enlace principal: nadie instala nada y la función
   *TU CANCIÓN* usa el selector del navegador.
2. **GitHub Release** con los tres ejecutables para quien quiera jugar sin conexión o con
   mejor rendimiento (y para la evidencia SENA).
3. Si se quiere móvil: falta el módulo Android (+ SDK/NDK/JDK) y controles táctiles
   (ver `PENDING.md`).
