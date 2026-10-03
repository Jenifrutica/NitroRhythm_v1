# CONTEXTO_PROYECTO.md — NitroRhythm

> **Documento de traspaso de contexto.** Resume absolutamente todo el estado del
> proyecto para continuar el desarrollo en una sesión nueva, sin perder información.
> Última actualización: sesión de desarrollo en `opencode`.

---

## 0. Resumen ejecutivo y rutas clave

**NitroRhythm** es un prototipo de carreras arcade 3D en **Unity 6 (URP)** con
generación de pistas **reactiva al audio**, persecución asimétrica Héroes vs
Villano y **multijugador local de 1 a 3 jugadores**. Es la evidencia del programa
SENA "Tecnología en Desarrollo de Videojuegos y Entornos Interactivos"
(GA5-220501087, ficha 3336511).

| Qué | Ruta absoluta |
| :-- | :-- |
| **Proyecto Unity (raíz del repo)** | `/home/jenifrutica/SENA/NitroRythm/My project` |
| Carpeta contenedora | `/home/jenifrutica/SENA/NitroRythm` |
| Build WebGL | `/home/jenifrutica/SENA/NitroRythm/Builds/WebGL` |
| Zip de la build | `/home/jenifrutica/SENA/NitroRythm/Builds/NitroRhythm_WebGL.zip` |
| Evidencias (fuera del repo) | `/home/jenifrutica/SENA/NitroRythm/Evidencias` |
| Herramientas (Blender/docs) | `/home/jenifrutica/SENA/NitroRythm/Tools` |
| **Repositorio GitHub** | `https://github.com/Jenifrutica/NitroRhythm_v1` (público) |
| **Release con build** | `https://github.com/Jenifrutica/NitroRhythm_v1/releases/tag/v1.0-prototype` |
| Editor Unity | `/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity` |
| Modelos originales | `/home/jenifrutica/SENA/general/` |
| Modelos Hunyuan sueltos | `/home/jenifrutica/SENA/Modelos3D/` |

> ⚠️ **Ojo con el nombre de la carpeta:** el proyecto está en `NitroRythm`
> (sin la primera "h"), pero existe otra carpeta `/home/jenifrutica/SENA/NitroRhythm`
> (con "h") que contiene una copia de `Tools/blender`. No confundirlas.

---

## 1. Arquitectura y estado actual de la lógica del juego

Todo el código vive en `Assets/Scripts/` dentro de la asamblea **`NitroRhythm`**
(hay un `.asmdef` en `Assets/Scripts/NitroRhythm.asmdef`). El editor está en
`Assets/Editor/` (asamblea `NitroRhythm.Editor`) y las pruebas en `Assets/Tests/`.

