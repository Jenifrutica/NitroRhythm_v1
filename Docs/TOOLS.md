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
| **NitroRhythm ▸ Build Font Assets** | `FontAssetBuilder.Build` | Genera los `TMP_FontAsset` de Orbitron, Rajdhani y Material Icons + materiales glow en `Resources/NitroRhythm/Fonts`. |
| **NitroRhythm ▸ Probe Model Alignment** | `ModelAlignmentProbe.Run` | Mide bounds de kart/piloto por personaje para ajustar `pilotOffset`/`pilotScale`. |
| **NitroRhythm ▸ Evidence Capture Tool** | `EvidenceCaptureTool` (EditorWindow) | Abre cada escena y captura el Game View a `Evidencias/Antes` o `Despues`. En Play usa `ScreenCapture`; en Edit renderiza la cámara a `RenderTexture`. |

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
| `webgl_play.js` | build WebGL servida | capturas + errores de consola (Playwright) |
| `webgl_smoke.js` | build WebGL servida | verificación de carga |

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

En `SENA/NitroRythm/Builds/`:
- `NitroRhythm_WebGL_v1.2.zip`, `NitroRhythm_WebGL_v1.3.zip`.
- `NitroRhythm_Linux_v1.2.zip`, `NitroRhythm_Linux_v1.3.zip`.
- `Jugar_Linux.sh`, `Jugar_NitroRhythm.sh` (lanzadores).
- `Builds/WebGL/` (salida del build más reciente).

Release: https://github.com/Jenifrutica/NitroRhythm_v1/releases/tag/v1.3

---

## 7. Menús de Unity (referencia rápida)

| Menú | Acción |
| :-- | :-- |
| **NitroRhythm ▸ Build Prototype Scenes** | Escenas + materiales base |
| **NitroRhythm ▸ Build WebGL** | Build WebGL |
| **NitroRhythm ▸ Build Font Assets** | Fuentes TMP |
| **NitroRhythm ▸ Probe Model Alignment** | Medir pilotos |
| **NitroRhythm ▸ Evidence Capture Tool** | Capturas manuales |
| **Window ▸ General ▸ Test Runner** | Pruebas |
| **File ▸ Build Settings** | Build Linux/otros |
