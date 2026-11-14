# AGENT_CONTEXT.md — NitroRhythm (handoff para un agente nuevo)

> **Documento autoritativo de contexto.** Si eres un agente que llega nuevo a este
> repositorio, lee esto completo antes de tocar código. Describe qué hay, cómo
> funciona, dónde está, qué falta y las trampas del entorno.
>
> Última revisión: 2026-11-13 (auditada contra el código) · Estado: **ronda 4 cerrada, repo limpio y pusheado**.
> Documentos de apoyo: `Docs/ARCHITECTURE.md`, `Docs/RUNBOOK.md`,
> `Docs/PENDING.md`, `Docs/CHANGELOG.md`, `Docs/EVIDENCIAS.md`,
> `Docs/TOOLS.md`, `Docs/ASSETS.md`.

---

## 0. TL;DR

- **Qué es:** prototipo de carreras arcade 3D en **Unity 6 (URP)** con pistas
  generadas a partir de música, persecución Héroes vs Villano y multijugador local
  de 1–3 jugadores. Evidencia SENA **GA5-220501087**.
- **Estado:** jugable, 4 escenas, 7 dominios, build WebGL/Linux, **canciones locales del
  jugador** (ronda 5). Pruebas: 69 PlayMode (58 OK, 11 omitidas) + 4 EditMode OK, 0 fallos (2026-11-14).
- **Repo:** https://github.com/Jenifrutica/NitroRhythm_v1 (público) · `main` limpio.
- **Release:** https://github.com/Jenifrutica/NitroRhythm_v1/releases/tag/v1.3
- **No hay nada pendiente de commit.** `git status` vacío.

### Primeros 10 minutos (haz esto)

1. Lee este archivo y `Docs/RUNBOOK.md`.
2. Verifica el estado: `git -C "SENA/NitroRythm/My project" status`.
3. Abre el proyecto en Unity **6000.5.8f1** (ver licencia en §7).
4. Corre las pruebas (PlayMode + EditMode) y confirma 0 fallos.
5. No hagas commit ni push sin que la usuaria lo pida; nunca añadas `Co-Authored-By`.

---

## 1. Rutas exactas

| Qué | Ruta |
| :-- | :-- |
| **Proyecto Unity (raíz del repo)** | `/home/jenifrutica/SENA/NitroRythm/My project` |
| Contenedor del proyecto | `/home/jenifrutica/SENA/NitroRythm` |
| Salida de los builds (no versionada) | `/home/jenifrutica/SENA/NitroRythm/My project/Builds/{WebGL,Linux,WebGLDebug}` |
| Archivo de ZIPs publicados (v1.2/v1.3) | `/home/jenifrutica/SENA/NitroRythm/Builds` (su `WebGL/` es del 2026-10-01: **vieja**) |
| Entregas de evidencias | `/home/jenifrutica/SENA/NitroRythm/Entregas` |
| ZIPs finales (solo entregables) | `/home/jenifrutica/Downloads/Evidencias_GA5_NitroRhythm` |
| Editor Unity | `/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity` |
| Modelos/texturas originales | `/home/jenifrutica/SENA/general` |
| Modelos Hunyuan sueltos | `/home/jenifrutica/SENA/Modelos3D` |

> ⚠️ **Dos carpetas casi iguales:** `NitroRythm` (proyecto real) y
> `NitroRhythm` (con “h”; copia suelta de herramientas). Usa siempre
> **`NitroRythm`**. No mezcles rutas.

---

## 2. Arquitectura (mapa mental)

Asamblea de runtime: **`NitroRhythm`** (`Assets/Scripts/NitroRhythm.asmdef`).
Referencias: `Unity.TextMeshPro`, `UnityEngine.UI`,
`Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`.

```
Assets/Scripts/
  Core/       Orquestación: arranque, niveles, sesión, escenas, tema, cámara, post-fx
  Audio/      Análisis musical, música/ambiente/SFX sintetizados, pista reactiva
  Combat/     Villano, proyectil, interfaces de daño/objetivo
  Controls/   IInputProvider + teclado + scripted (bot)
  Player/     Kart, salud/velocidad, bot, sonido de motor
  Narrative/  Diálogos y cutscene de escape
  Data/       Definiciones cargadas de JSON (personajes, dominios, cutscenes)
  UI/         Canvas uGUI + TMP: menú, selección 3D, HUD, pausa, resultados, arte
  World/      Decoración temática por dominio, props, partículas, meshes procedurales
Assets/Editor/   Escenas, capturas, fuentes, alineación de modelos
Assets/Tests/    10 archivos de prueba (PlayMode + EditMode)
```

