# 🏁 NitroRhythm

**Carreras arcade 3D con generación de pistas reactiva al audio, persecución
asimétrica Héroes vs Villano y multijugador local de 1 a 3 jugadores.**

> *"Velocidad sintética, frecuencia neón y vectores letales."*

Prototipo funcional desarrollado en **Unity 6 (URP)** como evidencia del programa
**Tecnología en Desarrollo de Videojuegos y Entornos Interactivos — SENA**
(GA5-220501087).

---

## 🎮 ¿De qué trata?

Dos héroes (**Lyra Pulse** y **Karel Volt**) persiguen al villano **Vox Null** a
través de **7 dominios musicales** suspendidos en el vacío. El villano corre por
delante y dispara proyectiles parabólicos al jugador más cercano. Cada dominio
tiene su propia paleta, dificultad y pista generada a partir de la música.

- **Género:** Combat Kart Racing / Speedrun Obby.
- **Cámara:** tercera persona, con vistas divididas según el modo.
- **Estética:** ciberpunk neón (bloom, viñeta, aberración cromática).

---

## ✨ Características

### Modos de juego (multijugador local)
| Modo | Entidades | Pantalla | Entradas |
| :-- | :-- | :-- | :-- |
| 1 Jugador | P1 vs Villano (IA) | Completa | `WASD` + `Espacio` |
| 1 Jugador + Bot | P1 + P2 (IA) vs Villano | Split 50/50 | P1: `WASD` |
| Cooperativo 2P | P1 + P2 vs Villano | Split 50/50 | P2: `Flechas` + `Shift Der.` |
| Showdown 3P | P1 + P2 vs Villano (humano) | P1 sup-izq, P2 sup-der, Villano abajo | P3: `IJKL` + `Enter` |

### Mecánicas
- Movimiento arcade con `Rigidbody` y colisión continua.
- **Salto** con verificación de suelo por `SphereCast`.
- **13 checkpoints en toda la carrera** (arcos con tus torres-altavoz, ámbar → verde con aviso en pantalla) y reaparición al caer (conservando el impulso).
- **HUD completo**: tiempo, posición en pista y distancia al villano, potenciadores activos (turbo / ralentizado), vida de P1 y P2, velocímetro y barra de progreso.
- **Retroalimentación de impacto**: sacudida de cámara, destello rojo, aviso "IMPACTO!" y sonido.
- **Efectos de sonido procedurales** (salto, turbo, checkpoint, disparo, meta, hover y motor) que respetan el volumen de efectos.
- **Dificultad progresiva suave**: los primeros dominios tienen huecos más cortos, menos peligros y un tramo seguro al inicio.
- **Una pantalla por nivel** con tarjeta de introducción: 7 dominios con cielo, luz, niebla, plataformas, props,
  partículas, **música y ambiente propios** (Percusalia, Echoris, Bassline Abyss, Arpeggion, Treble Spire, Noctua Chord,
  Void Crescendo).
- **Interfaz nueva**: fuentes Orbitron/Rajdhani, iconos Material, tarjetas neón, retratos del concept art, rango S/A/B/C.
- **Premios recogibles** (turbo) y tus modelos reales: karts con piloto sentado, torres-altavoz, premios, clave de sol y bola de espinas.
- Pads de **salto** y de **velocidad**, rampas, barras giratorias y hazards móviles.
- **Combate del villano**: ataca **al compás de la música** con 4 ataques (directo, ráfaga, mina y barrera giratoria),
  marca en el suelo dónde caerá cada proyectil y apunta a donde estará el jugador; daño por área, −15 % de vida y
  −40 % de velocidad por 3 s. Si te quedas sin vida, K.O.: vuelves al checkpoint con la vida llena.
- **Cooperación**: ambos héroes deben cruzar la meta ("Esperando a…").

### Generación reactiva al audio
`AudioAnalysisService` analiza el clip (FFT ligera offline → **BPM**, energía,
graves y agudos por pulso) y `ProceduralTrackBuilder` traduce:
- **Graves (bombos/sub-bass)** → huecos, saltos y rampas.
- **Agudos (hi-hats/sintes)** → barras, hazards móviles y pads.
- **BPM** → separación, ancho de plataforma, velocidad base y **cadencia de disparo del villano**.