### 1.1 Core (`Assets/Scripts/Core/`)
| Script | Responsabilidad |
| :-- | :-- |
| `TestSceneBootstrapper.cs` | **Arranque de la escena Gameplay.** Lee `GameSession`, crea `LevelManager`, `AudioReactiveMusicController`, jugadores (coloreados y texturizados por personaje), villano, cámaras split-screen, `EnvironmentManager`, `CutsceneController`, `DialogueSystem`, `GameplayHud`, `PauseMenu` y `RespawnOnFall`. |
| `LevelManager.cs` | Genera la pista procedural de 7 dominios jugables. Dos modos: `BuildTrack()` (fallback lineal) y **`GenerateFromAudio(analysis, levelCount, startX)`** (reactivo). Gestiona checkpoints (inicio + intermedios vía `ActivateCheckpoint`), `CompleteLevel`, `RepositionPlayersToCheckpoint`, registro de jugadores (`Players`). |
| `GameSession.cs` | Estado persistente entre escenas (`DontDestroyOnLoad`): modo, personajes P1/P2/P3, volumen música/SFX, calibración, `AudioTrackPath`, `Analysis`, `LastScore`. `EnsureExists()`. |
| `SceneFlow.cs` | Nombres y carga de escenas (`00_MainMenu`, `01_CharacterSelect`, `02_Gameplay`, `03_Results`). |
| `VisualEntityFactory.cs` | Crea karts (cubo placeholder + modelo FBX), materiales URP (`CreateSolidMaterial`, `CreateEmissiveMaterial`, `CreateNeonMaterial`, `CreateModelMaterial`), carga texturas y modelos desde `Resources`. Oculta el renderer del cubo y añade `KartAnchors`. |
| `KartAnchors.cs` | Anclajes modulares: `RootTransform`, `CenterOfMass`, `FiringPoint`, `WheelMeshes`, `HoverEffect`. |
| `ModelNormalizer.cs` | **Normaliza la escala** de cualquier modelo (Mesh o **SkinnedMesh**) a un `TargetSize` mundial, independiente de la escala del padre. Clave para modelos Hunyuan de escala arbitraria. |
| `NeonPostFx.cs` | Crea un `Volume` global URP con **Bloom, Vignette, Chromatic Aberration, Color Adjustments y ACES**, y activa post-proceso en todas las cámaras. `Ensure()`. |
| `StageFactory.cs` | Escenario 3D: suelo con **textura de rejilla neón** (`CreateFloor`), luces de contorno (`AddRimLight`), luz clave (`AddKeyLight`), niebla y ambiente. |
| `EnvironmentManager.cs` | Tema por dominio (cielo/niebla) leído de `PrototypeData` y pitch de música. |
| `CameraFollow.cs` | Cámara de seguimiento suave (offset local + `SmoothDamp`). |
| `LevelGoalTrigger.cs` | Meta de nivel; exige que **todos** los jugadores entren. Expone `CurrentWaitingMessage` para el HUD. |
| `CheckpointComponent.cs` | Trigger de checkpoint intermedio. |
| `RespawnOnFall.cs` | Reaparece jugadores/villano al caer bajo Y = −10. |
| `SpinningBarComponent.cs`, `MovingHazardComponent.cs`, `SpeedPadComponent.cs`, `BouncingPadComponent.cs` | Obstáculos y pads. |
| `GameModeManager.cs` | **Solo se conserva por el enum `GameMode`**. Su lógica de menú quedó obsoleta (la UI es por escenas). |
| `VisualEntityFactory`, `StageFactory`, `NeonPostFx` | Ver arriba. |

### 1.2 Audio (`Assets/Scripts/Audio/`)
| Script | Responsabilidad |
| :-- | :-- |
| `AudioAnalysisResult.cs` | Resultado serializable: `bpm`, `beatCount`, `beatEnergy`, `bassEnergy`, `trebleEnergy`. `CreateFlat()` genera datos sintéticos para pruebas. |
| `AudioAnalysisService.cs` | Análisis **offline sin FFT**: separa graves (low-pass one-pole) y agudos (residual), estima BPM por autocorrelación de onsets y agrega energía por pulso. |
| `MusicGenerator.cs` | **Loop sintetizado** (kick + sub-bass + hats + arpegio) de respaldo para que la demo funcione siempre (incluido WebGL). |
| `AudioReactiveMusicController.cs` | Carga el clip (URL/ruta con `UnityWebRequestMultimedia` o el loop generado), pre-analiza, reproduce y expone `Bass`/`Treble`/`Energy` en vivo. Evento `AnalysisReady`. |
| `ProceduralTrackPlan.cs` | `TrackSegment` (longitud, hueco, pads, tipo de obstáculo) y `ProceduralTrackPlan`. |
| `ProceduralTrackBuilder.cs` | Traduce el análisis a pista: **graves → huecos/saltos/rampas**, **agudos → obstáculos/pads**, **BPM → espaciado/velocidad**. |