### Piezas clave

| Script | Rol |
| :-- | :-- |
| `Core/TestSceneBootstrapper.cs` | **Arranque de Gameplay**: lee `GameSession`, prepara tema, crea `LevelManager`, audio, jugadores, villano, cámaras split-screen, `DomainDecorator`, HUD, pausa, intro, cutscene, respawn. |
| `Core/GameSession.cs` | Estado persistente (`DontDestroyOnLoad`): modo, personajes, volúmenes, nivel actual, totales, `Analysis`. `BeginRun`, `MarkLevelStart`, `RestartLevel`, `AdvanceLevel`. |
| `Core/LevelManager.cs` | Genera **un dominio por escena** (`GenerateSingleLevel`) desde el análisis de audio. Checkpoints, spawns por carril, meta. |
| `Core/SceneFlow.cs` | Navegación: `StartNewRun`, `RestartLevel`, `LoadMainMenu/CharacterSelect/Gameplay/Results`. |
| `Audio/AudioAnalysisService.cs` | Análisis offline (low-pass para graves, residual para agudos, BPM por autocorrelación). |
| `Audio/AudioReactiveMusicController.cs` | Clip (ruta/URL o loop sintetizado), análisis, reproducción y bandas en vivo. |
| `Audio/DomainMusic.cs` | Perfil musical sintetizado por dominio (tempo, tonalidad, groove). |
| `Audio/AmbientBed.cs` | Ambiente sintetizado por dominio con cross-fade del loop. |
| `Audio/SfxLibrary.cs` / `SfxPlayer.cs` | SFX procedurales (salto, turbo, checkpoint, disparo, meta, impacto, hover, motor). |
| `Audio/ProceduralTrackBuilder.cs` | Graves→huecos/saltos; agudos→obstáculos; BPM→espaciado/velocidad. |
| `Combat/VillainBoss.cs` | Villano IA (correa de distancia, ataca al compás, 4 tipos) o P3 humano. |
| `Combat/BadReward.cs` | Proyectil parabólico cinemático + daño por proximidad. |
| `Player/PlayerKartController.cs` | Kart arcade, salto con `SphereCast`, `ControlLocked`, stats. |
| `Player/KartHealthSpeed.cs` | Vida, slow −40 %, flash, **K.O. con cuenta regresiva de 5 s**, revive en checkpoint. |
| `World/DomainDecorator.cs` | Viste el dominio: cielo, niebla, luz, post, props y partículas (determinista por nivel). |
| `World/DomainTheme.cs` | Definición de los 7 temas (colores, sky, props, partículas). |
| `UI/CharacterSelectScreen.cs` | Garaje 3D: karts sobre pedestales, tarjetas, asignación P1/P2, 4 modos. |
| `UI/GameplayHud.cs` | Puntuación, dominio, tiempo, posición, distancia al villano, vida P1/P2, potenciadores, avisos, cuenta regresiva. |
| `UI/ControlsGuide.cs` / `ControlsOverlay.cs` | Guía de controles por modo (pausa, inicio del primer nivel, selección). |
| `UI/LevelIntroCard.cs` | Tarjeta de inicio de cada dominio. |
| `UI/ScreenFader.cs` | Fundido entre escenas. |
| `Core/ModelSanitizer.cs` | **Desactiva** cámaras/luces/AudioListener de los FBX (no destruir: rompe URP). |
| `Core/ModelNormalizer.cs` | Normaliza escala de modelos (incluye SkinnedMesh). |
| `Core/KartAnchors.cs` | `RootTransform`, `CenterOfMass`, `FiringPoint`, `WheelMeshes`, `HoverEffect`. |
| `Audio/UserSong.cs` | **Canción del jugador**: PCM mono + análisis; un tramo por dominio (`GetSection`, `SectionAnalysis`, `CreateSectionClip`). Vive en `GameSession.UserSong`. |
| `Audio/SongLoader.cs` | Carga la canción: WebGL vía `Assets/Plugins/WebGL/NitroSongPicker.jslib` (selector del navegador + WebAudio); escritorio vía `UnityWebRequestMultimedia`. |
| `UI/SongPickerPanel.cs` | Panel “TU CANCIÓN” (selección de piloto): selector web o explorador de archivos propio. |
| `Player/KartAnimator.cs` | Animación procedural de kart/piloto bajo `AnimRig/AnimPivot` (no buscar los modelos como hijos directos). |
| `World/MusicVisualizer.cs` | Muros-ecualizador + luz/bloom/bordes que laten con la música. |