Si no se carga un audio externo, se usa un **loop sintetizado** (`MusicGenerator`),
por lo que la demo funciona siempre (incluso en WebGL).

### Narrativa
- **10 dominios musicales**: Conservatorio Armónico → Percusalia → Echoris →
  Bassline Abyss → Arpeggion → Treble Spire → Noctua Chord → Encrucijada →
  Void Crescendo → Renacimiento de Harmonya.
- Cutscenes **data-driven** (`DialogueSystem` + `CutsceneController`) leídas desde
  `prototype_data.json`, con retratos y escape del villano por un portal.

### Rediseño gráfico 3D
- Karts y pilotos **modelados en Blender / generados con Hunyuan3D** y texturizados.
- Pista con rejilla neón, pedestales, luces de contorno y **post-proceso URP**
  (Bloom, Vignette, Chromatic Aberration, ACES).
- Pipeline de **anclajes modulares** (`RootTransform`, `CenterOfMass`, `FiringPoint`,
  `WheelMeshes`, `HoverEffect`) para intercambiar modelos sin tocar la lógica.

---

## 🕹️ Controles

| Jugador | Conducir | Salto | Disparo |
| :-- | :-- | :-- | :-- |
| P1 (Lyra, cian) | `W A S D` | `Espacio` | `Ctrl Izq.` |
| P2 (Karel, azul) | `Flechas` | `Shift Der.` / `Numpad 0` | `Numpad 1` |
| P3 (Vox, rojo) | `I J K L` | `U` | `Enter` (lanzar obstáculo) |
| Pausa | — | `Escape` | — |

---

## 🛠️ Tecnología

- **Unity 6000.5.8f1** (URP 17).
- **Input System** + backend legacy (para WebGL).
- **TextMeshPro** (UI Canvas).
- **Unity Test Framework** (NUnit) — 40 pruebas automáticas (36 PlayMode + 4 EditMode).
- **Blender 5.2** (bpy) para generar/exportar modelos.

---

## 📁 Estructura

```
Assets/
  Editor/         PrototypeSceneBuilder (genera las 4 escenas) + EvidenceCaptureTool
  Resources/NitroRhythm/
    prototype_data.json   Datos narrativos (personajes, 10 dominios, cutscenes)
    Models/               Kart_Neon_01..03, Piloto_Neon_01..03, PremioMalo.fbx
    Textures/             Lyra.jpg, Karel.jpg, Vox.jpg
    Materials/            Materiales base URP (garantizan shaders en el build)
  Scenes/         00_MainMenu · 01_CharacterSelect · 02_Gameplay · 03_Results
  Scripts/
    Audio/        Análisis musical, generador de loop, plan de pista reactiva, SFX procedurales
    Combat/       Villano, proyectil, interfaces de daño/objetivo
    Controls/     IInputProvider + teclado + scripted (bot)
    Core/         LevelManager, GameSession, SceneFlow, factoría visual, stage, post-fx
    World/        Temas por dominio, props procedurales y propios, partículas, peligros y decorador
    Data/         Definiciones cargadas desde JSON
    Narrative/    DialogueSystem + CutsceneController
    Player/       PlayerKartController, salud/velocidad, bot
    UI/           UIFactory + menú, selección 3D, opciones, pausa, HUD, resultados
  Tests/
    PlayMode/     36 pruebas de sistemas en ejecución (+ 5 generadores de capturas)
    EditMode/     4 pruebas de escenas/recursos
Tools/            Scripts de Blender (modelos) y de documentos
```

---

## ▶️ Cómo ejecutar

1. Abra el proyecto con **Unity Hub** (versión 6000.5.8f1).
2. Espere la importación inicial.
3. Las 4 escenas se generan automáticamente si faltan; si no, use el menú
   **NitroRhythm ▸ Build Prototype Scenes**.
4. Abra `Assets/Scenes/00_MainMenu.unity` y pulse **Play**.

### Jugar la build
- **Web:** `Builds/Jugar_NitroRhythm.sh` (sirve la build WebGL y abre el navegador).
- **Linux:** `Builds/Linux/NitroRhythm.x86_64` (ver `Builds/Jugar_Linux.sh`).