### 1.3 Combate (`Assets/Scripts/Combat/`)
| Script | Responsabilidad |
| :-- | :-- |
| `VillainBoss.cs` | Villano IA (corre por delante y dispara al jugador más cercano) o controlado por P3 (`IJKL` + `Enter`). Cadencia de disparo **sincronizada al BPM**. Usa `KartAnchors.FiringPoint`. |
| `BadReward.cs` | Proyectil **"PremioMalo"**: vuelo cinemático parabólico (`Lerp` + `Sin`), aterriza, reactiva física/collider y se destruye a los 6 s. Daño por proximidad a jugadores. |
| `ObstacleLauncher.cs` | Lanzador genérico (legacy, **no está enganchado al villano**). |
| `IDamageable.cs`, `ITargetSelectable.cs` | Interfaces. |

### 1.4 Controles (`Assets/Scripts/Controls/`)
- `IInputProvider.cs` (abstracción), `KeyboardInputProvider.cs` (P1 WASD, P2 flechas, P3 IJKL),
  `ScriptedInputProvider.cs` (bot/eventos). `PlayerKartController` consume un `IInputProvider`.

### 1.5 Jugador (`Assets/Scripts/Player/`)
| Script | Responsabilidad |
| :-- | :-- |
| `PlayerKartController.cs` | Kart arcade: `Rigidbody`, gravedad custom, salto con `SphereCast`, `Continuous` collision, `ApplyCharacterStats(speed,grip,power)`, boost temporal, implementa `IDamageable`/`ITargetSelectable`. |
| `KartHealthSpeed.cs` | Vida, penalización −40 % de velocidad 3 s y flash rojo por viewport. |
| `BotKartAI.cs` | Bot que conduce y salta obstáculos. |

### 1.6 Narrativa (`Assets/Scripts/Narrative/`)
- `DialogueSystem.cs`: tarjeta de cómic (Canvas + TMP) que consume `CutsceneDefinition`.
- `CutsceneController.cs`: al cruzar la meta congela el tiempo, muestra el diálogo, anima al villano escapando por un portal y avanza de nivel (o carga `03_Results`).

### 1.7 Datos (`Assets/Scripts/Data/`)
- `PrototypeData.cs` (raíz del JSON, `Instance`/`Load`, `PlayableLevels`), `CharacterDefinition.cs`,
  `LevelDefinition.cs`, `CutsceneDefinition.cs`. Se cargan con `Resources.Load<TextAsset>("NitroRhythm/prototype_data")`.

### 1.8 UI (`Assets/Scripts/UI/`)
| Script | Responsabilidad |
| :-- | :-- |
| `UIFactory.cs` | Factoría de Canvas uGUI + TextMeshPro: sprites **redondeado/gradiente/círculo**, paneles, tarjetas, botones, sliders, gauge radial. Fuente TMP con fallback. |
| `UIHoverEffect.cs` | Feedback de hover (escala + glow) con tiempo no escalado. |
| `MainMenuScreen.cs` | Menú con **stage 3D** (kart héroe girando) y botones. |
| `OptionsPanel.cs` | Sliders de volumen música/SFX y calibración. |
| `CharacterSelectScreen.cs` | **Garaje 3D**: 3 karts sobre pedestales + tarjetas con stats y asignación P1/P2 + 4 modos. |
| `CharacterDisplay.cs` | Showcase 3D de un personaje (kart + piloto, pedestal, luz, rotación, selección, clic). |
| `GameplayHud.cs` | HUD: puntuación, dominio, velocímetro radial, barra de progreso, vida y mensaje de espera; pulso al ritmo del bajo. |
| `PauseMenu.cs` | Pausa (Escape): reanudar / reiniciar / menú. |
| `ResultsScreen.cs` | Podio 3D + puntuación y resumen. |

### 1.9 Flujo de escenas
`00_MainMenu` → `01_CharacterSelect` → `02_Gameplay` → (al ganar) `03_Results`.
`GameSession` persiste entre todas. El modo y los personajes se eligen en la selección.