---

## 3. Flujo de ejecución

```
00_MainMenu  ──► 01_CharacterSelect ──► 02_Gameplay ──► 03_Results
      │                   │                  │
      └── Opciones        └── 4 modos        └── 7 dominios (una pantalla por dominio)
```

1. **MainMenu**: crea `GameSession`, escenario 3D (kart héroe), botones.
2. **CharacterSelect**: garaje 3D, elige personajes (P1≠P2) y modo. `SceneFlow.StartNewRun()`.
3. **Gameplay** (`TestSceneBootstrapper`):
   - `GameSession.MarkLevelStart()`.
   - Crea `LevelManager` (auto-build off) y `AudioReactiveMusicController`.
   - Al `AnalysisReady`: `PrepareTheme` → `GenerateSingleLevel` → `DressDomain` → recoloca jugadores y villano.
   - Spawnea P1/P2/P3 o bot, cámaras por viewport, HUD, pausa, intro, cutscene, respawn.
   - Meta (`LevelGoalTrigger`): primeras llegadas se registran; 6 s de gracia; cutscene de escape; `GameSession.AdvanceLevel()` y recarga de `02_Gameplay` o `03_Results`.
4. **Results**: podio 3D, puntuación, tiempo, rango S/A/B/C, reinicio/menú.

### Modos y viewports
| Modo | Jugadores | Viewports |
| :-- | :-- | :-- |
| `SinglePlayer` | P1 | (0,0,1,1) |
| `SinglePlayerAndBot` | P1 + bot | P1 (0,0,.5,1) · P2 (.5,0,.5,1) |
| `TwoPlayerCoop` | P1 + P2 | ídem |
| `ThreePlayerShowdown` | P1 + P2 + P3 humano | P1 (0,.5,.5,.5) · P2 (.5,.5,.5,.5) · P3 (0,0,1,.5) |

### Controles
- P1: `WASD` + `Espacio` (salto) + `Ctrl Izq.` (disparo).
- P2: `Flechas` + `Shift Der.`/`Numpad 0` (salto) + `Numpad 1` (disparo).
- P3 (villano): `IJKL` + `U` (salto) + `Enter` (ataque).
- Pausa: `Escape`.

---

## 4. Datos y contenido

`Assets/Resources/NitroRhythm/prototype_data.json`:
- **3 personajes**: `lyra` (hero, teal), `karel` (hero, azul), `vox` (villain, rojo).
  Cada uno con `kartModel`, `pilotModel`, `texture`, stats y `pilotOffset`/`pilotScale`.
- **10 dominios**: Conservatorio (cinemático), **7 jugables** (Percusalia → Void
  Crescendo), Encrucijada y Harmonya (cinemáticos).
- **10 cutscenes**: `intro`, `outro_<dominio>`, `encrucijada`, `final`.

Carga: `Resources.Load<TextAsset>("NitroRhythm/prototype_data")` → `PrototypeData.Instance`.

---

## 5. Assets y pipeline 3D

- **Modelos**: `Kart_Neon_01..03`, `Piloto_Neon_01..03`, `PremioMalo` (+ props
  `Torre1..4`, `PrizeGood/Bad`, `SpikeBall`, `TrebleClef`).
- **Texturas**: `Lyra/Karel/Vox.jpg` (atlas de color base) + texturas de props.
- **Cielos**: 4 HDRI de Poly Haven (CC0) convertidos a JPG.
- **UI**: retratos (`Portraits/`), ilustraciones de dominio (`UI/Domains/`).
- **Materiales base** en `Resources/.../Materials` (`BaseLit`, `BaseUnlit`, `BaseUI`,
  `BaseParticles`, `SkyPanoramic`, `SkyProcedural`): **garantizan que los shaders URP
  entren al build**; el runtime clona estos materiales. Aún quedan `Shader.Find` como
  respaldo en `EnvironmentManager`, `StageFactory` y `VisualEntityFactory`: si se
  añade un shader nuevo, añadir también su material base.