### Compilar para WebGL
Menú **NitroRhythm ▸ Build WebGL** → salida en `Builds/WebGL` (Brotli + fallback,
funciona en cualquier hosting estático).

### Pruebas
```bash
# En el editor: Window ▸ General ▸ Test Runner
# O por línea de comandos:
Unity -batchmode -runTests -testPlatform PlayMode -projectPath <ruta>
```
Resultado actual: **40/40 en verde** (detalle en [`Docs/Evidencias/RESULTADO_PRUEBAS.md`](Docs/Evidencias/RESULTADO_PRUEBAS.md)).

### Regenerar modelos (Blender)
```bash
flatpak run org.blender.Blender --background --python Tools/blender/generate_karts.py
```

---

## 📚 Documentación

Toda la documentación del proyecto está dentro de este repositorio, en la carpeta
**[`Docs/`](Docs/)**:

- 📋 **[Cuestionario de prueba](Docs/Evidencias/GA5-AA1-EV01_Cuestionario.md)** (10 preguntas).
- 📝 **[Guía para 5 usuarios](Docs/Evidencias/Guia_5_Usuarios.md)**.
- 📊 **[Informe de hallazgos](Docs/Evidencias/GA5-AA2-EV01_Informe_Hallazgos.md)** con analítica.
- 🔧 **[Registro de cambios](Docs/Evidencias/GA5-AA3-EV01_Registro_Cambios.md)** (antes/después).
- 🖼️ **[Infografía comparativa](Docs/Evidencias/GA5-AA4-EV01_Infografia_Comparativa.md)**.
- 🗺️ **[Plan de ejecución](Docs/Evidencias/PLAN_EJECUCION.md)**.
- ✅ **[Resultado de las pruebas](Docs/Evidencias/RESULTADO_PRUEBAS.md)** (25 aprobadas).
- 🔗 **[Enlaces exactos de cada evidencia](Docs/Evidencias/ENLACES_EVIDENCIAS.md)**.
- 🧾 **[Créditos y licencias](Docs/CREDITOS_Y_LICENCIAS.md)** (fuentes OFL, HDRIs CC0, iconos Apache 2.0).
- 🧊 **[Paquete de referencia para Hunyuan3D](Docs/Hunyuan/PROMPTS_HUNYUAN.md)** (vistas y prompts de los personajes).
- 📈 **[Analítica](Docs/Analitica/)** (respuestas CSV, gráficos y resumen).
- 📝 **[Preguntas para Google Forms](Docs/Forms_Google/PREGUNTAS_FORMULARIO.md)**.
- 🖼️ **[Infografía (PPTX para Canva)](Docs/Evidencias/GA5-AA4-EV01_Infografia.pptx)**.
- 📸 **[Capturas](Docs/Capturas/Despues/)** del prototipo final.
- 🧰 **[Scripts de herramientas](Docs/Tools/)** (Blender y documentos).

Cada documento está en `.md`, `.docx` y `.pdf`.

## ⬇️ Descargar la build jugable

La build WebGL está en la sección **[Releases](../../releases)** del repositorio
(archivo `NitroRhythm_WebGL.zip`). Para jugarla en local:

```bash
unzip NitroRhythm_WebGL.zip -d NitroRhythm
cd NitroRhythm && python3 -m http.server 8080
# Abrir http://localhost:8080
```

---

## 🧪 Estado del proyecto

- [x] 4 escenas + UI Canvas/TMP.
- [x] Audio reactivo, 4 modos, combate, checkpoints.
- [x] Narrativa data-driven y 10 dominios.
- [x] Modelos 3D texturizados + post-proceso neón.
- [x] 40/40 pruebas automatizadas.
- [x] HUD ampliado, checkpoints visibles, feedback de impacto y SFX (hallazgos P4, P6 y P9).
- [x] Build WebGL publicable.

---

## 📜 Créditos

Proyecto académico **SENA** — Ficha 3336511.
Aprendiz: **Jenifer Daniela Urbano Córdoba**.
Modelos 3D generados con Hunyuan3D y refinados en Blender.
Motor: Unity 6 (URP).
