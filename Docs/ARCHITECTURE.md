# ARCHITECTURE.md — NitroRhythm

Detalle técnico de cómo está construido el juego. Complementa `AGENT_CONTEXT.md`.

---

## 1. Asambleas y dependencias

| Asamblea | Ruta | Referencias |
| :-- | :-- | :-- |
| `NitroRhythm` | `Assets/Scripts/NitroRhythm.asmdef` | `Unity.TextMeshPro`, `UnityEngine.UI`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime` |
| `NitroRhythm.Editor` | `Assets/Editor/NitroRhythm.Editor.asmdef` | `NitroRhythm` (solo Editor) |
| `NitroRhythm.Tests` | `Assets/Tests/PlayMode/NitroRhythm.Tests.asmdef` | `NitroRhythm`, TMP, UI, TestRunner, NUnit |
| `NitroRhythm.Tests.EditMode` | `Assets/Tests/EditMode/NitroRhythm.Tests.EditMode.asmdef` | `NitroRhythm`, `NitroRhythm.Editor`, TestRunner, NUnit |

La asamblea de runtime es **auto-referenciada** para que las escenas y los scripts
de `Assets/Editor` la encuentren.

---

## 2. Ciclo de arranque (Gameplay)

```
TestSceneBootstrapper.Start()
  ├─ GameSession.EnsureExists()
  ├─ GameSession.MarkLevelStart()      // totales para reinicio
  ├─ LevelGoalTrigger.ResetMessage()
  └─ StartGame(session.Mode)
       ├─ GetViewportRects(mode)
       ├─ InitializeTrackAndAudio()
       │    ├─ LevelManager (SetAutoBuild(false))
       │    └─ AudioReactiveMusicController → evento AnalysisReady
       ├─ SpawnEntities(mode)          // P1/P2/P3 o bot, villano, health
       ├─ SetupCameras(mode)           // una cámara por viewport + CameraFollow
       └─ SetupSystems()
            ├─ NeonPostFx.Ensure()
            ├─ EnvironmentManager
            ├─ LevelIntroCard
            ├─ CutsceneController + DialogueSystem
            ├─ GameplayHud + PauseMenu
            └─ RespawnOnFall
```

Cuando el audio termina de analizarse (`AnalysisReady`):

```
PrepareTheme(session)
  └─ DomainTheme.SetCurrent(DomainTheme.Get(id))
LevelManager.GenerateSingleLevel(analysis, CurrentLevelIndex, 7, startX)
  └─ ProceduralTrackBuilder.Build(...) → plataformas, huecos, obstáculos, pads, meta
DressDomain(session)
  └─ DomainDecorator.Build(...) → cielo, niebla, luz, post, props, partículas
