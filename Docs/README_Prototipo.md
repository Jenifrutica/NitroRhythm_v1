# NitroRhythm — Prototipo funcional (SENA)

Prototipo de carreras arcade 3D con generación de pistas reactiva al audio,
persecución asimétrica Héroes vs Villano y multijugador local de 1 a 3 jugadores.

- **Motor:** Unity **6000.5.8f1** (URP 17).
- **Carpeta del proyecto:** `SENA/NitroRythm/My project`
- **Escenas (4):** `00_MainMenu`, `01_CharacterSelect`, `02_Gameplay`, `03_Results`

## 1. Requisitos

1. Unity Hub con el editor **6000.5.8f1** (o superior de la línea 6000.5).
2. Licencia de Unity activada (Personal). Si el editor pide licencia, **inicie
   sesión en Unity Hub** una vez.
3. Blender (opcional, solo para regenerar modelos 3D).

## 2. Cómo abrir y ejecutar

1. Abra Unity Hub → **Open** → carpeta `SENA/NitroRythm/My project`.
2. Espere a que importe (la primera vez tarda unos minutos).
3. Las 4 escenas **ya están generadas** en `Assets/Scenes`. Si hace cambios y
   quiere recrearlas, use **NitroRhythm ▸ Build Prototype Scenes**.
4. Abra `Assets/Scenes/00_MainMenu.unity` y pulse **Play**.

> Si las escenas ya existen se pueden abrir directamente sin volver a generarlas.

## 3. Controles

| Jugador | Conducir | Salto | Disparo |
| :--- | :--- | :--- | :--- |
| P1 (Cian / Lyra) | `W A S D` | `Espacio` | `Ctrl Izq.` |
| P2 (Azul / Karel) | `Flechas` | `Shift Der.` / `Numpad 0` | `Numpad 1` |
| P3 (Villano / Vox) | `I J K L` | `Shift Der.` / `Numpad 0` | `Enter` |
| Pausa | — | `Escape` | — |

## 4. Modos de juego

1. **1 Jugador** — P1 a pantalla completa.
2. **1 Jugador + Bot** — pantalla dividida 50/50 con IA aliada.
3. **Cooperativo 2P** — pantalla dividida 50/50.
4. **Showdown 3P** — P1 arriba-izq., P2 arriba-der., Villano abajo (pantalla completa).

## 5. Generación reactiva al audio

- `AudioAnalysisService` analiza el clip (FFT ligera offline): BPM, energía,
  graves y agudos por pulso.
- `ProceduralTrackBuilder` traduce:
  - **Graves (bombos/sub-bass)** → huecos, saltos y rampas.
  - **Agudos (hi-hats/sintes)** → barreras giratorias, hazards móviles y pads.
  - **BPM** → separación, ancho de plataforma y velocidad base.
  - La **cadencia de disparo del villano** se sincroniza con el BPM (2 disparos por pulso).
- Si no se carga un audio externo, se usa un **loop sintetizado** (`MusicGenerator`)
  para que la demo funcione siempre, incluido WebGL.
- Para usar un audio propio: defina `GameSession.AudioTrackPath` (URL o ruta).

## 6. Compilar para WebGL (compartir con 5 usuarios)

> Ya generado: la build está en **`SENA/NitroRythm/Builds/WebGL/`** y el paquete
> listo para subir es **`SENA/NitroRythm/Builds/NitroRhythm_WebGL.zip`**.

Para regenerarla: menú **NitroRhythm ▸ Build WebGL** (produce
`My project/Builds/WebGL`; cópiela a `SENA/NitroRythm/Builds`). Suba el .zip a un
hosting (itch.io, Netlify Drop, GitHub Pages o AWS S3 + CloudFront con su
dominio) y comparta el enlace.

## 7. Herramienta de evidencias

Menú **NitroRhythm ▸ Evidence Capture Tool**:

- Abre cada escena.
- Captura el *Game View* (1920×1080) en carpetas **Antes** y **Despues**
  (`SENA/NitroRythm/Evidencias/`), para las evidencias AA3 y AA4.

## 8. Regenerar los modelos 3D (opcional)

```bash
flatpak run org.blender.Blender --background \
  --python "SENA/NitroRhythm/Tools/blender/generate_karts.py"
```

Genera `Kart_Neon_01..03`, `Piloto_Neon_01..03` y `PremioMalo.fbx` en
`Assets/Models` y `Assets/Resources/NitroRhythm/Models`.

## 9. Estructura de scripts

```
Assets/Scripts/
  Audio/     Análisis musical, generador de loop, plan de pista reactiva
  Combat/    Villano, proyectil PremioMalo, interfaces de daño/objetivo
  Controls/  IInputProvider + teclado + scripted (bot)
  Core/      LevelManager, GameSession, SceneFlow, factoría visual, cámaras, temas
  Data/      Definiciones cargadas desde Resources/NitroRhythm/prototype_data.json
  Narrative/ DialogueSystem + CutsceneController (data-driven)
  Player/    PlayerKartController, salud/velocidad, bot
  UI/        UIFactory + menú, selección, opciones, pausa, HUD, resultados
Assets/Editor/  PrototypeSceneBuilder + EvidenceCaptureTool
```
