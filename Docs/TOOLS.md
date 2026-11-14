# TOOLS.md — Herramientas del proyecto

Ficha de cada herramienta (Editor, Blender, documentos, WebGL) y de los archivos
especiales que no son código de juego.

Rutas base:
```bash
P="/home/jenifrutica/SENA/NitroRythm/My project"
UNITY="/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity"
```

---

## 1. Herramientas de Editor (`Assets/Editor/`)

| Menú | Script | Qué hace |
| :-- | :-- | :-- |
| **NitroRhythm ▸ Build Prototype Scenes** | `PrototypeSceneBuilder.BuildAll` | Crea/actualiza las 4 escenas (`00_MainMenu..03_Results`), registra Build Settings y crea los materiales base. También se ejecuta solo si faltan escenas al abrir el proyecto. |
| **NitroRhythm ▸ Build WebGL** | `PrototypeSceneBuilder.BuildWebGL` | `BuildAll` + Brotli/Decompression Fallback + `BuildPipeline.BuildPlayer` a `Builds/WebGL`. |
| **NitroRhythm ▸ Build Windows** / **Build macOS** / **Build Everything** | `PrototypeSceneBuilder.BuildWindows` / `BuildMac` / `BuildEverything` | `Builds/Windows/NitroRhythm.exe`, `Builds/macOS/NitroRhythm.app`, o las cuatro plataformas seguidas (sale con código 1 si una falla). |
| **NitroRhythm ▸ Build Linux** | `PrototypeSceneBuilder.BuildLinux` | `BuildAll` + build `StandaloneLinux64` a `Builds/Linux/NitroRhythm.x86_64`. |
| *(sin menú)* | `PrototypeSceneBuilder.BuildWebGLDebug` | Build WebGL de desarrollo sin compresión y con stack traces a `Builds/WebGLDebug`. |
| **NitroRhythm ▸ Ensure Base Materials** | `PrototypeSceneBuilder.EnsureMaterials` | Crea solo los materiales base en `Resources/NitroRhythm/Materials`. |
| **NitroRhythm ▸ Build Font Assets** | `FontAssetBuilder.Build` | Genera los `TMP_FontAsset` de Orbitron, Rajdhani y Material Icons + materiales glow en `Resources/NitroRhythm/Fonts`. |
| **NitroRhythm ▸ Probe Model Alignment** | `ModelAlignmentProbe.Run` | Mide bounds de kart/piloto por personaje para ajustar `pilotOffset`/`pilotScale`. |
| **NitroRhythm ▸ Evidence Capture Tool** | `EvidenceCaptureTool` (EditorWindow) | Captura el Game View a `<raíz del proyecto>/Evidencias/Antes` o `Despues` (**no** a `Docs/Capturas`; mover a mano). En Play usa `ScreenCapture`; en Edit renderiza la cámara a `RenderTexture`. |

Notas:
- `PrototypeSceneBuilder` usa `AssetDatabase` para crear carpetas/materiales y
  `EditorBuildSettings` para el orden de escenas.
- `EvidenceCaptureTool` es para capturas manuales; las capturas automáticas de
  evidencia usan los tests (ver `RUNBOOK.md`).

---

## 2. Herramientas de documentación (`Docs/Tools/docs/`)

| Script | Entrada | Salida |
| :-- | :-- | :-- |
| `generate_analytics.py` | `~/Downloads/forms_responses.csv` o `Docs/Analitica/respuestas_raw.csv` | `respuestas.csv`, `reporte_google_forms.md`, 3 PNG, `resumen_analitica.md` |
| `md_to_docx.py` | carpeta con `.md` (por defecto `Docs/Evidencias`) | `.docx` junto a cada `.md` |
| `make_infographic.py` | capturas `Antes/legacy` + `Despues` | `GA5-AA4-EV01_Infografia.pptx` |
| `make_aa4_docx.py` | mismas capturas | `GA5-AA4-EV01_Infografia_Comparativa.docx` (fuente negra, fondo blanco) |
| `summarize_tests.py` | `Logs/test_results_*.xml` | `Docs/Evidencias/RESULTADO_PRUEBAS.md` |
| `webgl_play.js` | `<url> <carpeta_salida>` de una build servida | capturas + errores de consola (`playwright-core`, ver RUNBOOK §5) |
| `webgl_smoke.js` | build WebGL servida | verificación de carga |
| `webgl_song_test.js` | `<url> <salida> <canción> [chrome]` | prueba de punta a punta de “TU CANCIÓN” en la build web (selector, análisis, carrera) |

