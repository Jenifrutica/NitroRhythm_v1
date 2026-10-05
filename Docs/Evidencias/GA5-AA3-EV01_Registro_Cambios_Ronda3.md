# Registro de cambios — Ronda 3 (cámara, controles y orientación de personajes)

Proyecto NitroRhythm · GA5-220501087 · ficha 3336511.

## Problemas reportados y causa

| # | Reporte | Causa encontrada | Corrección |
| :-- | :-- | :-- | :-- |
| 1 | La cámara muestra los premios (buenos y malos) y no el carro | `PrizeGood.fbx` traía una **cámara de Blender** incrustada. Cada premio generado añadía otra cámara a la escena (la prueba contó 8) y una de ellas dibujaba desde el premio | `Assets/Editor/ModelImportSanitizer.cs` elimina cámaras, luces y AudioListener de todo FBX al importarlo. Ahora hay 1 cámara y 1 AudioListener |
| 2 | El carro no se deja manejar / se siente lento | El rozamiento del suelo (`linearDamping = 4`) se aplicaba también mientras se aceleraba y frenaba el kart a ~15 u/s en vez de los 30–44 u/s del diseño | `PlayerKartController.ApplyDrive`: el rozamiento solo actúa al soltar el acelerador. Velocidad medida: 41 u/s |
| 2b | La cámara quedaba lejos a alta velocidad | Retraso del suavizado (`Smoothing 6`) | Offset (0, 4, −8.5) y suavizado 12 |
| 3 | Los personajes están al revés / no miran al frente | (a) Los modelos de piloto miran hacia +Z: en la vitrina el kart está girado 180° y el piloto no, así que miraba hacia atrás. (b) El vaivén arrancaba en una fase aleatoria y cada kart se veía en un ángulo distinto | Piloto girado 180° en la vitrina (en carrera el kart no gira, así que el piloto va sin giro), `pilotOffset` recalculado, vaivén ±10° desde la vista frontal con fase 0 |
| 3b | **Los karts no salían en la build web** (la primera v1.3) | La limpieza de modelos destruía la cámara del FBX en ejecución; URP lanza `NullReferenceException` en `UniversalAdditionalCameraData.OnDestroy` y en la build *release* eso rompe el dibujado de todo lo 3D (la build de depuración sí mostraba los karts, por eso solo se vio al jugar la release en el navegador) | `ModelSanitizer` (`Assets/Scripts/Core`) **desactiva** cámaras, luces y AudioListener de los FBX en lugar de destruirlos; el filtro de importación se eliminó |
| 3c | Mensaje «Esperando a Jugador 1 & Jugador 2…» visible desde el inicio en cooperativo | Se mostraba aunque nadie hubiera llegado a la meta | Solo aparece cuando alguien ya está en la meta (`LevelGoalTrigger`) |
| 4 | Justificar los controles | — | `Docs/Evidencias/GA5-AA3-EV01_Justificacion_Controles.md`; las teclas se muestran en la tarjeta de introducción de cada nivel |

## Ajustes posteriores a la prueba de la usuaria

| # | Reporte | Causa | Corrección |
| :-- | :-- | :-- | :-- |
| 5 | Sale el mismo personaje dos veces | (a) En cooperativo los dos karts aparecían **en el mismo punto** y el de Lyra tapaba al de Karel; (b) se podía elegir el mismo piloto para P1 y P2 | Cada jugador va en su carril (`LevelManager.GetCheckpointSpawn`), también al revivir; al elegir un piloto ya tomado por el otro jugador se intercambian; P2 nunca puede ser igual a P1 (`GameSession.GetCharacter`) |
| 6 | Revivir debe tardar 5 s con cuenta regresiva | El K.O. devolvía al kart al checkpoint al instante | `KartHealthSpeed`: K.O. → el kart se detiene y parpadea, **cuenta regresiva de 5 s** en pantalla (`REVIVIENDO EN 5…1`, centrada en la mitad de cada jugador), revive con vida completa; el villano ignora a los karts caídos |
| 7 | Lyra se ve descuadrada (piloto pequeño y hundido) | Su pose de loto hace que su caja de colisión sea grande y el piloto salía diminuto | `pilotScale` por personaje (Lyra 1,3) y `pilotOffset` recalculado |
| 8 | Línea fea a la izquierda de los botones | Barra de acento (`AccentBar`) de `UIFactory.CreateButton` | Eliminada en todos los botones |
| 9 | Retratos mal cortados | La imagen era rectangular dentro de un marco redondeado | `UIArt.RoundedPortrait`: esquinas redondeadas incorporadas al canal alfa del retrato |

