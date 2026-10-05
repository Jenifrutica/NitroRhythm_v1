# Justificación de controles — NitroRhythm

Proyecto GA5-220501087 · ficha 3336511. Los controles están implementados en
`Assets/Scripts/Controls/KeyboardInputProvider.cs` y se muestran al jugador en la tarjeta de introducción de cada nivel.

## Tabla de controles

| Jugador | Conducir | Salto | Disparo | Pausa |
| :-- | :-- | :-- | :-- | :-- |
| P1 (Lyra) | `W A S D` | `Espacio` | `Ctrl Izq.` | `Esc` |
| P2 (Karel) | `Flechas` | `Shift Der.` / `Numpad 0` | `Numpad 1` | `Esc` |
| P3 (Vox, villano) | `I J K L` | `U` | `Enter` (lanzar obstáculo) | `Esc` |

## Por qué estos controles

1. **Un esquema por zona del teclado.** El juego admite hasta tres jugadores locales en un solo teclado. WASD queda
   a la izquierda, las flechas a la derecha y IJKL en el centro-derecha, de modo que las manos de cada jugador no
   se cruzan ni se pisan teclas.
2. **Convenciones conocidas.** WASD y flechas son el estándar de los juegos de carreras en PC. Un jugador nuevo
   no necesita aprender nada: W acelera, S frena o retrocede, A/D giran.
3. **Pocas acciones, todas cerca de la mano.** Solo hay conducir, saltar y disparar. El salto está en la barra espaciadora
   (la tecla más grande, la que se pulsa sin mirar) porque es la acción más urgente: se usa para saltar huecos de la pista
   al compás de la música. El kart avanza continuamente, así que no hay tiempo de buscar una tecla.
4. **Entrada analógica simulada.** Throttle y Steer se leen como ejes de −1 a 1. Así el mismo `IInputProvider`
   sirve para el teclado, el bot (IA) y las pruebas automáticas (`ScriptedInputProvider`), y quedaría listo para
   un mando sin cambiar la lógica del kart.
5. **Compatibilidad con WebGL.** Se usa el backend de entrada clásico (`Input.GetKey`), que funciona igual en
   la build web y en la de escritorio, sin permisos extra del navegador.
6. **Acciones puntuales con "latch".** Salto y disparo se guardan hasta que el kart los consume
   (`ConsumeJump`/`ConsumeFire`), de modo que una pulsación rápida entre dos pasos de física no se pierde.
7. **Pausa única (`Esc`).** Una sola tecla común para todos, lejos de las teclas de juego para no pulsarla sin querer.
8. **Recordatorio visible.** La tarjeta de introducción de cada nivel (que congela el juego 2–3 s) muestra las
   teclas de P1 y P2; así el jugador las ve justo antes de empezar.

## Cómo se verificó

- Prueba automática `GameplayFlowTests.RealScene_KartDrivesAndCameraFollowsIt`: en la escena real inyecta entrada
  de acelerar y comprueba que el kart avanza más de 15 m, que la cámara lo sigue (target = P1, distancia < 25 m),
  que el juego no queda congelado tras la tarjeta de nivel y que hay una sola cámara y un solo AudioListener.
- Capturas de lo que ve la cámara de seguimiento mientras se conduce (`Docs/Capturas/Despues/ronda3/`).