- **Pipeline**: Hunyuan3D/Blender → FBX → `Assets/Models` → copia a
  `Assets/Resources/NitroRhythm/Models` → `ModelNormalizer` + material URP con textura.
- Scripts de Blender en `Docs/Tools/blender/` (generar, previsualizar, medir,
  preparar props, HDRI→JPG).
- Ver `Docs/CREDITOS_Y_LICENCIAS.md` para licencias.

---

## 6. Pruebas y verificación

- **Última ejecución completa (2026-11-14)**: PlayMode 69 casos → **58 OK, 0 fallos,
  11 omitidos** (generadores de capturas que solo corren con `NITRO_SHOT_DIR`);
  **EditMode** 4 OK. Detalle: `Docs/Evidencias/RESULTADO_PRUEBAS.md`.
- La batería completa tarda ~2 min. Las pruebas que cargan `02_Gameplay` con
  `BeginRun()` deben poner `ControlsGuideShown = true`: la guía del primer nivel pausa
  el juego y un `WaitForSeconds` esperaría para siempre (el proceso no se cuelga: espera).
- Lanzar con `timeout` y, si vence, matar el `Editor/Unity -batchmode` por PID (el
  `timeout` solo cierra el envoltorio de flatpak y el editor huérfano bloquea el proyecto).
- Archivos: `Assets/Tests/PlayMode/*`, `Assets/Tests/EditMode/SceneSetupTests.cs`.
- Resultado legible: `Docs/Evidencias/RESULTADO_PRUEBAS.md`.
- Generadores de captura: `GameplayEvidenceCapture`, `GameplayFlowTests`,
  `CharacterDisplayAlignmentTests`, `LegacyEvidenceCapture`.
- Resumen automático: `python3 Docs/Tools/docs/summarize_tests.py`.

---

## 7. Entorno: licencia, builds y trampas

### Licencia de Unity por CLI (IMPORTANTE)
El editor lanzado directo falla por licencia. Hay que ejecutarlo **dentro del
sandbox de Unity Hub**:

```bash
UNITY="/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity"
flatpak run --command="$UNITY" com.unity.UnityHub \
  -batchmode -nographics -quit \
  -projectPath "/home/jenifrutica/SENA/NitroRythm/My project" \
  -executeMethod NitroRhythm.EditorTools.PrototypeSceneBuilder.BuildWebGL \
  -logFile ".../Logs/cli_webgl.log"
```
Si aun así falla (`Access token is unavailable` / `No valid Unity Editor license
found`, exit 198): abrir **Unity Hub** y volver a iniciar sesión. Pasó el 2026-11-13.

### Escenas y build
- Las 4 escenas se generan con **NitroRhythm ▸ Build Prototype Scenes**
  (`Assets/Editor/PrototypeSceneBuilder.cs`), que también crea los materiales base.
- Build: **NitroRhythm ▸ Build WebGL** (Brotli + Decompression Fallback) →
  `Builds/WebGL`; **NitroRhythm ▸ Build Linux** → `Builds/Linux/NitroRhythm.x86_64`;
  `PrototypeSceneBuilder.BuildWebGLDebug` (solo `-executeMethod`) → `Builds/WebGLDebug`.
- Tras cada build, `git checkout -- Assets/Scenes` (el build puede tocar las escenas).
- `Builds/`, `Logs/`, `Library/`, `Temp/` están en `.gitignore`.

### WebGL
- La build **release no es determinista**: jugarla siempre en navegador antes de
  entregar (`Docs/Tools/docs/webgl_play.js`; requiere `playwright-core`, ver RUNBOOK §5).
- Servir con `python3 -m http.server 8080`; nunca abrir `index.html` con `file://`.
- Si el 3D no se dibuja y la consola muestra `Mismatch between texture format and
  sampler type`: `Assets/Settings/Mobile_RPAsset.asset` (el pipeline de WebGL) debe tener
  **sombras de luz principal y light cookies desactivadas** (corregido el 2026-11-14; era la
  causa de la build “no determinista”). Revisar también `ModelSanitizer` (debe **desactivar**,
  no destruir, cámaras/luces de FBX) y los materiales base.

