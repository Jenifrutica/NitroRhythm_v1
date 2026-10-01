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
- **Checkpoints intermedios** y reaparición al caer (conservando el impulso).
- Pads de **salto** y de **velocidad**, rampas, barras giratorias y hazards móviles.
- **Combate del villano**: proyectil parabólico (`Lerp` + `Sin`), daño de proximidad,
  −15 % de vida y −40 % de velocidad por 3 s con flash en pantalla.
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
| P3 (Vox, rojo) | `I J K L` | `Shift Der.` / `Numpad 0` | `Enter` |
| Pausa | — | `Escape` | — |

---

## 🛠️ Tecnología

- **Unity 6000.5.8f1** (URP 17).
- **Input System** + backend legacy (para WebGL).
- **TextMeshPro** (UI Canvas).
- **Unity Test Framework** (NUnit) — 14 pruebas.
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
    Audio/        Análisis musical, generador de loop, plan de pista reactiva
    Combat/       Villano, proyectil, interfaces de daño/objetivo
    Controls/     IInputProvider + teclado + scripted (bot)
    Core/         LevelManager, GameSession, SceneFlow, factoría visual, stage, post-fx
    Data/         Definiciones cargadas desde JSON
    Narrative/    DialogueSystem + CutsceneController
    Player/       PlayerKartController, salud/velocidad, bot
    UI/           UIFactory + menú, selección 3D, opciones, pausa, HUD, resultados
  Tests/
    PlayMode/     10 pruebas de sistemas en ejecución
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

### Compilar para WebGL
Menú **NitroRhythm ▸ Build WebGL** → salida en `Builds/WebGL` (Brotli + fallback,
funciona en cualquier hosting estático).

### Pruebas
```bash
# En el editor: Window ▸ General ▸ Test Runner
# O por línea de comandos:
Unity -batchmode -runTests -testPlatform PlayMode -projectPath <ruta>
```
Resultado actual: **14/14 en verde**.

### Regenerar modelos (Blender)
```bash
flatpak run org.blender.Blender --background --python Tools/blender/generate_karts.py
```

---

## 🧪 Estado del proyecto

- [x] 4 escenas + UI Canvas/TMP.
- [x] Audio reactivo, 4 modos, combate, checkpoints.
- [x] Narrativa data-driven y 10 dominios.
- [x] Modelos 3D texturizados + post-proceso neón.
- [x] 14/14 pruebas automatizadas.
- [x] Build WebGL publicable.

---

## 📜 Créditos

Proyecto académico **SENA** — Ficha 3336511.
Aprendiz: **Jenifer Daniela Urbano Córdoba**.
Modelos 3D generados con Hunyuan3D y refinados en Blender.
Motor: Unity 6 (URP).
