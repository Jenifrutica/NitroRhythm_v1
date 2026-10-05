# Registro de cambios — Ronda 4 (prueba con usuarios y versión final)

**Evidencia:** GA5-220501087-AA3-EV01 (continuación de los registros de rondas 1–3)
**Productos:** ajustes en Unity (scripts, datos y assets) + capturas antes/después
**Programa:** Tecnología en Desarrollo de Videojuegos y Entornos Interactivos — SENA
**Aprendiz:** Jenifer Daniela Urbano Córdoba · Ficha 3336511

> Cuarta ronda: se aplicó la **prueba de funcionalidad a 5 usuarios** mediante un
> formulario de Google (10 preguntas con escala 1–5 + 1 abierta). Los hallazgos se
> procesaron y **quedaron implementados en la versión final** del prototipo. Este
> registro ordena los cambios por importancia y afectación, como exige la evidencia.

**Formulario:** <https://docs.google.com/forms/d/e/1FAIpQLSeePkpwiZbBUEjVSZMQqB_Svr0BzVuGsiC_B5i-UrjwJA_rYg/viewform>
**Respuestas:** <https://docs.google.com/spreadsheets/d/1Z-BRPvt6kR823RYpHda_g-1CGeblpPiVRACWpRiF83M/edit>
**Analítica:** `Docs/Analitica/` (CSV, reporte estilo Forms y 3 gráficos).
**Promedio general:** 4.22 / 5.

---

## 1. Estado inicial de esta ronda (Antes)

Capturas del prototipo antes de la ronda de ajustes (ronda 1 y ronda 2):

![Antes — HUD original](../Capturas/Antes/03_Gameplay_HUD_original.png)

![Antes — selección de personajes](../Capturas/Antes/02_CharacterSelect_original.png)

![Antes — pista de un solo estilo con el bot cubo](../Capturas/Antes/ronda2/gameplay_cubos_y_un_solo_estilo.png)

---

## 2. Lista ordenada de cambios (por importancia y afectación)

| # | Prioridad | Hallazgo de los usuarios | Cambio implementado (versión final) | Archivos | Afectación |
| :-- | :-- | :-- | :-- | :-- | :-- |
| 1 | Alta | **P4 (3.80):** "el HUD podría dar más información" | HUD ampliado: **tiempo** (`mm:ss.cc`), **posición en pista**, **distancia al villano**, **potenciadores** con temporizador y vida de P1 y P2 | `GameplayHud.cs`, `PlayerKartController.cs`, `KartHealthSpeed.cs` | Interfaces |
| 2 | Alta | **P9 (3.60):** "la dificultad sube rápido" | Progresión suavizada: huecos −40 % y menos peligros en el primer dominio, tramo seguro inicial; checkpoints más frecuentes en los dominios 1–2 | `ProceduralTrackBuilder.cs`, `LevelManager.cs` | Niveles / mecánicas |
| 3 | Alta | **P6 (4.00):** "poco aviso al recibir un impacto" | Sacudida de cámara, destello rojo, aviso "IMPACTO! −15 VIDA", vida parpadeando y **sonido de golpe**, además del chip "RALENTIZADO" | `KartHealthSpeed.cs`, `CameraFollow.cs`, `GameplayHud.cs`, `SfxLibrary.cs` | Interactividad / audio |
| 4 | Media | "la cámara a veces se aleja en las curvas" | Cámara de seguimiento con offset (0, 4, −8.5) y suavizado 12, y ligera sacudida solo en impactos | `CameraFollow.cs`, `TestSceneBootstrapper.cs` | Interactividad |
| 5 | Media | "subiría la dificultad gradual" | Curva por dominio (D1 ×0.60 → D7 ×1.00) y 3 tramos accesibles de inicio | `ProceduralTrackBuilder.cs` | Niveles |
| 6 | Media | "que se entienda qué jugador soy" | Insignia "▼ JUGADOR 1 / JUGADOR 2" en la selección, tarjeta elegida resaltada y **guía de controles** por modo al iniciar y en pausa | `CharacterSelectScreen.cs`, `ControlsGuide.cs`, `ControlsOverlay.cs`, `PauseMenu.cs` | Interfaces |
| 7 | Baja | "los pilotos deben ir sentados" | `pilotOffset`/`pilotScale` por personaje; los tres pilotos quedan sentados en el kart | `CharacterDefinition.cs`, `CharacterDisplay.cs`, `prototype_data.json` | Concepto gráfico |

---

## 3. Detalle de los ajustes

### 3.1 HUD ampliado (P4)
Se añadieron al HUD: tiempo de carrera, posición "POSICIÓN 2/3", distancia "VOX A 115 m",
chips de potenciador ("TURBO x1.6 3.0 s" y "RALENTIZADO −40 % 3.0 s") y la vida de P1 y P2.
![Después — HUD ampliado](../Capturas/Despues/gameplay/gameplay_impacto_checkpoint_turbo.png)

### 3.2 Progresión y checkpoints (P9)
Rampa por dificultad del dominio y checkpoints cada 2 plataformas en los dominios 1–2;
arcos neón **ámbar → verde** con aviso "CHECKPOINT" y sonido.
![Después — gameplay con checkpoints](../Capturas/Despues/gameplay/gameplay_conduciendo.png)

### 3.3 Feedback de impacto (P6)
Sacudida de cámara (0.4 s), destello rojo, aviso central "IMPACTO!" y sonido de impacto.
![Después — resultados y HUD tras el ajuste](../Capturas/Despues/gameplay/resultados_con_tiempo.png)

### 3.4 Cámara, dificultad gradual y guía de controles
Cámara más estable, curva de dificultad por dominio y guía de controles por modo
(1 jugador, jugador + bot, cooperativo y showdown) al iniciar el primer nivel y en la pausa.

---

## 4. Implementación en Modo Editor

Los ajustes están en `Assets/Scripts` y en `Assets/Resources/NitroRhythm/prototype_data.json`.
El HUD, los checkpoints, la guía de controles y los efectos se construyen en tiempo de
ejecución, por lo que las 4 escenas se regeneran sin cambios de estructura
(**NitroRhythm ▸ Build Prototype Scenes**) y conservan la misma jerarquía.

## 5. Verificación en Modo Play

| Conjunto | Pruebas | Resultado |
| :-- | :-- | :-- |
| PlayMode | 36 (+5 generadores de capturas) | **en verde** |
| EditMode | 4 | **en verde** |

Ver el detalle en `Docs/Evidencias/RESULTADO_PRUEBAS.md` (40 aprobadas, 0 fallidas).

## 6. Captura del estado final (Después)

| Aspecto | Antes | Después |
| :-- | :-- | :-- |
| HUD | `Capturas/Antes/03_Gameplay_HUD_original.png` | `Capturas/Despues/gameplay/gameplay_impacto_checkpoint_turbo.png` |
| Selección | `Capturas/Antes/02_CharacterSelect_original.png` | `Capturas/Despues/gameplay/seleccion_personajes.png` |
| Niveles | `Capturas/Antes/ronda2/gameplay_cubos_y_un_solo_estilo.png` | `Capturas/Despues/ronda2/RESUMEN_dominios.png` |
| Pilotos | `Capturas/Antes/piloto/*.png` | `Capturas/Despues/piloto/*.png` |

## 7. Resultado

Los hallazgos P4, P6 y P9 quedaron **atendidos e implementados en la versión final** y
verificados con las pruebas automáticas. Carpeta de entrega:
`SENA/NitroRythm/Entregas/GA5-AA3-EV01/`.
