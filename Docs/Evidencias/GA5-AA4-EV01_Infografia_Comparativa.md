# Infografía comparativa — Ajustes realizados al prototipo NitroRhythm

**Evidencia:** GA5-220501087-AA4-EV01
**Producto:** Infografía comparativa (plantilla gratuita) + PDF
**Programa:** Tecnología en Desarrollo de Videojuegos y Entornos Interactivos — SENA

> Contenido listo para montar en **Canva** (plantilla gratuita). Cada sección
> incluye la descripción *Antes* y *Después*; inserte las imágenes capturadas con
> **Evidence Capture Tool** (`Evidencias/Antes` y `Evidencias/Despues`).

---

## Encabezado

**NITRORHYTHM** — Prototipo funcional · Comparativa antes / después de los ajustes
Evidencia GA5-220501087-AA4-EV01 · SENA · Jenifer Daniela Urbano Córdoba

---

## 1. Estructura e interfaces del juego

**Antes:** una única escena de prueba (`SampleScene`) con menú y HUD dibujados por
código (`OnGUI`), sin separación de pantallas ni navegación formal.

**Después:** cuatro escenas diferenciadas — **Menú principal**, **Selección de
piloto**, **Gameplay** y **Resultados** — con interfaces construidas en **Canvas +
TextMeshPro** (menú con botones, opciones con sliders de audio, pausa y HUD con
velocímetro radial, barra de progreso y puntuación).
_[Imagen Antes: menú OnGUI] → [Imagen Después: menú Canvas]_

## 2. Concepto gráfico (imágenes y audios)

**Antes:** entidades con primitivas cúbicas de color plano; sin audio reactivo.

**Después:** estética neón URP con **modelos 3D generados en Blender** (tres karts,
pilotos y proyectil), paletas por personaje (Lyra teal, Karel azul, Vox rojo),
temas de color por dominio y **música sintetizada con generación de pista reactiva
al audio**.
_[Imagen Antes: cubos] → [Imagen Después: kart con modelo 3D y HUD]_

## 3. Niveles del videojuego

**Antes:** pista procedural lineal sin temática ni narrativa.

**Después:** **10 dominios musicales** (Conservatorio → Percusalia → Echoris →
Bassline Abyss → Arpeggion → Treble Spire → Noctua Chord → Encrucijada → Void
Crescendo → Harmonya), 7 jugables con dificultad progresiva, **checkpoints
intermedios** y cutscenes de escape del villano.
_[Imagen Antes: pista gris] → [Imagen Después: dominio con tema dorado/azul]_

## 4. Mecánicas del videojuego

**Antes:** movimiento básico y villano estático; proyectil con trayectoria
inconsistente.

**Después:** karts arcade con salto y reaparición conservando impulso, **4 modos
multijugador local** (1P, 1P+Bot, Coop 2P, Showdown 3P), villano que persigue y
dispara con **cadencia sincronizada al BPM**, impacto con pérdida de vida y
penalización de velocidad, y pista cuya dificultad nace de la música.
_[Imagen Antes: gameplay simple] → [Imagen Después: split-screen co-op con HUD]_

## 5. Conclusión de la comparativa

- La **estructura** pasó de un único escenario a un flujo completo de pantallas.
- Las **interfaces** migraron a Canvas/TMP, mejorando legibilidad y evaluación.
- El **concepto gráfico** ganó identidad (3D, neón, audio reactivo).
- Los **niveles** incorporaron progresión, tema y narrativa.
- Las **mecánicas** se completaron con multijugador, combate y checkpoints.

---

### Instrucciones de montaje en Canva

1. Cree un diseño de infografía vertical (1080 × 2400 px).
2. Copie cada sección y coloque las imágenes Antes / Después en paralelo.
3. Use los colores del juego: cian `#33CCFF`, dorado `#FBBF24`, rojo `#FF0000`.
4. Exporte a **PDF** y comparta el enlace.
