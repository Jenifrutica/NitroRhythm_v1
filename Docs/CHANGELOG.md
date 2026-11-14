# CHANGELOG.md — NitroRhythm

Historial de rondas de desarrollo. Fechas del entorno de trabajo.

## Ronda 5 — Tu canción, animación y ambientación (2026-11-13)

- **Canciones locales**: botón “TU CANCIÓN” en la selección. Web: selector del navegador
  (MP3/WAV/OGG/M4A/FLAC, decodificado por WebAudio vía `NitroSongPicker.jslib`); Linux/Editor:
  explorador de archivos propio (MP3/OGG/WAV/AIFF). La canción se reparte en los 7 dominios
  (un tramo consecutivo por pantalla) y cada pista se genera de su tramo; el villano ataca a su BPM.
- Análisis: muestras sin `AudioClip`, mezcla real a mono, BPM con precisión sub-frame,
  rejilla de pulsos sin deriva; el generador usa el tramo completo (agrupa pulsos).
- **Animación procedural** de kart y piloto (`KartAnimator`) y salto con giro en el garaje.
- **Ambientación**: muros-ecualizador reactivos al espectro, luz/bloom que laten con los
  graves, bordes de pista que destellan al compás.
- **Corrección**: `UIHoverEffect` repintaba los botones con su color inicial y borraba la
  selección (modo de juego, P1/P2 no se marcaban).
- **Pruebas**: 69 PlayMode (58 OK, 11 omitidas) + 4 EditMode, 0 fallos. Las pruebas de flujo
  saltan la guía del primer nivel (añadida el 2026-10-05), que las dejaba esperando en pausa.
- **Interfaz/cámara**: velocímetro de P2 en cooperativo, “K.O. n” en la barra de vida,
  cámara con mirada adelantada (menos vacío bajo la pista) y FOV dinámico, tramo de la
  canción en la tarjeta del dominio, canción en Resultados.

## Revisión de documentación (2026-11-13)

- Documentos de handoff auditados contra el código: cifras de pruebas unificadas,
  menús de Editor completos (Build Linux, Ensure Base Materials, WebGLDebug), rutas de
  builds aclaradas, `webgl_play.js` con dependencia y argumentos, correa del villano,
  `PremioMalo` como respaldo y restos de plantilla inventariados.
- Licencia de Unity renovada; pruebas re-ejecutadas en la ronda 5.

---

## Ronda 4 — Prueba con usuarios y cierre de evidencias (2026-10-05/06)

- **Prueba de funcionalidad** con 5 usuarios vía Google Forms (10 preguntas 1–5 + 1 abierta).
- Analítica real: **promedio 4.22/5**; gráficos por pregunta, usuario y distribución.
- **AA2** regenerado con gráficos incrustados.
- **AA4**: infografía comparativa (PPTX → Canva → PDF) y documento DOCX con fuente negra.
- Capturas “antes” **simuladas** (proto de cubos) para una comparativa clara.
- Entregas organizadas por evidencia y ZIPs finales.
- Enlaces reales (Forms/Sheets/Canva) documentados.

## Ronda 3 — Cámara, controles y orientación (2026-10-04)

- Cámara: offset (0, 4, −8.5), suavizado 12.
- `PlayerKartController`: rozamiento solo al soltar el acelerador (~41 u/s).
- Modelos: kart orientado al frente; piloto en el asiento; `pilotOffset`/`pilotScale`.
- **K.O. con cuenta regresiva de 5 s** y revive en checkpoint.
- P1 ≠ P2; carriles de spawn distintos.
- **ModelSanitizer**: desactiva cámaras/luces/AudioListener de los FBX (arregla el 3D
  de la build WebGL release).
- Meta con llegadas registradas + 6 s de gracia.
- Selección con insignias de jugador; `LayoutStage` escala los karts con las tarjetas.
- `ControlsGuide`, `ControlsOverlay`, pausa rediseñada; P3 salta con `U`.
- Lyra más grande/centrada; retratos con esquinas redondeadas; botones sin barra.
- Pruebas en verde (última ejecución completa: 51 PlayMode → 42 OK + 9 omitidas,
  4 EditMode); build v1.3 verificada en navegador.

## Ronda 2 — Mundo temático y UI nueva (2026-10-04)

- **Una pantalla por dominio** (`GenerateSingleLevel`) con tarjeta de introducción.
- `World/`: `DomainTheme`, `DomainDecorator`, `DomainMaterials`, `PropFactory`,
  `ProceduralMeshes`, `DomainParticles`, `PropAnimator`, `HazardVisuals`, `PrizePickup`.
- **Música y ambiente por dominio** (`DomainMusic`, `AmbientBed`) y SFX procedurales.
- Villano que ataca al compás: 4 tipos, telegrafía, predicción, daño por área.
- Checkpoints reducidos (~121 → 13) y por posición.
- UI nueva: Orbitron/Rajdhani/Material Icons, tarjetas neón, HUD rediseñado, rangos.
- Props reales (torres, premios, clave de sol, bola de espinas) decimados.
- Cielos HDRI (Poly Haven, CC0) + cielos procedurales.

## Ronda 1 — HUD, SFX, bot y pilotos (2026-10-01)

- HUD ampliado: tiempo, posición, distancia al villano, potenciadores, vida P1/P2.
- Progresión suavizada (huecos −40 % en el primer dominio).
- Checkpoints visibles (arcos ámbar→verde) y aviso.
- Feedback de impacto: sacudida, destello, aviso, sonido.
- SFX procedurales + motor.
- Bot detecta huecos por delante y salta.
- Pilotos alineados/sentados por personaje.
- Tiempo total en Resultados.

## Base — Prototipo (2026-09)

- 4 escenas: menú, selección, gameplay, resultados.
- Pista reactiva al audio (graves/agudos/BPM).
- Multijugador local 1–3 con viewports.
- Combate del villano y proyectil parabólico.
- Narrativa data-driven (10 dominios, 3 personajes, cutscenes).
- UI Canvas + TMP; post-proceso neón.
- Modelos Hunyuan/Blender texturizados.
- Pruebas automatizadas y build WebGL.