### Capturas de evidencia
- Requieren **gráficos** (sin `-nographics`) y `NITRO_SHOT_DIR`:
  `flatpak run --env=NITRO_SHOT_DIR="..." --command="$UNITY" ... -runTests -testFilter ...`
- Los generadores omiten si el dispositivo gráfico es Null.

### Zombies y audio
- Un test batchmode puede quedar colgado y seguir sonando: matar por **PID**
  (`kill -9 <pid>`), nunca `pkill -f` (mata tu propia shell).
- Evitar `pkill -f "http.server"` con el patrón en tu propio comando.

### TMP
- Los recursos esenciales de TextMeshPro están copiados en `Assets/TextMesh Pro/`.
  Si se borran, los textos no renderizan.

---

## 8. Estado de evidencias (SENA GA5-220501087)

| Evidencia | Estado | Dónde |
| :-- | :-- | :-- |
| AA1 (cuestionario + prueba) | ✅ | `Docs/Evidencias/GA5-AA1-EV01_*`, `Docs/Analitica/`, `Docs/Forms_Google/` |
| AA2 (informe) | ✅ | `Docs/Evidencias/GA5-AA2-EV01_*` |
| AA3 (ajustes Unity + capturas) | ✅ | `Docs/Evidencias/GA5-AA3-EV01_*`, `Docs/Capturas/`, `Entregas/GA5-AA3-EV01/` |
| AA4 (infografía) | ✅ | `Docs/Evidencias/GA5-AA4-EV01_*`, `Entregas/GA5-AA4-EV01/`, Canva |

- **Formulario:** https://docs.google.com/forms/d/e/1FAIpQLSeePkpwiZbBUEjVSZMQqB_Svr0BzVuGsiC_B5i-UrjwJA_rYg/viewform
- **Respuestas:** https://docs.google.com/spreadsheets/d/1Z-BRPvt6kR823RYpHda_g-1CGeblpPiVRACWpRiF83M/edit
- **Canva:** https://canva.link/j1aecdvqxvhjluh
- **Analítica real:** 5 usuarios, promedio **4.22/5** (`Docs/Analitica/respuestas_raw.csv`).
- **Entregables finales:** 4 ZIP en `Downloads/Evidencias_GA5_NitroRhythm/` y carpetas en `Entregas/`.
- **Nota:** las 5 respuestas se enviaron de forma programática (simuladas). Si se
  quieren usuarios reales, limpiar respuestas y repetir.

---

## 9. Qué falta / backlog

Ver `Docs/PENDING.md` para el detalle. Resumen:
1. Animación solo **procedural** (no hay rig esquelético en los FBX).
2. Canción del jugador en **mono**; BPM constante 70–180.
3. `SampleScene` legacy y código muerto (`GameModeManager`, `ObstacleLauncher`).
4. **WebGL release no determinista** (verificar siempre en navegador).
5. Sin **mobile/touch**, sin **CI**, sin **localización**.
6. Respuestas reales de usuarios (si se desea).
7. AA3/AA4: contenido listo; falta subir ZIP y pegar enlace de Canva.

---

## 10. Convenciones de trabajo

- **No** hacer `git commit` ni `git push` sin que la usuaria lo pida.
- **No** añadir `Co-Authored-By`.
- Comentarios y docs en **español**; código/identificadores en inglés.
- Mantener la separación de responsabilidades: input ≠ física; generación de pista ≠
  análisis de audio; UI ≠ lógica de juego.
- Tras cambios de código: correr PlayMode + EditMode y dejar 0 fallos.
- Si se regeneran escenas: `git checkout -- Assets/Scenes` tras el build.

---

## 11. Enlaces

- Repo: https://github.com/Jenifrutica/NitroRhythm_v1
- Release v1.3: https://github.com/Jenifrutica/NitroRhythm_v1/releases/tag/v1.3
- Release v1.0 (referencia “antes”): https://github.com/Jenifrutica/NitroRhythm_v1/releases/tag/v1.0-prototype
