# RUNBOOK.md — comandos de NitroRhythm

Comandos exactos para operar el proyecto. Rutas base:

```bash
PROJECT="/home/jenifrutica/SENA/NitroRythm/My project"
UNITY="/home/jenifrutica/SENA/Unity/Hub/Editor/6000.5.8f1/Editor/Unity"
```

> **Regla de licencia:** el editor debe lanzarse **dentro del sandbox de Unity Hub**.
> Lanzarlo directo (`"$UNITY" ...`) falla con “No valid Unity Editor license”.

---

## 1. Abrir el proyecto

1. Unity Hub → **Open** → `SENA/NitroRythm/My project`.
2. Versión **6000.5.8f1**.
3. Si faltan escenas: menú **NitroRhythm ▸ Build Prototype Scenes**.
4. Abrir `Assets/Scenes/00_MainMenu.unity` → **Play**.

Si Hub pide licencia: iniciar sesión y reintentar.

---

## 2. Pruebas

### PlayMode
```bash
cd "$PROJECT"
flatpak run --command="$UNITY" com.unity.UnityHub \
  -batchmode -nographics \
  -runTests -testPlatform PlayMode \
  -projectPath "$PROJECT" \
  -testResults "$PROJECT/Logs/test_results_playmode.xml" \
  -logFile "$PROJECT/Logs/cli_tests_pm.log"
```

### EditMode
```bash
flatpak run --command="$UNITY" com.unity.UnityHub \
  -batchmode -nographics \
  -runTests -testPlatform EditMode \
  -projectPath "$PROJECT" \
  -testResults "$PROJECT/Logs/test_results_editmode.xml" \
  -logFile "$PROJECT/Logs/cli_tests_em.log"
```

### Resumen legible
```bash
python3 "$PROJECT/Docs/Tools/docs/summarize_tests.py"
# → Docs/Evidencias/RESULTADO_PRUEBAS.md
```

Resultado esperado: **36 PlayMode OK + 5 omitidas + 4 EditMode OK, 0 fallos**.

---

## 3. Generar escenas y materiales base

```bash
flatpak run --command="$UNITY" com.unity.UnityHub \
  -batchmode -nographics -quit \
  -projectPath "$PROJECT" \
  -executeMethod NitroRhythm.EditorTools.PrototypeSceneBuilder.BuildAll \
  -logFile "$PROJECT/Logs/cli_scenes.log"
```

Genera `Assets/Scenes/00..03`, registra Build Settings y crea los materiales base
en `Assets/Resources/NitroRhythm/Materials`.

---

## 4. Compilar

### WebGL
```bash
flatpak run --command="$UNITY" com.unity.UnityHub \
  -batchmode -nographics -quit \
  -projectPath "$PROJECT" \
  -executeMethod NitroRhythm.EditorTools.PrototypeSceneBuilder.BuildWebGL \
  -logFile "$PROJECT/Logs/cli_webgl.log"
```
Salida: `$PROJECT/Builds/WebGL` (Brotli + fallback).

Después del build:
```bash
git -C "$PROJECT" checkout -- Assets/Scenes
```

### Linux
Menú de Unity: **File ▸ Build Settings ▸ Linux** (o `BuildPipeline` con
`BuildTarget.StandaloneLinux64`). Salida en `Builds/Linux`.

> La build **release WebGL no es determinista**. Jugarla siempre antes de entregar.

---

## 5. Probar la build WebGL en local

```bash
cd "$PROJECT/Builds/WebGL"
python3 -m http.server 8080
# Abrir http://localhost:8080
```

No abrir `index.html` con `file://`.

### Prueba automatizada en navegador
```bash
NODE_PATH=/home/jenifrutica/Proyectos/legato/node_modules \
  node "$PROJECT/Docs/Tools/docs/webgl_play.js"
# (servir la build antes en un puerto; ver encabezado del script)
```

---

## 6. Capturas de evidencia

Requieren **gráficos** (sin `-nographics`) y `NITRO_SHOT_DIR`:

