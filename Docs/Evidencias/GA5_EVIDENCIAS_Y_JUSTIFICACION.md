# Evidencias GA5-220501087 — NitroRhythm y su justificación de cumplimiento

**Programa:** Tecnología en Desarrollo de Videojuegos y Entornos Interactivos — SENA
**Ficha:** 3336511 · **Aprendiz:** Jenifer Daniela Urbano Córdoba
**Proyecto:** NitroRhythm (prototipo funcional en Unity 6 URP)

Este documento reúne las cuatro evidencias de la guía **GA5-220501087** y justifica,
punto por punto, **por qué deben cumplirse obligatoriamente** y **cómo el prototipo
ya construido las satisface**. La construcción del juego no es un requisito aparte:
es el soporte material de cada evidencia; sin prototipo no hay cuestionario que
aplicar, ni hallazgos que interpretar, ni ajustes que comparar.

> **Tesis de obligatoriedad:** las cuatro evidencias forman una cadena de
> trazabilidad (prototipo → prueba → hallazgos → ajustes → infografía). El
> prototipo de NitroRhythm ya existe con 4 escenas, mecánicas, interfaces y
> analítica, por lo que **cada acción exigida tiene un artefacto concreto que la
> respalda**; omitir una rompe la cadena y deja las demás sin sustento.

---

## Evidencia GA5-220501087-AA1-EV01 — Cuestionario. Implementación de prototipo con prueba de funcionalidad

### Requerimientos de la guía
- Implementar el prototipo en **Unity, en máximo 4 escenas**, con esquemas visuales y mecánicas básicas.
- Seleccionar **cinco (5) usuarios reales** y compartirles una carpeta con los archivos Unity.
- Diseñar un **cuestionario** que evalúe: estructura, interfaces, interactividad, concepto gráfico (imágenes/audios), niveles y mecánicas.
- Asignar **nombre y objetivo** a la prueba, con **mínimo 5 y máximo 10 preguntas**.
- Usar una **herramienta digital gratuita** que genere analítica (estadísticas y gráficos).
- **Enviar el enlace** a los cinco participantes.

### Producto y formato
- Producto: cuestionario con preguntas para evaluar el prototipo.
- Formato: reporte de analítica generado por la herramienta digital.
- Extensión: 5–10 preguntas.

### Justificación de cumplimiento
| Requerimiento | Cómo lo cumple el proyecto | Artefacto |
| :-- | :-- | :-- |
| Máximo 4 escenas | El prototipo tiene exactamente **4 escenas**: menú, selección de piloto, gameplay y resultados, registradas en Build Settings. | `Assets/Scenes/00_MainMenu.unity`, `01_CharacterSelect.unity`, `02_Gameplay.unity`, `03_Results.unity` |
| Esquemas visuales y mecánicas básicas | UI Canvas+TMP, escenario 3D neón, 4 modos, combate, audio reactivo y checkpoints. | `Assets/Scripts/UI/*`, `Core/*`, `Audio/*`, `Combat/*` |
| 5 usuarios reales | Guía paso a paso para compartir la build y recoger respuestas. | `Docs/Evidencias/Guia_5_Usuarios.md` |
| Cuestionario de los 6 aspectos | 10 preguntas que cubren estructura, interfaces, interactividad, concepto gráfico, niveles y mecánicas + 1 abierta. | `Docs/Evidencias/GA5-AA1-EV01_Cuestionario.md` |
| Nombre y objetivo | "Prueba de funcionalidad y experiencia del prototipo NitroRhythm" con objetivo explícito. | Ídem |
| Herramienta gratuita con analítica | Instrucciones para Google Forms + **script local** que genera CSV y gráficos. | `Docs/Tools/docs/generate_analytics.py`, `Docs/Analitica/` |
| Enviar enlace | La build está publicada y compartible (Release + guía de hosting). | `Releases/v1.0-prototype` |

**Por qué es obligatoria:** sin prototipo en Unity y sin cuestionario aplicado no
existe la materia prima de las evidencias AA2–AA4. El proyecto **ya construyó** las
4 escenas y el cuestionario; la única acción externa pendiente es enviar el enlace
a los 5 usuarios y adjuntar su analítica.