| 10 | Debe revivir en el checkpoint, no donde muere | El teletransporte movía solo el `transform`; con la interpolación del Rigidbody la física lo sobrescribía y el kart seguía donde murió | `KartHealthSpeed.Revive` y `LevelManager.RepositionPlayersToCheckpoint` mueven el propio Rigidbody (`body.position`) y sincronizan la física. Prueba: muere en x≈86 con el checkpoint en x≈39 y revive en x≈39 |
| 11 | Cambiar la interfaz de cambio de nivel | La tarjeta estiraba la ilustración del dominio sin respetar su proporción, y el tirón de carga de la escena (varios segundos en el navegador) consumía su duración antes de verse | Rediseño: ilustración con marco y esquinas redondeadas a su proporción, nombre grande, regla de acento, descripción, chip de música, 7 segmentos de progreso, indicación de inicio, controles y barra de cuenta atrás; el tiempo por cuadro se limita a 0,1 s para que la tarjeta se vea completa |
| 12 | La selección de kart está desproporcionada | Los karts 3D estaban en posiciones fijas y con tamaño fijo, mientras las tarjetas de la interfaz escalan con la ventana: en el canvas del navegador (16:10) los karts eran más anchos que sus tarjetas y no quedaban sobre ellas | `CharacterSelectScreen.LayoutStage`: cada kart se coloca sobre el centro de su tarjeta y se escala según el ancho de la tarjeta, y se recalcula si cambia el tamaño de la ventana |

| 13 | Al llegar a la meta del nivel 1 no pasa al nivel 2 | La meta exigía que **todos** los jugadores estuvieran dentro de la zona **a la vez**; a ~41 u/s un kart la cruza en un cuarto de segundo y la sale, así que en cooperativo / con bot nunca coincidían | `LevelGoalTrigger`: las llegadas quedan registradas, el kart que llega se detiene en la meta, los jugadores K.O. o caídos no se esperan y, tras la primera llegada, los demás tienen 6 s (se muestra la cuenta) antes de pasar de nivel. Pruebas: `ReachingTheGoal_AdvancesToTheNextLevel` y `Cooperative_GoalAdvancesEvenIfThePartnerStaysBehind` |
| 14 | Los pilotos no están centrados en el kart | Estaban centrados de lado a lado pero sentados demasiado atrás (sobre el eje trasero) | `pilotOffset` adelantado (z) para los tres: ahora van sobre el asiento, en el centro del cockpit. Vistas desde arriba y de lado en `pilotos_en_carrera_arriba_y_lado.png` |
| 15 | No se sabe cuál kart está seleccionado | Solo cambiaba levemente el brillo del marco | Insignia «▼ JUGADOR 1 / JUGADOR 2» sobre la tarjeta elegida, tarjeta elegida más grande y brillante, las demás atenuadas, y en 3D se resaltan los karts elegidos |

