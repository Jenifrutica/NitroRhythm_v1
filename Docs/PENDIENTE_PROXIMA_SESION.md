# Pendiente para la próxima sesión (2026-10-04, al cerrar)

## Estado
- Código y pruebas: 39 PlayMode + 4 EditMode en verde (antes de cambiar los retratos). Sin commit ni push.
- Hecho en esta tanda (en el proyecto, sin commitear): cuenta regresiva de 5 s al quedar K.O. (`KartHealthSpeed`, `PlayerKartController.ControlLocked`, HUD `GameplayHud`), P1 y P2 siempre distintos (`GameSession.GetCharacter`, `CharacterSelectScreen.Assign`), botones sin la línea lateral (`UIFactory.CreateButton`), retratos con esquinas redondeadas horneadas en el sprite (`UIArt.RoundedPortrait`), piloto de Lyra más grande y centrado (`pilotScale` en `prototype_data.json`).

## PROBLEMA ABIERTO (bloqueante)
La **build WebGL release no dibuja nada en 3D** (sin karts ni pista; solo UI y cielo), con ~259 avisos `GL_INVALID_OPERATION: Mismatch between texture format and sampler type` y sin excepciones. Es determinista.
- Funcionó: la build hecha tras cambiar `ModelSanitizer` a *desactivar* (en vez de destruir) cámaras/luces de los FBX; ahí había 0 errores y se veían karts y pista.
- Falló después de: cuenta regresiva K.O., `GameplayHud`, botones, `CharacterSelectScreen`, retratos (con `Mask` y luego horneados; quitar la máscara NO lo arregló), `pilotScale`, `GameSession`.
- La build de depuración (`PrototypeSceneBuilder.BuildWebGLDebug`) sí dibujaba en una ocasión anterior.
- Plan: bisecar (revertir por grupos de cambios y compilar la web, ~10 min cada una), probando con `Docs/Tools/docs/webgl_play.js` (`NODE_PATH=/home/jenifrutica/Proyectos/legato/node_modules`, servir la build con `python3 -m http.server <puerto nuevo>`; NO `pkill -f`). Sospechosos: `UIArt.RoundedPortrait` (Blit/ReadPixels a RenderTexture en WebGL), cambios del HUD, `CharacterDisplay`/`pilotScale`.
- Un build interrumpido al apagar: `Builds/WebGL` puede estar incompleta. `Builds/Linux` y los zips `NitroRhythm_*_v1.3.zip` son de ANTES de los retratos horneados; el zip web v1.3 actual está roto. No subirlos al Release.

## Después de arreglarlo
1. Correr toda la suite (PlayMode + EditMode) y repetirla para comprobar que es estable.
2. Compilar WebGL y Linux (`git checkout -- Assets/Scenes` tras cada build), jugar la web con `webgl_play.js` y mirar las capturas (karts, pilotos de frente, retratos redondeados, botones sin línea).
3. Verificar a mano: K.O. con cuenta regresiva de 5 s, P1 y P2 distintos en carrera.
4. Regenerar zips v1.3, copiar capturas a `Docs/Capturas/Despues/ronda3/` y actualizar `Docs/Evidencias/GA5-AA3-EV01_Registro_Cambios_Ronda3.md` (añadir: revivir con cuenta regresiva, personajes distintos, botones, retratos, Lyra).
5. Solo si la usuaria lo pide: commit/push (sin `Co-Authored-By`), subir la build web al Release.