### 1.10 Estado de las mecánicas
- ✅ Movimiento arcade, salto, pads, rampas, obstáculos, checkpoints y reaparición.
- ✅ 4 modos con viewports exactos.
- ✅ Combate del villano (parábola + daño + penalización + flash).
- ✅ Pista reactiva al audio y cadencia de disparo por BPM.
- ✅ Narrativa data-driven con 10 dominios y cutscene de escape.
- ✅ UI Canvas/TMP completa y escenario 3D neón.
- ✅ Modelos texturizados integrados.

---

## 2. Rutas y tipos de recursos/assets

### 2.1 Dentro del proyecto (`Assets/Resources/NitroRhythm/`)
| Recurso | Tipo | Notas |
| :-- | :-- | :-- |
| `prototype_data.json` | TextAsset (JSON) | Personajes, 10 dominios y cutscenes. **Editable sin recompilar.** |
| `Models/Kart_Neon_01..03.fbx` | Modelo FBX | Karts originales Hunyuan/Blender (Lyra/Karel/Vox). |
| `Models/Piloto_Neon_01..03.fbx` | Modelo FBX | Pilotos riggeados (SkinnedMesh). |
| `Models/PremioMalo.fbx` | Modelo FBX | Proyectil (generado por script). |
| `Textures/Lyra.jpg`, `Karel.jpg`, `Vox.jpg` | Textura | Mapas de color base (atlás UV) recuperados de `SENA/general`. |
| `Materials/BaseLit.mat`, `BaseUnlit.mat`, `BaseUI.mat` | Material | **Garantizan que los shaders URP entren al build** (evitan magenta). Los crea `PrototypeSceneBuilder.EnsureBaseMaterials()`. |
| `Shaders/` | (carpeta vacía) | Reservada. |

### 2.2 Fuera del proyecto
- `Assets/Models/` (copia de los FBX, no usada en runtime).
- `Assets/Scenes/`: `00_MainMenu`, `01_CharacterSelect`, `02_Gameplay`, `03_Results`, `SampleScene` (legacy, se puede borrar).
- `Assets/Editor/`: `PrototypeSceneBuilder.cs` (genera las 4 escenas + build WebGL + materiales base), `EvidenceCaptureTool.cs` (capturas Antes/Después).
- `Assets/Tests/`: `PlayMode/PrototypePlayModeTests.cs` (10), `EditMode/SceneSetupTests.cs` (4).
- `Docs/` (dentro del repo): evidencias, analítica, capturas y scripts.
- **Originales del usuario** (fuente de modelos): `/home/jenifrutica/SENA/general/` →
  `Kart_Neon_01..03.fbx`, `Piloto_Neon_01..03.fbx`, `KartLyra.blend`, `karelKart.blend`,
  `VoxKart.blend`, `voxKartv2.blend`, `textlirrrrrrrrrrr.jpeg`, `textkar.jpeg`, `textVoxx.jpeg`.
- **Hunyuan sueltos**: `/home/jenifrutica/SENA/Modelos3D/` (muchos `.glb`/`.fbx` de 26–99 MB; **sin clasificar**).

### 2.3 Datos narrativos (`prototype_data.json`)
- 3 personajes: `lyra` (Lyra Pulse, teal `#33CCFF`), `karel` (Karel Volt, azul `#0022B2`),
  `vox` (Vox Null, rojo `#FF0000`, villano).
- 10 dominios: Conservatorio Armónico (cinemático), Percusalia, Echoris, Bassline Abyss,
  Arpeggion, Treble Spire, Noctua Chord, Void Crescendo (7 jugables), Encrucijada y
  Renacimiento de Harmonya (cinemáticos).
- Cutscenes: `intro`, `outro_<dominio>` por cada nivel, `encrucijada`, `final`.

---

## 3. Tareas pendientes, bugs detectados y por implementar

### 3.1 Bugs / detalles conocidos
1. **Piloto de Lyra desalineado**: los FBX tienen orígenes distintos; el piloto de Lyra
   queda ligeramente desplazado de su kart (Karel y Vox se ven bien). Ajustar
   `localPosition` por personaje en `CharacterDisplay.SpawnModel` (calls al final de `Build`).