LevelManager.RepositionPlayersToCheckpoint()
VillainBoss.RepositionToLevel(...)
```

> El orden es importante: los jugadores se crean **antes** de que exista la pista;
> por eso se recolocan al terminar la generación.

---

## 3. Estado de sesión

`GameSession` (`DontDestroyOnLoad`) guarda:

| Campo | Uso |
| :-- | :-- |
| `Mode` | `GameMode` (4 modos). |
| `P1/P2/P3CharacterId` | Ids del JSON. `GetCharacter` fuerza P2 ≠ P1. |
| `UseAudioReactive`, `AudioTrackPath` | Fuente de música. |
| `MusicVolume`, `SfxVolume`, `AudioCalibrationMs` | Opciones. |
| `CurrentLevelIndex` | Dominio jugable actual (1..7). |
| `ControlsGuideShown` | Muestra la guía una vez por partida. |
| `LastScore`, `LastTime` | Totales de la carrera. |
| `LevelStartScore`, `LevelStartTime` | Para `RestartLevel`. |
| `Analysis` | Resultado pre-analizado. |
| `BeginRun/MarkLevelStart/RestartLevel/AdvanceLevel` | Transiciones. |

`SceneFlow` centraliza la navegación y detecta si una escena está en Build Settings.

---

## 4. Generación de pista

`AudioAnalysisService.Analyze(clip)`:
- Mezcla a mono, frames de 20 ms.
- Low-pass one-pole (≈180 Hz) → graves; residual → agudos.
- Envolvente de energía + autocorrelación de onsets → **BPM**.
- Agrega energía por pulso (`beatEnergy`, `bassEnergy`, `trebleEnergy`).

`AudioAnalysisService.AnalyzeSamples(pcm, channels, rate)` hace lo mismo sobre muestras
sueltas (sin `AudioClip`): lo usan las canciones del jugador y puede correr en un hilo.
El BPM se afina con interpolación parabólica y los pulsos se agregan con límites
fraccionarios, así la rejilla no se desfasa en canciones largas.

`ProceduralTrackBuilder.Build(analysis, levelDef, startX)`:
- Hasta 48 segmentos; si el tramo tiene más pulsos, cada segmento agrupa varios
  (máximo de graves/agudos del grupo) para que **todo** el tramo dé forma a la pista.
- **Rejilla de pulsos**: 1 pulso = `cruiseSpeed (34 u/s) × segundos por pulso`. Cada segmento
  (plataforma + hueco) ocupa exactamente sus pulsos: el borde del hueco coincide con un beat y
  el nivel dura lo mismo que su música. `plan.beatXs` → `LevelManager.SpawnBeatLines` (líneas
  `BeatLine`/`BeatLineBar` que pulsan con el bombo). `AudioReactiveMusicController.SyncToRaceStart`
  reinicia la música en el pulso 0 cuando se cierra la tarjeta/guía.
- `bass > 0.45` → hueco grande + jump pad/ramp.
- `treble > 0.35/0.5/0.7` → spinner / moving hazard / barrier.
- BPM → espaciado y `speedMultiplier`.
- Dificultad del dominio → escala de huecos y tramos seguros.

`LevelManager`:
- `GenerateSingleLevel`: construye **solo** el dominio actual (cada nivel es su
  propia pantalla) y mantiene `TotalLevels` para las comprobaciones de “último nivel”.
- `SpawnPlatform`, `SpawnObstacleByKind`, `SpawnJumpPad`, `SpawnSpeedPad`,
  `SpawnRamp`, `SpawnCheckpoint`, `SpawnLevelGoal`.
- Checkpoints por posición (`CheckpointXs`), spawns por carril (`GetCheckpointSpawn`).
- `ActivateCheckpoint` solo avanza; `RepositionPlayersToCheckpoint` mueve el
  **Rigidbody** (no solo el transform) para que la física no lo sobrescriba.

---

## 5. Audio

| Clase | Responsabilidad |
| :-- | :-- |
| `AudioAnalysisResult` | Datos serializables (BPM, beats, bandas). `CreateFlat()` para tests. |
| `MusicGenerator` | Loop sintetizado de respaldo (kick, sub-bass, hats, arpegio). |
| `DomainMusic` | Perfil musical por dominio (tempo/tono/groove). |
| `AmbientBed` | Ambiente por dominio, loop con cross-fade. |
| `SfxLibrary` | SFX sintetizados y cacheados (sin archivos). |
| `SfxPlayer` | Pool de `AudioSource` 2D, aplica `GameSession.SfxVolume`. |
| `AudioReactiveMusicController` | Carga clip (ruta/URL o generado), analiza, reproduce, expone `Bass/Treble/Energy`. |
| `EngineSound` | Bucle de motor con pitch/volumen según velocidad. |
| `UserSong` | Canción del jugador: PCM mono + análisis completo; la divide en **un tramo por dominio** (alineado a pulsos, mínimo 24 s; en canciones cortas los tramos se solapan) y crea el clip de cada tramo con fundidos. |
| `SongLoader` | Carga la canción: WebGL → selector del navegador (`Assets/Plugins/WebGL/NitroSongPicker.jslib`, decodifica con WebAudio a mono 32 kHz); escritorio/Editor → `UnityWebRequestMultimedia` (MP3/OGG/WAV/AIFF). Analiza en un hilo (salvo WebGL). Máx. 8 min, mín. 15 s. |

**Canción del jugador en la campaña:** `GameSession.UserSong` persiste entre escenas.
`AudioReactiveMusicController` usa, en el dominio *n* de 7, `UserSong.CreateSectionClip(n)`
y `SectionAnalysis(n)`: el dominio 1 corre el inicio de la canción y el 7 el final.
Sin canción, cada dominio usa su música sintetizada (`DomainMusic`). `GameSession.AudioTrackPath`
sigue funcionando como entrada heredada/pruebas (se carga como `UserSong`).

Los SFX y la música son **procedurales** para que funcionen en WebGL sin assets de audio.

---

## 6. Combate

- `VillainBoss`:
  - IA: corre por delante, mantiene **correa** de distancia (objetivo 26 m, frena por
    encima de 38 m, se teletransporta a > 70 m o si queda 10 m por detrás), ataca cada
    2 pulsos, 4 tipos (directo, ráfaga, mina, barrera), telegrafía en el suelo.
  - Jugador (P3): `IJKL`, salto `U`, ataque `Enter`.
  - `FiringPoint` desde `KartAnchors`.
- `BadReward`: vuelo cinemático `Lerp + Sin`, daño por proximidad, aterriza,
  reactiva física y se destruye a los 6 s.
- `KartHealthSpeed`: −15 HP, slow −40 % 3 s, flash, **K.O. 5 s** con cuenta
  regresiva, revive en checkpoint con vida completa.
- `IDamageable` / `ITargetSelectable` desacoplan daño y objetivos.

---

## 7. Jugador y controles

- `IInputProvider` (`Throttle`, `Steer`, `ConsumeJump`, `ConsumeFire`).
- `KeyboardInputProvider` (P1/P2/P3), `ScriptedInputProvider` (bot/eventos).
- `PlayerKartController`:
  - `Rigidbody` con gravedad propia, `Continuous`, rotación X/Z congelada.
  - `SphereCast` para suelo; salto por velocidad.
  - `ApplyCharacterStats(speed, grip, power)`.
  - `ControlLocked` durante K.O.
  - `ApplySpeedBoost`, `CurrentSpeed`.
- `BotKartAI`: throttle 1, centra el rumbo, detecta huecos **por delante** y salta.
- `EngineSound`: pitch/volumen por velocidad.
- `KartAnimator` (instalado por `VisualEntityFactory.CreateKartEntity`): animación procedural
  de kart y piloto (los FBX no tienen huesos): alabeo en curvas, cabeceo al acelerar/saltar,
  rebote de suspensión, estirar/aplastar al despegar/aterrizar, temblor al recibir golpe,
  trompo en K.O., piloto que se inclina y mira hacia la curva y “respira” parado.
  Los modelos cuelgan de `AnimRig` (escala inversa del collider) → `AnimPivot`, para rotar sin
  cizalla; por eso las pruebas buscan `*_Visual`/`*_Pilot` en profundidad.
- `PilotAnimator`: clips esqueléticos del piloto (FBX importado como **Generic**, cargados de
  `Resources` con `LoadAll<AnimationClip>`), mezclados con Playables (`AnimationMixerPlayable`):
  Idle, SteerLeft/Right, Boost, Celebrate, Hit, KO. Restaura cada frame las posiciones de reposo
  de los huesos (los clips de Blender también hornean posiciones). Importarlo como **Legacy**
  desplazaba al piloto del asiento: no hacerlo.
- `KartWheelRig`: crea en tiempo de carga los huesos `Wheel_FL/FR/RL/RR` del kart (malla única,
  `isReadable`) y la convierte en `SkinnedMeshRenderer`; `KartAnimator` gira las ruedas según la
  velocidad y orienta las delanteras. **No reexportar los FBX de kart desde Blender**: el nodo
  raíz cambia y el kart se desplaza respecto al piloto.
- `CameraFollow`: mira 7 m por delante del kart (`LookAhead`) y abre el FOV con la velocidad
  y el turbo (`DynamicFov`).

---

## 8. Mundo temático (`World/`)

- `DomainTheme`: struct de tema (cielo/panorama, exposición, niebla, ambiente, luz,
  patrón de plataforma, colores, post, partículas, `PropSpec[]`).
- `DomainDecorator.Build`: aplica cielo/fog/luz/post, dispersa props a ambos lados
  de la pista (determinista por semilla), partículas y decoración.
- `DomainMaterials`: texturas procedurales (tiles, ventanas, puntos).
- `PropFactory`: instancia modelos propios (`Torre*`, `TrebleClef`, `SpikeBall`,
  `Prize*`) o mallas procedurales; sin colliders.
- `ProceduralMeshes`: toro/arco, cono, cristal, roca/isla.
- `PropAnimator`: giro, bob, deriva y pulso reactivo al bajo.
- `DomainParticles` / `ParticleFollow`: volumen de partículas que sigue al líder.
- `HazardVisuals`: builders temáticos de hazards/pads/portal con colliders correctos.
- `MusicVisualizer`: dos muros-ecualizador (30 barras por lado, bandas logarítmicas del
  espectro en vivo) que siguen al líder; la luz principal y el bloom laten con los graves y
  las tiras de neón del borde de la pista destellan con cada golpe.
- `PrizePickup`: premio recogible → turbo temporal.

---

## 9. UI

- **uGUI + TextMeshPro**, `CanvasScaler` 1920×1080, `match=0.5`.
- `UIFactory`: sprites procedurales (redondeado, gradiente, círculo), paneles,
  tarjetas, botones, sliders, gauge radial; fuente TMP con fallback.
- `UIArt`: carga retratos/ilustraciones desde `Resources`.
- `MainMenuScreen`: stage 3D + menú + opciones + música.
- `CharacterSelectScreen`: garaje 3D, tarjetas, P1/P2, 4 modos, guía, botón **TU CANCIÓN**
  y etiqueta de la música elegida. Al elegir piloto, el kart da un salto con giro.
- `SongPickerPanel`: panel “TU CANCIÓN” (web: selector del navegador; escritorio: explorador
  de archivos propio con accesos a Música/Descargas/Inicio, lee `user-dirs.dirs`).
- `GameplayHud`: puntuación, dominio, tiempo, posición, distancia al villano,
  vida P1/P2 (muestra “K.O. n” parpadeando), potenciadores, avisos, cuenta regresiva,
  progreso con checkpoints. En cooperativo P2 tiene su propio velocímetro (abajo a la derecha).
- `LevelIntroCard`: con canción del jugador, el chip musical indica “Tramo n/7 de «…» · m:ss–m:ss”.
- `ResultsScreen`: el subtítulo nombra la canción si se usó una; el ganador celebra en el podio.
- `LevelTransition`: animación 2D al superar un dominio (≈4,5 s, tiempo sin escalar), llamada
  desde `CutsceneController.FinishEscape` antes de cargar el siguiente dominio o los resultados.
- `LevelIntroCard`: tarjeta por dominio (se muestra al iniciar cada pantalla).
- `ControlsGuide` / `ControlsOverlay`: guía por modo.
- `PauseMenu`, `ResultsScreen` (rango S/A/B/C), `ScreenFader`, `UIAmbient`, `UIHoverEffect`.
- `NeonPostFx`: Volume global (Bloom, Vignette, Chromatic Aberration, Color
  Adjustments, ACES) y `renderPostProcessing` en todas las cámaras.

---

## 10. Narrativa

- `DialogueSystem`: tarjeta de cómic (Canvas) que consume `CutsceneDefinition`.
- `CutsceneController`: al cruzar la meta congela el tiempo, muestra el diálogo,
  anima al villano escapando por un portal y avanza de nivel (o carga `03_Results`).
- `LevelGoalTrigger`: registra llegadas, 6 s de gracia, mensaje de espera.

---

## 11. Datos

`prototype_data.json` → `PrototypeData.Instance`:
- `CharacterDefinition`: id, nombre, rol, colores, stats, modelos, textura,
  `pilotOffset`, `pilotScale`.
- `LevelDefinition`: index, id, nombre, colores, `trackWidth`, dificultad,
  `isPlayable`, `isCinematic`, música, descripción.
- `CutsceneDefinition`: id, título, speaker, fondo, taunt, líneas, `nextLevelId`.

`PrototypeData.PlayableLevels` devuelve los 7 jugables ordenados.

---

## 12. Pruebas

| Archivo | Cobertura |
| :-- | :-- |
| `PrototypePlayModeTests` | Datos, audio/BPM, track builder, LevelManager, kart, salto, villano, proyectil, sesión, input. |
| `Phase0Tests` | Checkpoints, orientación de kart/piloto, ataques del villano. |
| `Phase1Tests` | Sesión/reinicio, avance de dominios, un solo dominio. |
| `Phase2Tests` | Decorador, ambiente/música por dominio, fuentes, HUD, UI, rangos. |
| `HudAndFeedbackTests` | Bot, cámara, checkpoints, HUD (rango/tiempo), SFX, boost, impacto, dificultad. |
| `GameplayFlowTests` | Escena real: kart conduce, cámara sigue, una cámara/AudioListener. |
| `CharacterDisplayAlignmentTests` | Pilotos centrados/sentados (3 personajes). |
| `GameplayEvidenceCapture` / `LegacyEvidenceCapture` | Capturas (solo con `NITRO_SHOT_DIR`). |
| `UserSongTests` | Análisis de muestras, mono, tramos (orden, solape, longitud), clip del tramo, controlador con canción, carga de WAV y MP3 reales. |
| `AnimationAndAmbienceTests` | Rig sin cizalla, alabeo en curvas, aplastamiento al aterrizar, muros-ecualizador, hover que respeta la selección. |
| `Round6Tests` | Bordes de plataforma en pulsos, duración = música, 4 ruedas que giran, clips del piloto, transición 2D. |
| `SceneSetupTests` (EditMode) | 4 escenas, Build Settings, JSON, modelos importados. |

---

## 13. Puntos de extensión

- **Nuevo dominio**: añadir `LevelDefinition` al JSON + `DomainTheme.Get(id)` (colores,
  props, partículas) + `outro_<id>` y ajustar `DomainMusic`/`AmbientBed`.
- **Nuevo personaje**: `CharacterDefinition` + modelos/textura + `pilotOffset`.
- **Nuevo modo**: `GameMode` + `GetViewportRects` + `SpawnEntities` + `ControlsGuide`.
- **Nueva fuente de audio**: crear un `UserSong` (`UserSong.FromPcm`) y asignarlo a
  `GameSession.UserSong`; el resto (tramos, pista, villano) se adapta solo.
- **Nuevo obstáculo**: `ProceduralTrackPlan.ObstacleKind` + `LevelManager` +
  `HazardVisuals`.
- **Nuevo prop**: `PropKind` + `PropFactory` + `DomainTheme.PropSpec`.