Para PDF: `soffice --headless --convert-to pdf --outdir <dir> <archivo>.docx`.

---

## 3. Herramientas de Blender (`Docs/Tools/blender/`)

| Script | Qué hace |
| :-- | :-- |
| `generate_karts.py` | Genera karts/pilotos/premio con materiales y los exporta a `Assets/Models`. |
| `preview_model.py` | Renderiza una previsualización de un FBX/GLB/BLEND (elegir modelos). |
| `inspect_model.py` | Lista imágenes, materiales y objetos de un modelo. |
| `measure_bounds.py` | Mide bounds para escalar/normalizar. |
| `prepare_props.py` | Decima y prepara props (torres, premios, clave, bola) para Unity. |
| `render_views.py` | Renderiza vistas frente/lado/atrás/3-4 de personajes. |
| `contact_thumbs.py` | Hoja de contactos de vistas. |
| `hdr_to_jpg.py` | Convierte HDRI a JPG LDR para los cielos. |
| `rig_models.py` | Crea los clips de los pilotos (Idle, Steer, Boost, Celebrate, Hit, KO) en su esqueleto y renderiza vistas previas; con `--karts` inspecciona la detección de ruedas. |
| `probe_rig.py`, `probe_bones.py`, `probe_wheels.py`, `probe_fbx_nodes.py` | Diagnóstico: objetos/armaduras, huesos, ruedas y nodos FBX. |

Ejecución:
```bash
flatpak run --command=blender org.blender.Blender --background --python <script>.py
```

---

## 4. Utilidades sueltas (`Docs/Tools/`)

| Script | Qué hace |
| :-- | :-- |
| `extract_unitypackage.py` | Extrae un `.unitypackage` (assets + metas) a una carpeta. |
| `skies_horizon.py` | Procesa horizontes de los cielos. |

---

## 5. Archivos especiales

| Archivo | Para qué |
| :-- | :-- |
| `Assets/link.xml` | Evita que IL2CPP/WebGL elimine `UnityEngine.PhysicsModule` (necesario para `MeshCollider` del suelo procedural). |
| `Assets/TextMesh Pro/` | Recursos esenciales de TMP (fuente, shaders, settings). **No borrar**: sin esto no hay texto. |
| `Assets/Resources/NitroRhythm/Materials/Base*.mat` | Materiales base que garantizan que los shaders URP entren al build. |
| `mcp_server.py` | Servidor MCP de utilidades (listar/leer scripts, etc.) usado por `openclaw.json`. |
| `openclaw.json` | Configuración del servidor MCP `nitrorhythm`. |

---

## 6. Builds y lanzadores

Salida de los builds: `My project/Builds/` (`WebGL/`, `Linux/`, `WebGLDebug/`; no versionada).

Archivo en `SENA/NitroRythm/Builds/`:
- `NitroRhythm_WebGL_v1.2.zip`, `NitroRhythm_WebGL_v1.3.zip`.
- `NitroRhythm_Linux_v1.2.zip`, `NitroRhythm_Linux_v1.3.zip` (los v1.3 son idénticos en
  tamaño a los publicados en la release v1.3).
- `NitroRhythm_WebGL.zip` (2026-10-01, versión antigua).
- `Jugar_Linux.sh`, `Jugar_NitroRhythm.sh` (lanzadores).
- `WebGL/`: build del **2026-10-01** (vieja; no usar como referencia).

Release: https://github.com/Jenifrutica/NitroRhythm_v1/releases/tag/v1.3

---

## 7. Menús de Unity (referencia rápida)

| Menú | Acción |
| :-- | :-- |
| **NitroRhythm ▸ Build Prototype Scenes** | Escenas + materiales base |
| **NitroRhythm ▸ Build WebGL** | Build WebGL |
| **NitroRhythm ▸ Build Linux** | Build Linux |
| **NitroRhythm ▸ Ensure Base Materials** | Solo materiales base |
| **NitroRhythm ▸ Build Font Assets** | Fuentes TMP |
| **NitroRhythm ▸ Probe Model Alignment** | Medir pilotos |
| **NitroRhythm ▸ Evidence Capture Tool** | Capturas manuales |
| **Window ▸ General ▸ Test Runner** | Pruebas |
| **File ▸ Build Settings** | Otras plataformas |