2. **`SampleScene.unity`** es un remanente legacy con el `Bootstrapper`; se puede eliminar.
3. **Código muerto**: `GameModeManager` (solo por el enum), `ObstacleLauncher` (no enganchado),
   `EnvironmentManager` (funciona pero podría integrarse mejor con el stage 3D).
4. **Escalado del modelo**: si se cambian de modelos, `ModelNormalizer.TargetSize` (kart 3.8–4.4,
   piloto 1.9) debe reajustarse.
5. **Audio externo**: `AudioReactiveMusicController` intenta cargar `GameSession.AudioTrackPath`
   (URL/ruta); no hay UI para elegir archivo todavía. En WebGL solo funciona con URL.
6. **TMP**: los recursos esenciales de TMP se copiaron a `Assets/TextMesh Pro/` (fuente + shaders).
   Si se borra esa carpeta, los textos no renderizan.

### 3.2 Mejoras pendientes (de los hallazgos de la prueba)
- **HUD ampliado** (pregunta 4 = 3.67/5): añadir posición en pista, potenciadores activos y tiempo.
- **Progresión/checkpoints** (pregunta 9 = 3.67/5): suavizar la dificultad inicial y señalizar mejor los checkpoints.
- **Feedback de impacto** (pregunta 6): reforzar aviso visual/sonoro al recibir un proyectil.
- **Sonido**: no hay SFX (solo música). Añadir bus de audio, SFX de motor/salto/impacto.
- **Animaciones**: los pilotos son estáticos (no hay Animator/AnimationClip en uso).

### 3.3 Tareas de evidencia SENA (no técnicas)
- Diligenciar los **2 usuarios reales** en `Docs/Analitica/respuestas.csv` y regenerar con
  `Tools/docs/generate_analytics.py` (ya hay 3 simulados; promedio 4.27/5).
- Capturar el **"Antes"** (`Docs/Capturas/Antes/` está vacío).
- Montar la **infografía comparativa** en Canva con el contenido de AA4.
- Publicar la build (Netlify Drop / itch.io / S3+CloudFront) y aplicar el cuestionario a 5 usuarios.

---

## 4. Librerías, dependencias y motores

- **Motor:** Unity **6000.5.8f1** (instalado en `/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1`).
- **Render:** Universal Render Pipeline (**URP 17.5.0**). Assets en `Assets/Settings/`
  (`PC_RPAsset`, `Mobile_RPAsset`, `DefaultVolumeProfile`). **Upscaling Filter = Linear**
  (se cambió de FSR a Linear porque FSR falla en WebGL).
- **Input:** `com.unity.inputsystem` 1.20.0. `activeInputHandler = 2` (**Both**); el código
  usa el backend legacy (funciona en WebGL).
- **UI:** `com.unity.ugui` 2.5.0 (**TextMeshPro** incluido).
- **Tests:** `com.unity.test-framework` 1.7.0 (NUnit).
- **Otros paquetes:** `com.unity.ai.navigation`, `com.unity.timeline`,
  `com.unity.visualscripting`, `com.unity.collab-proxy`, `com.unity.ide.rider`,
  `com.unity.ide.visualstudio`, `com.unity.multiplayer.center`.
- **Asmdefs:** `NitroRhythm` (runtime; referencia `Unity.TextMeshPro`, `UnityEngine.UI`,
  `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`),
  `NitroRhythm.Editor`, `NitroRhythm.Tests`, `NitroRhythm.Tests.EditMode`.
- **Blender 5.2** (Flatpak `org.blender.Blender`) para generar/exportar modelos (`bpy`).
- **Python 3** con `python-docx`, `matplotlib`, `Pillow` (documentos y analítica).
- **LibreOffice** (`soffice --headless`) para convertir `.docx` → `.pdf`.
- **Node/Playwright-core + Chromium** (caché en `~/.cache/ms-playwright`) para capturar el WebGL headless.
- **GitHub CLI (`gh`)** autenticado como `Jenifrutica`.

### 4.1 Comandos clave (importante)