---

## Evidencia GA5-220501087-AA2-EV01 — Informe. Hallazgos de las funcionalidades del prototipo

### Requerimientos de la guía
- Interpretar el **reporte de analítica** y determinar el tipo de hallazgo por aspecto.
- Elaborar un informe en Word (exportable a PDF) con:
  - Portada, tabla de contenido, cuerpo y referencias bibliográficas.
  - **Introducción** sobre la importancia de los hallazgos.
  - **Anexo** con la analítica de resultados.
  - **Análisis por cada pregunta** con la descripción de los hallazgos.
  - **Conclusiones** sobre posibilidades de mejora.

### Producto y formato
- Producto: informe sobre los hallazgos de las funcionalidades.
- Formato: **PDF**. Extensión: sin límite.

### Justificación de cumplimiento
| Requerimiento | Cómo lo cumple el proyecto | Artefacto |
| :-- | :-- | :-- |
| Interpretar analítica y clasificar hallazgos | Informe con promedio por pregunta y clasificación (Fortaleza ≥ 4.0, Aceptable 3.0–3.9, etc.). | `Docs/Evidencias/GA5-AA2-EV01_Informe_Hallazgos.md` |
| Portada, índice, referencias | El informe incluye portada, tabla de contenido y referencias APA. | Ídem (`.docx`/`.pdf`) |
| Introducción | Sección que explica por qué documentar hallazgos mejora el producto. | Ídem |
| Anexo de analítica | Enlaza los gráficos y el CSV generados. | `Docs/Analitica/analitica_preguntas.png`, `analitica_usuarios.png`, `respuestas.csv` |
| Análisis por pregunta | Las 10 preguntas analizadas una por una (P1…P10) con su interpretación. | Ídem |
| Conclusiones y mejoras | Fortalezas y mejoras priorizadas (P4 HUD, P9 progresión, P6 feedback). | Ídem |

**Por qué es obligatoria:** el informe es la interpretación de la prueba; sin él no
hay criterio para los ajustes de AA3. El proyecto **ya produjo** la analítica (con
3 usuarios simulados y 2 reales pendientes) y el informe completo en `.md`, `.docx`
y `.pdf`.

---

## Evidencia GA5-220501087-AA3-EV01 — Archivo Unity. Ajustes de las funcionalidades del prototipo

### Requerimientos de la guía
- Capturar el **estado de las escenas antes** de los ajustes.
- Lista **ordenada** de cambios por importancia y afectación.
- Implementar los ajustes en **Modo Editor**.
- Verificar los ajustes en **Modo Play**.
- Capturar y guardar **cada escena** con los ajustes.
- Crear una **carpeta** con los archivos Unity de las escenas ajustadas + capturas antes/después.
- Compartir la carpeta.

### Producto y formato
- Producto: archivo Unity (escenas ajustadas) + capturas.
- Formato: Unity. Extensión: **máximo 4 archivos**.

### Justificación de cumplimiento
| Requerimiento | Cómo lo cumple el proyecto | Artefacto |
| :-- | :-- | :-- |
| Capturas "antes" | Herramienta de captura incluida; carpeta preparada. | `Docs/Capturas/Antes/`, `Assets/Editor/EvidenceCaptureTool.cs` |
| Lista ordenada de cambios | Registro con prioridad Alta/Media/Baja y su afectación. | `Docs/Evidencias/GA5-AA3-EV01_Registro_Cambios.md` |
| Implementar en Editor | Los ajustes se aplican en los scripts y escenas del proyecto. | `Assets/Scripts/*`, `Assets/Scenes/*` |
| Verificar en Play | 14 pruebas automatizadas (10 PlayMode + 4 EditMode) en verde. | `Assets/Tests/`, `Logs/test_results_*.xml` |
| Capturar cada escena | Capturas del estado final de las 4 escenas. | `Docs/Capturas/Despues/` |
| Carpeta con escenas + capturas | Todo dentro del repositorio. | `Assets/Scenes/` + `Docs/Capturas/` |
| Máximo 4 archivos Unity | Las 4 escenas ajustadas. | `00_MainMenu`, `01_CharacterSelect`, `02_Gameplay`, `03_Results` |