```bash
SHOT="/home/jenifrutica/SENA/NitroRythm/My project/Docs/Capturas/Despues/captura"
mkdir -p "$SHOT"
flatpak run --env=NITRO_SHOT_DIR="$SHOT" --env=DISPLAY=:0 \
  --command="$UNITY" com.unity.UnityHub \
  -batchmode \
  -runTests -testPlatform PlayMode \
  -testFilter NitroRhythm.Tests.GameplayEvidenceCapture \
  -projectPath "$PROJECT" \
  -testResults "$PROJECT/Logs/test_capture.xml" \
  -logFile "$PROJECT/Logs/cli_capture.log"
```

Generadores:
- `GameplayEvidenceCapture` (HUD, dominios, selección, resultados, villano).
- `CharacterDisplayAlignmentTests` (pilotos frente/lado).
- `GameplayFlowTests` (cámara).
- `LegacyEvidenceCapture` (estado “antes” simulado: menú, selección, pista, gameplay).

> Si el dispositivo gráfico es Null, los tests se **omiten** (por eso hay que quitar
> `-nographics`).

---

## 7. Modelos 3D (Blender)

```bash
flatpak run --command=blender org.blender.Blender --background \
  --python "$PROJECT/Docs/Tools/blender/generate_karts.py"
```
Exporta a `Assets/Models`; copiar a `Assets/Resources/NitroRhythm/Models`.

Otros scripts: `preview_model.py`, `inspect_model.py`, `measure_bounds.py`,
`prepare_props.py`, `render_views.py`, `contact_thumbs.py`, `hdr_to_jpg.py`.

---

## 8. Analítica (Google Forms)

```bash
python3 "$PROJECT/Docs/Tools/docs/generate_analytics.py"
```
- Si existe `~/Downloads/forms_responses.csv` (export de Forms) lo usa.
- Si no, usa `Docs/Analitica/respuestas_raw.csv`.
- Genera: `respuestas.csv`, `reporte_google_forms.md`, 3 PNG y `resumen_analitica.md`.

Formulario y hoja actuales:
- Form: https://docs.google.com/forms/d/e/1FAIpQLSeePkpwiZbBUEjVSZMQqB_Svr0BzVuGsiC_B5i-UrjwJA_rYg/viewform
- Sheet: https://docs.google.com/spreadsheets/d/1Z-BRPvt6kR823RYpHda_g-1CGeblpPiVRACWpRiF83M/edit

---

## 9. Documentos (md → docx → pdf)

```bash
python3 "$PROJECT/Docs/Tools/docs/md_to_docx.py" "$PROJECT/Docs/Evidencias"
cd "$PROJECT/Docs/Evidencias"
soffice --headless -env:UserInstallation=file:///tmp/lo_docs \
  --convert-to pdf --outdir . *.docx
```

Infografía:
```bash
python3 "$PROJECT/Docs/Tools/docs/make_infographic.py"   # PPTX para Canva
python3 "$PROJECT/Docs/Tools/docs/make_aa4_docx.py"       # DOCX AA4 con fuente negra
```

> `md_to_docx.py` reescribe el DOCX de AA4; después ejecutar `make_aa4_docx.py`
> para restaurar la versión en negro.

---

## 10. Git

```bash
cd "$PROJECT"
git status
git add -A
git commit -m "tipo: mensaje"   # sin Co-Authored-By
git push origin main
```
No hacer commit/push sin autorización de la usuaria.

---

## 11. Limpieza de procesos

```bash
# Buscar por PID (no usar pkill -f con patrones que aparezcan en tu comando)
ps -eo pid,cmd | grep -iE "Editor/Unity|http.server" | grep -v grep
kill -9 <PID>
```
Si un test batchmode queda colgado y suena audio, matar el PID de Unity.

---

## 12. Trampas conocidas

- `NitroRythm` vs `NitroRhythm`: usar la primera.
- `-nographics` ⇒ sin capturas (dispositivo Null).
- `ModelSanitizer` debe **desactivar** cámaras/luces de FBX, no destruirlas.
- Materiales base en `Resources/.../Materials` (si faltan, todo sale magenta).
- Recursos de TMP en `Assets/TextMesh Pro/` (si faltan, no hay texto).
- Release WebGL no determinista: probar en navegador.
- `Builds/`, `Logs/`, `Library/`, `Temp/` no se commitean.
