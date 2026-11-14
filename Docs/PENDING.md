# PENDING.md — Pendientes, bugs y backlog

Estado a 2026-11-13. El proyecto está **jugable y con la evidencia cerrada**; esto
es lo que queda para una versión de producción.

---

## 1. Bugs / limitaciones conocidas

| # | Severidad | Descripción | Dónde | Nota |
| :-- | :-- | :-- | :-- | :-- |
| 1 | — | ~~Release WebGL “no determinista” sin 3D~~ → causa real (2026-11-14, ronda 6, diagnosticada instrumentando WebGL): el sol nunca proyecta sombras, así que Unity pide la variante de Lit **sin** sombras; como `PC_RPAsset` sí admitía sombras, el *stripping* dejaba variantes con sombras y, si faltaba la exacta, Unity elegía una con `sampler2DShadow` (`hlslcc_zcmp_MainLightShadowmapTexture`, `…AdditionalLightsShadowmapTexture`) enlazada a una textura sin comparación → WebGL2 descarta todo lo Lit. Qué variante se elegía cambiaba entre builds. Arreglo: sombras desactivadas en **ambos** assets de URP (no hay variantes de sombra). | `Assets/Settings/*_RPAsset.asset` | Seguir probando la build en navegador (`webgl_song_test.js`). |
| 2 | Baja | Animación esquelética hecha (ronda 6). Lyra lleva los brazos casi quietos: su vestido/pelo están pesados a los brazos y se deforman; en el kart de Karel una pieza del motor está soldada a la rueda trasera y gira con ella. | `rig_models.py`, `KartWheelRig` | Repintar pesos en Blender si se quiere más. |
| 3 | Baja | La canción del jugador se reproduce en **mono** (y a 32 kHz en web) para ahorrar memoria. El BPM supone tempo constante (70–180). | `UserSong`, `SongLoader` | Aceptable para el prototipo. |
| 4 | Baja | `SampleScene.unity` legacy y restos de la plantilla URP (`TutorialInfo`, `Readme.asset`, `SampleSceneProfile`, carpetas vacías). | `Assets/` | Se puede eliminar (ver `ASSETS.md` §12). |
| 5 | Baja | Código muerto: `GameModeManager` (solo por el enum `GameMode`), `ObstacleLauncher` (no enganchado). | `Core`, `Combat` | Limpiar o documentar. |
| 6 | Baja | El estado “antes” de las capturas es **simulado** (proto de cubos). | `Docs/Capturas/Antes/legacy` | No había capturas del original. |
| 7 | Baja | Respuestas del Forms son **simuladas** (enviadas por script). | `Docs/Analitica` | Si se quieren reales: limpiar respuestas y repetir. |
| 8 | Baja | `pilotOffset` por personaje es empírico. | `prototype_data.json` | Ajustar si cambian los modelos. |
| 9 | — | ~~Pruebas sin re-verificar~~ → re-ejecutadas el 2026-11-14: 58 OK, 0 fallos. | `RESULTADO_PRUEBAS.md` | Resuelto. |
| 10 | Baja | `webgl_play.js` dependía de `~/Proyectos/legato/node_modules`, que ya no existe. | `Docs/Tools/docs` | Instalar `playwright-core` (RUNBOOK §5). |

---

## 2. Backlog técnico (priorizado)

### Alta
1. ~~Animación de pilotos/karts~~ → hecho de forma procedural (2026-11-13); falta rig esquelético.
2. ~~UI de selección de música~~ → hecho: “TU CANCIÓN” (web + escritorio, 2026-11-13).
3. **Determinismo del build WebGL**: fijar pipeline de build reproducible y smoke test
   automático en CI antes de publicar.

### Media
4. **Selector de personaje/kart con estadísticas reales** (afectan manejo).
5. **Tabla de tiempos/puntuación** y guardado local (save system).
6. **Accesibilidad**: remapeo de teclas, escala de UI, daltonismo.
7. **Audio**: mezcla por buses (música/SFX/ambiente) y opción de mute.
8. **Limpiar código muerto** y `SampleScene`.
9. **Localización** (ES/EN) para textos de UI y narrativa.

### Baja
10. **Mobile/touch** (joystick virtual, botones) + módulo Android.
11. **Multijugador online** (hoy solo local).
12. **Más dominios** y cinemáticas 3D de intro/encrucijada/final.
13. **VFX** con Visual Effect Graph.
14. **CI** (GitHub Actions) con tests y build.

---

## 3. Evidencias (SENA) — lo que queda operativo

- **AA1**: subir `Entregas/GA5-AA1-EV01/` (cuestionario + analítica).
- **AA2**: subir `Entregas/GA5-AA2-EV01/` (informe).
- **AA3**: subir `AA3-Ajustes_Unity.zip` (ya comprimido). Nota: `Entregas/GA5-AA3-EV01/`
  incluye también el PDF del informe AA2; quitarlo si no corresponde.
- **AA4**: pegar el enlace de Canva (`https://canva.link/j1aecdvqxvhjluh`) y subir el PDF.

ZIPs finales ya generados en `Downloads/Evidencias_GA5_NitroRhythm/`. Allí hay además
dos carpetas sueltas `AA4-Infografia` y `AA4-Infografia (2)` (descomprimidas/duplicadas):
revisar antes de subir para no adjuntar la equivocada.

---

## 4. Deuda de documentación

- `CONTEXTO_PROYECTO.md` queda como **histórico**; el documento vigente es
  `AGENT_CONTEXT.md`.
- `Docs/PENDIENTE_PROXIMA_SESION.md` está **obsoleto** (describía un bloqueo ya
  resuelto); se conserva solo como registro. No usarlo como estado actual.
- Regenerar `.docx/.pdf` si se editan los `.md`, incluidas las copias de
  `SENA/NitroRythm/Entregas/Documentacion_Tecnica/`.

---

## 5. Criterios de “listo para producción”

- [ ] 60 FPS estables en el hardware objetivo (medido con Profiler).
- [ ] Build WebGL determinista y verificada en 3 navegadores.
- [ ] Sin código muerto ni `SampleScene`.
- [ ] Save system y tabla de tiempos.
- [ ] Accesibilidad básica (remapeo, escala, contraste).
- [ ] Localización ES/EN.
- [ ] Pruebas estables repetidas 3 veces.