**Compilar / ejecutar pruebas (workaround de licencia):** el editor por CLI directa falla por
licencia; hay que lanzarlo **dentro del sandbox de Unity Hub**:
```bash
flatpak run --command="/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity" \
  com.unity.UnityHub -batchmode -nographics -quit \
  -projectPath "/home/jenifrutica/SENA/NitroRythm/My project" \
  -executeMethod NitroRhythm.EditorTools.PrototypeSceneBuilder.BuildWebGL \
  -logFile "/home/jenifrutica/SENA/NitroRythm/My project/Logs/cli_webgl.log"
```
- Generar escenas: `-executeMethod NitroRhythm.EditorTools.PrototypeSceneBuilder.BuildAll`
- PlayMode: `-runTests -testPlatform PlayMode -testResults <xml>`
- EditMode: `-runTests -testPlatform EditMode -testResults <xml>`
- Requiere **sesión iniciada en Unity Hub** (licencia Personal). Si falla: abrir Unity Hub y firmar.

**Regenerar modelos:**
```bash
flatpak run --command=blender org.blender.Blender --background \
  --python "/home/jenifrutica/SENA/NitroRhythm/Tools/blender/generate_karts.py"
```

**Servir la build WebGL:**
```bash
cd "/home/jenifrutica/SENA/NitroRythm/Builds/WebGL" && python3 -m http.server 8080
```

---

## 5. Estado de verificación

- **Compilación:** OK (0 errores, 0 advertencias).
- **Pruebas:** **14/14 en verde** (10 PlayMode + 4 EditMode).
  - Resultados en `My project/Logs/test_results_playmode.xml` y `test_results_editmode.xml`.
- **Build WebGL:** OK (`Builds/WebGL`, Brotli + Decompression Fallback → funciona en cualquier hosting).
- **Repo:** `https://github.com/Jenifrutica/NitroRhythm_v1` (público), 3 commits.
- **Release:** `v1.0-prototype` con `NitroRhythm_WebGL.zip` (33 MB).

---

## 6. Prompt sugerido para la nueva sesión

> Copia y pega esto en la sesión nueva:

```
Trabajo en el proyecto NitroRhythm (Unity 6 URP, carreras arcade 3D con pistas
reactivas al audio y multijugador local). Lee primero
/home/jenifrutica/SENA/NitroRythm/My project/CONTEXTO_PROYECTO.md y luego
Docs/README.md, Docs/Evidencias/PLAN_EJECUCION.md y README.md.

Contexto rápido:
- Proyecto Unity: /home/jenifrutica/SENA/NitroRythm/My project (Unity 6000.5.8f1).
- Repo: https://github.com/Jenifrutica/NitroRhythm_v1 (público).
- Para compilar por CLI, la licencia solo funciona lanzando el editor dentro del
  sandbox de Unity Hub: flatpak run --command=".../Editor/Unity" com.unity.UnityHub
  -batchmode -nographics ...
- Blender está en flatpak (org.blender.Blender).
- Las 14 pruebas pasan (10 PlayMode + 4 EditMode).

Tareas prioritarias:
1. Corregir la alineación del piloto de Lyra en CharacterDisplay.
2. Ampliar el HUD (posición, potenciadores, tiempo) y suavizar la progresión
   (hallazgos P4 y P9 del informe).
3. Reforzar el feedback al recibir impactos.
4. (Opcional) Añadir SFX y publicar la build.
No hagas commit ni push sin que yo lo pida.

Empieza confirmando el estado con git status y leyendo los archivos de contexto,
y luego propón un plan corto antes de tocar código.
```

---

## 7. Notas de seguridad / higiene

- El `.gitignore` de Unity excluye `Library/`, `Builds/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`.
- No hay secretos en el repo (se verificó). `mcp_server.py` y `openclaw.json` son tooling MCP sin credenciales.
- La carpeta `My project` pesa ~3.5 GB por `Library/` (no se sube).
- Los commits se hicieron **sin `Co-Authored-By`**.
