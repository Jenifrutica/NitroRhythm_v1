# Documentación — NitroRhythm

Índice de toda la documentación del proyecto.

## Empezar aquí

| Documento | Para qué |
| :-- | :-- |
| [`../AGENT_CONTEXT.md`](../AGENT_CONTEXT.md) | **Handoff autoritativo** para una persona/agente nueva: estado, rutas, arquitectura, trampas, pendientes. |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Arquitectura técnica detallada (scripts, flujo, datos, UI, mundo, pruebas). |
| [`RUNBOOK.md`](RUNBOOK.md) | Comandos exactos: pruebas, builds, capturas, Blender, analítica, docs. |
| [`PENDING.md`](PENDING.md) | Bugs, limitaciones y backlog priorizado. |
| [`CHANGELOG.md`](CHANGELOG.md) | Historial de rondas 1–4. |
| [`EVIDENCIAS.md`](EVIDENCIAS.md) | Mapa de las 4 evidencias y entregables. |
| [`TOOLS.md`](TOOLS.md) | Herramientas de Editor, Blender, documentos y WebGL. |
| [`ASSETS.md`](ASSETS.md) | Inventario de modelos, texturas, cielos, fuentes y materiales. |
| [`DEPLOY.md`](DEPLOY.md) | Builds de todas las plataformas y cómo publicarlas (web, GitHub, itch.io). |

## Evidencias (GA5-220501087)

| Archivo | Descripción |
| :-- | :-- |
| `Evidencias/GA5-AA1-EV01_Cuestionario.*` | Cuestionario (10 preguntas, escala 1–5). |
| `Evidencias/Guia_5_Usuarios.*` | Guía para publicar el prototipo y aplicarlo. |
| `Evidencias/GA5-AA2-EV01_Informe_Hallazgos.*` | Informe de hallazgos con analítica. |
| `Evidencias/GA5-AA3-EV01_Registro_Cambios.*` | Registro ordenado de ajustes (ronda 1). |
| `Evidencias/GA5-AA3-EV01_Registro_Cambios_Ronda2.*` | Ajustes ronda 2. |
| `Evidencias/GA5-AA3-EV01_Registro_Cambios_Ronda3.*` | Ajustes ronda 3. |
| `Evidencias/GA5-AA3-EV01_Registro_Cambios_Ronda4.*` | Ajustes ronda 4 (prueba con usuarios). |
| `Evidencias/GA5-AA3-EV01_Justificacion_Controles.*` | Justificación de controles. |
| `Evidencias/GA5-AA4-EV01_Infografia_Comparativa.*` | Contenido comparativo + DOCX fuente negra. |
| `Evidencias/GA5-AA4-EV01_Infografia.pptx` / `.pdf` | Infografía para Canva. |
| `Evidencias/ENLACES_EVIDENCIAS.md` | Enlaces exactos por evidencia. |
| `Evidencias/RESULTADO_PRUEBAS.md` | Resultado de las pruebas automáticas. |
| `Evidencias/PLAN_EJECUCION.md` | Plan de ejecución y estado. |
| `Evidencias/GA5_EVIDENCIAS_Y_JUSTIFICACION.*` | Justificación de cumplimiento de las 4 evidencias. |
| `Evidencias/README_ENTREGAS.txt` | Paso a paso por evidencia (entrega). |

## Recursos

| Carpeta | Descripción |
| :-- | :-- |
| `Analitica/` | `respuestas_raw.csv`, `respuestas.csv`, reporte Forms, 3 gráficos, resumen. |
| `Forms_Google/` | Preguntas listas y enlace del formulario. |
| `Capturas/Antes/` | Estado inicial (incluye `legacy/` simulado). |
| `Capturas/Despues/` | Estado final (ronda 1/2/3). |
| `Hunyuan/` | Vistas de referencia y prompts para Hunyuan3D. |
| `Tools/blender/` | Scripts de Blender (generar, previsualizar, medir, props, HDRI). |
| `Tools/docs/` | Analítica, md→docx, infografía, capturas WebGL, resumen de pruebas. |
| `Tools/extract_unitypackage.py`, `skies_horizon.py` | Utilidades. |
| `CREDITOS_Y_LICENCIAS.md` | Créditos y licencias de assets. |
| `README_Prototipo.md` | Guía técnica del proyecto Unity. |

## Formatos

Cada documento de evidencia está en **`.md`** (editable), **`.docx`** (Word) y
**`.pdf`** (entrega). Si editas un `.md`, regenera con `Tools/docs/md_to_docx.py`
y `soffice --convert-to pdf`.
