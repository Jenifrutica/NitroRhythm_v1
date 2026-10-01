# Plan de ejecución y estado — NitroRhythm (GA5-220501087)

Documento maestro con lo **hecho**, lo **pendiente** y el **paso a paso** para
cerrar las cuatro evidencias.

---

## 1. Estado actual

### Hecho (código + assets + build)
- [x] Proyecto Unity **6000.5.8f1** ordenado en `SENA/NitroRythm/My project`.
- [x] **4 escenas**: `00_MainMenu`, `01_CharacterSelect`, `02_Gameplay`, `03_Results` (registradas en Build Settings).
- [x] **Generación de pistas reactiva al audio** (FFT + BPM: graves→huecos/saltos, agudos→obstáculos, BPM→velocidad/cadencia).
- [x] **Multijugador local 1–3** con viewports exactos del GDD.
- [x] **Narrativa data-driven** (`prototype_data.json`: 10 dominios, Lyra/Karel/Vox, cutscenes).
- [x] **UI Canvas + TextMeshPro** rediseñada (menú, selección 3D, opciones, pausa, HUD, resultados).
- [x] **Rediseño gráfico 3D**: karts con modelo Blender, pista con rejilla neón, post-proceso (Bloom/Vignette/Chromatic Aberration).
- [x] **Modelos Blender** (`Kart_Neon_01..03`, `Piloto_Neon_01..03`, `PremioMalo.fbx`) con pipeline de anclajes.
- [x] **Pruebas automatizadas**: 10 PlayMode + 4 EditMode = **14/14 en verde**.
- [x] **Build WebGL** + `.zip` para compartir.
- [x] **Capturas** del estado final en `Evidencias/Despues`.
- [x] **Documentos**: cuestionario AA1, informe AA2 (con analítica), registro AA3, infografía AA4, guía de 5 usuarios.
- [x] **Analítica**: 3 usuarios simulados + 2 reales pendientes (`Analitica/`).

### Pendiente (acciones de la aprendiza)
- [ ] Publicar el prototipo y **aplicar el cuestionario a los 5 usuarios** (2 reales).
- [ ] Completar la fila de los 2 usuarios reales en `Analitica/respuestas.csv` y regenerar gráficos.
- [ ] **Capturar "Antes"** (versión previa) si se desea la comparativa; el "Después" ya está.
- [ ] Montar la **infografía comparativa** en Canva y exportar a PDF.
- [ ] Ajustes finos de balance (HUD ampliado y progresión) según hallazgos P4/P9.

---

## 2. Paso a paso

### 2.1 Probar el juego en local
```bash
cd "SENA/NitroRythm/Builds/WebGL"
python3 -m http.server 8080
# Abrir http://localhost:8080
```
(No abrir `index.html` con `file://`; el navegador bloquea el WebGL.)

### 2.2 Publicar para los 5 usuarios
- **Netlify Drop** (<https://app.netlify.com/drop>): arrastrar `Builds/WebGL`.
- **itch.io**: subir `NitroRhythm_WebGL.zip` como proyecto HTML.
- **AWS + dominio propio**: subir `Builds/WebGL` a **S3** (sitio estático) y usar
  **CloudFront** para HTTPS/caché. Costo muy bajo para un prototipo.

### 2.3 Aplicar el cuestionario
1. Crear el formulario en Google Forms con las 10 preguntas de
   `Documentos/GA5-AA1-EV01_Cuestionario`.
2. Compartir el enlace del juego y del formulario con **5 personas**.
3. Descargar el reporte de analítica.

### 2.4 Completar la analítica
Editar `Documentos/Analitica/respuestas.csv` (filas Real 1 y Real 2) y ejecutar:
```bash
python3 "SENA/NitroRythm/Tools/docs/generate_analytics.py"
```

### 2.5 Capturas antes/después (AA3/AA4)
- **Después**: ya generadas en `Evidencias/Despues` (menú, selección, gameplay, pausa).
- **Antes**: capturar la versión previa (prototipo de una sola escena) o usar las
  descripciones del registro AA3.
- En Unity: menú **NitroRhythm ▸ Evidence Capture Tool** para capturar en vivo.

### 2.6 Infografía comparativa (AA4)
1. Abrir Canva (plantilla de infografía vertical).
2. Copiar el contenido de `Documentos/GA5-AA4-EV01_Infografia_Comparativa`.
3. Colocar imágenes Antes/Después por aspecto (estructura, interfaces, gráfico, niveles, mecánicas).
4. Exportar a PDF y compartir el enlace.

---

## 3. Cómo reabrir/regenerar el proyecto

- Abrir `SENA/NitroRythm/My project` en Unity Hub (versión 6000.5.8f1).
- Las escenas ya existen; si se borran, se regeneran solas al abrir o con
  **NitroRhythm ▸ Build Prototype Scenes**.
- Recompilar WebGL: **NitroRhythm ▸ Build WebGL** (salida en `My project/Builds/WebGL`).

> Nota técnica: para compilar por línea de comandos, la licencia se resolvió
> ejecutando el editor **dentro del sandbox de Unity Hub**
> (`flatpak run --command=".../Unity" com.unity.UnityHub -batchmode ...`).

---

## 4. Controles

| Jugador | Conducir | Salto | Disparo |
| :-- | :-- | :-- | :-- |
| P1 (Lyra) | `W A S D` | `Espacio` | `Ctrl Izq.` |
| P2 (Karel) | `Flechas` | `Shift Der.` / `Numpad 0` | `Numpad 1` |
| P3 (Vox) | `I J K L` | `Shift Der.` / `Numpad 0` | `Enter` |
| Pausa | — | `Escape` | — |

---

## 5. Estructura de la entrega

```
SENA/NitroRythm/
  My project/            Proyecto Unity (Assets, escenas, tests, modelos)
  Builds/WebGL/          Build publicable + NitroRhythm_WebGL.zip
  Evidencias/
    Antes/               Capturas del estado inicial (pendiente)
    Despues/             Capturas del estado final (listo)
    Documentos/          Cuestionario, informes, guía, infografía (.md/.docx/.pdf)
      Analitica/         CSV + gráficos + resumen
  Tools/                 Scripts (Blender, generación de analítica, md→docx)
  README_Prototipo.md
```