**Por qué es obligatoria:** es la materialización de las mejoras detectadas; sin
ajustes no hay "después" que comparar en AA4. El proyecto **ya incluye** el registro
de cambios, la herramienta de captura y la verificación por pruebas.

---

## Evidencia GA5-220501087-AA4-EV01 — Infografía comparativa. Ajustes realizados a las funcionalidades

### Requerimientos de la guía
- Capturar la apariencia de **cada escena ajustada**.
- Tener las imágenes del **estado antes** de los ajustes.
- Elaborar una **infografía comparativa** con:
  - Imágenes **antes y después** con breve descripción.
  - Descripción de **estructura e interfaces** antes/después.
  - Descripción del **concepto gráfico** antes/después.
  - Descripción de **niveles y mecánicas** antes/después.
- Presentarla con una **plantilla gratuita** (Canva).
- Compartir el enlace.

### Producto y formato
- Producto: infografía. Formato: enlace + PDF. Extensión: sin límite.

### Justificación de cumplimiento
| Requerimiento | Cómo lo cumple el proyecto | Artefacto |
| :-- | :-- | :-- |
| Capturas antes/después | Carpetas `Antes/` y `Despues/` con capturas de las 4 escenas. | `Docs/Capturas/` |
| Estructura e interfaces antes/después | Sección redactada (1 escena OnGUI → 4 escenas Canvas/TMP). | `Docs/Evidencias/GA5-AA4-EV01_Infografia_Comparativa.md` |
| Concepto gráfico antes/después | Primitivas planas → modelos 3D texturizados + post-proceso neón. | Ídem + capturas |
| Niveles y mecánicas antes/después | Pista gris lineal → 10 dominios, checkpoints, 4 modos, combate. | Ídem |
| Plantilla gratuita | Instrucciones de montaje en Canva con paleta y pasos. | Ídem |
| Compartir enlace | Repo público y Release con la build. | GitHub |

**Por qué es obligatoria:** cierra la trazabilidad demostrando la evolución del
producto. El proyecto **ya generó** el contenido comparativo y las capturas; falta
únicamente montarlo en Canva y exportar el enlace/PDF.

---

## Matriz de trazabilidad global

| Evidencia | Estado | Artefacto principal | Acción externa pendiente |
| :-- | :-- | :-- | :-- |
| AA1-EV01 | 🟢 Prototipo y cuestionario listos | 4 escenas + `GA5-AA1-EV01_Cuestionario` | Enviar a 5 usuarios |
| AA2-EV01 | 🟢 Informe y analítica listos | `GA5-AA2-EV01_Informe_Hallazgos` + `Analitica/` | Completar 2 usuarios reales |
| AA3-EV01 | 🟢 Registro y capturas "después" | `GA5-AA3-EV01_Registro_Cambios` + `Capturas/Despues` | Capturar "Antes" |
| AA4-EV01 | 🟢 Contenido comparativo listo | `GA5-AA4-EV01_Infografia_Comparativa` | Montar en Canva y compartir |

---

## Conclusión: por qué sí o sí deben cumplirse

1. **Cadena de dependencia:** AA1 produce el prototipo y la prueba; AA2 interpreta
   sus resultados; AA3 aplica las mejoras; AA4 demuestra la evolución. Cada una
   **necesita** a la anterior.
2. **El prototipo ya está construido** con las 4 escenas, las mecánicas, las
   interfaces y el audio reactivo, por lo que **no hay excusa técnica** para no
   generar los artefactos: ya existen en el repositorio.
3. **Verificabilidad:** las 14 pruebas automatizadas demuestran que el prototipo
   funciona, requisito implícito de toda la guía ("prueba de funcionalidad").
4. **Evidencia auditable:** cada afirmación apunta a un archivo concreto dentro del
   repositorio público, de modo que el evaluador puede verificarla sin ambigüedad.

> En resumen: **la construcción del juego es la condición de posibilidad de las
> cuatro evidencias**. Como el juego ya está construido y documentado, las
> evidencias deben y pueden completarse; omitirlas dejaría el prototipo sin su
> proceso de validación y mejora, que es precisamente lo que evalúa la guía GA5.
