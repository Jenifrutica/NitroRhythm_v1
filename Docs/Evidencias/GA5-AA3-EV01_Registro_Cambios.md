# Registro de ajustes a las funcionalidades del prototipo NitroRhythm

**Evidencia:** GA5-220501087-AA3-EV01
**Productos:** archivos Unity de las escenas ajustadas + capturas antes/después
**Programa:** Tecnología en Desarrollo de Videojuegos y Entornos Interactivos — SENA

> Este registro ordena los cambios por nivel de importancia y afectación a las
> funcionalidades, según lo exige la evidencia. El estado **Antes** corresponde al
> prototipo inicial (mecánicas base en una sola escena) y **Después** al prototipo
> con las 4 escenas, UI Canvas y generación reactiva al audio.

---

## 1. Captura del estado inicial (Antes)

1. Abra **NitroRhythm ▸ Evidence Capture Tool**.
2. Carpeta destino: `Antes`.
3. Abra cada escena, entre en **Play** y capture el Game View:
   `00_MainMenu`, `01_CharacterSelect`, `02_Gameplay`, `03_Results`.
   (En el estado inicial existía una sola escena `SampleScene` con primitivas;
   regístrelo como punto de partida.)

## 2. Lista ordenada de cambios

| # | Prioridad | Cambio | Afectación |
| :--- | :--- | :--- | :--- |
| 1 | Alta | Separación en **4 escenas** (menú, selección, gameplay, resultados). | Estructura y navegación |
| 2 | Alta | **UI Canvas + TextMeshPro** (menú, opciones con sliders, pausa, HUD con velocímetro, barra de progreso y puntuación) en reemplazo de `OnGUI`. | Interfaces |
| 3 | Alta | **Generación de pistas reactiva al audio** (FFT + BPM): graves → huecos/saltos, agudos → obstáculos, BPM → velocidad y cadencia de disparo. | Mecánicas y concepto gráfico |
| 4 | Alta | **Modos de juego completos** (1P, 1P+Bot, Coop 2P, Showdown 3P) con viewports exactos. | Mecánicas |
| 5 | Media | **Sistema narrativo data-driven** (`DialogueSystem` + `CutsceneController`) leyendo `prototype_data.json`; 10 dominios musicales y retratos. | Niveles y concepto gráfico |
| 6 | Media | **Checkpoints intermedios** y reaparición conservando el impulso. | Niveles / interactividad |
| 7 | Media | **Abstracción de entrada** (`IInputProvider`) desacoplada de la física; bot reescrito. | Interactividad |
| 8 | Media | **Villano**: `FiringPoint` modular, proyectil parabólico corregido (`Lerp`+`Sin`), daño de proximidad. | Mecánicas |
| 9 | Media | **Modelos 3D** generados en Blender (Kart_Neon_01..03, pilotos, PremioMalo) con materiales URP y anclajes. | Concepto gráfico |
| 10 | Baja | **Temas de color por dominio** desde el concept art (Conservatorio → Harmonya). | Niveles / concepto gráfico |

## 3. Implementación en Modo Editor

Cada cambio se implementó en los scripts de `Assets/Scripts` y se refleja en las
escenas generadas por **NitroRhythm ▸ Build Prototype Scenes**.

## 4. Verificación en Modo Play

1. Abra `02_Gameplay`, pulse **Play** y verifique: movimiento, salto, pista
   reactiva, disparo del villano y HUD.
2. Pulse `Escape` para verificar la pausa.
3. Complete un nivel para verificar la cutscene y el paso a Resultados.

## 5. Captura del estado final (Después)

1. **Evidence Capture Tool** → carpeta `Despues`.
2. Capture las 4 escenas en Play.
3. Compare con las capturas de `Antes`.

## 6. Carpeta de entrega

```
Evidencias/
  Antes/    (capturas del estado inicial)
  Despues/  (capturas del estado ajustado)
```

> Entregue máximo 4 archivos Unity (las 4 escenas ajustadas) junto con esta
> carpeta de capturas.