| 16 | Lyra y Karel salen fuera del kart en la selección | `ModelNormalizer` mantiene constante el tamaño del modelo ante la escala del padre, pero la posición local del piloto sí se escalaba con el padre: al ajustar el tamaño de la vitrina el piloto se desplazaba | `ModelNormalizer.InheritParentScale`: en la vitrina el kart y el piloto escalan juntos, y quedan dentro del kart a cualquier tamaño de ventana |
| 17 | Guía de controles al pausar y al iniciar, por modo | No existía | `ControlsGuide` (tarjetas por jugador con sus teclas y reglas), `ControlsOverlay` (pantalla «CÓMO JUGAR» al inicio del primer nivel, una vez por partida, que congela el juego hasta pulsar), pausa rediseñada con la guía del modo actual y botón «CONTROLES» en la selección. Modos: 1 jugador (P1), jugador + bot (P1; P2 lo juega la IA), cooperativo (P1 y P2) y showdown (P1, P2 y villano P3) |
| 18 | En Showdown, P2 y P3 saltaban con la misma tecla | `Shift Der.` / `Numpad 0` estaban en ambos | El villano (P3) salta con `U` |

Nota técnica: una máscara de UI (`Mask`) para los retratos y el uso del stencil coincidieron con una build web sin 3D; se descartó la máscara. Además se comprobó que la compilación release de WebGL **no es determinista**: dos compilaciones del mismo código dieron resultados distintos (una sin 3D). Por eso cada build final se juega en el navegador (`webgl_play.js`) antes de entregarla.

## Pruebas

- 40 PlayMode + 4 EditMode, en verde.
- Nuevas: cuenta regresiva de 5 s (`KnockedOutKart_WaitsFiveSecondsThenRevivesAtCheckpoint`), P1≠P2 (`PlayerTwo_IsNeverTheSamePilotAsPlayerOne`), cooperativo con karts y cámaras distintos (`Cooperative_PlayersShowTheirOwnDistinctKartsAndCameras`), `GameplayFlowTests.RealScene_KartDrivesAndCameraFollowsIt` (el kart avanza, la cámara lo sigue, una sola cámara y un solo AudioListener) y `CaptureFollowCameraView` (capturas).
- Se estabilizaron tres pruebas que dependían del orden de ejecución (marcador de impacto, rango del villano, alineación del piloto).
- `CharacterDisplayAlignmentTests` ahora acumula los fallos de los tres personajes en lugar de detenerse en el primero.

## Verificación de la build final (v1.3)

- `Docs/Tools/docs/webgl_play.js`: juega la build WebGL en Chromium (menú → selección → carrera), guarda capturas y lista los errores de consola. Resultado: karts, pilotos, pista y HUD visibles, 0 errores (solo 2 avisos de la plataforma).
- Arranque de la build de Linux sin excepciones.
- 37 PlayMode + 4 EditMode en verde.
- Lección: probar siempre la build **release** jugándola, no solo el editor ni la prueba de humo de carga.

## Evidencia

- `Docs/Capturas/Despues/ronda3/guia_inicio_cooperativo.png`, `guia_SinglePlayer.png`, `guia_ThreePlayerShowdown.png`, `pausa_con_controles.png`, `web_guia_controles_inicio.png`: guía de controles.
- `Docs/Capturas/Despues/ronda3/seleccion_con_jugadores_marcados.png`: selección con la insignia de cada jugador.
- `Docs/Capturas/Despues/ronda3/tarjeta_nivel_1.png`, `tarjeta_nivel_3.png`, `tarjeta_nivel_7.png`, `web_tarjeta_nivel_secuencia.png`: nueva tarjeta de cambio de nivel.
- `Docs/Capturas/Despues/ronda3/coop_pantalla_dividida.png`, `gameplay_ko_cuenta_regresiva.png`, `seleccion_retratos_redondeados.png`.

- `Docs/Capturas/Despues/ronda3/cam_nivel1_1.png`, `cam_nivel1_3.png`, `cam_nivel4_2.png`: lo que ve la cámara de seguimiento en carrera.
- `Docs/Capturas/Despues/ronda3/seleccion_personajes.png`: selección con karts y pilotos de frente.
- `Docs/Capturas/Despues/ronda3/web_seleccion_build_final.png` y `web_carrera_build_final.png`: la build WebGL final jugada en el navegador.
- `Docs/Capturas/Despues/ronda3/personajes_frente_y_lado.png`: kart de frente / 3/4 y piloto de frente / lado, por personaje.
