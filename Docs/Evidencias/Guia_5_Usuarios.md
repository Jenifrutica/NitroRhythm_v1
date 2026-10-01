# Guía para la prueba con 5 usuarios reales (GA5-AA1-EV01)

Esta guía explica cómo compartir el prototipo con cinco usuarios reales y recoger
la evidencia que exige la actividad.

## 1. Preparar el prototipo

1. En Unity: **NitroRhythm ▸ Build Prototype Scenes** (una vez).
2. En Unity: **NitroRhythm ▸ Build WebGL**.
3. El resultado queda en `SENA/NitroRythm/Builds/WebGL`.

## 2. Publicar el prototipo (opciones gratuitas)

- **itch.io**: cree una página de proyecto "HTML" y suba el .zip de la build WebGL.
  Genere un enlace público.
- **Netlify Drop** (<https://app.netlify.com/drop>): arrastre la carpeta de la build
  y obtenga un enlace inmediato.
- **GitHub Pages**: suba la build a un repositorio y active Pages.
- **AWS (con su dominio)**: suba la build a **Amazon S3** (sitio estático) y use
  **CloudFront** para HTTPS y caché. Es de bajo costo para tráfico pequeño
  (S3 + CloudFront tienen capa gratuita y el costo mensual para un prototipo es de
  centavos). Configure el dominio con **Route 53** o el DNS de su proveedor.

## 3. Crear el cuestionario

1. Cree el formulario en **Google Forms** copiando las 10 preguntas del archivo
   `GA5-AA1-EV01_Cuestionario.md`.
2. Active el resumen de respuestas (gráficos automáticos).

## 4. Invitar a los usuarios

1. Comparta el enlace del juego y el enlace del formulario con **5 personas**.
2. Pídales: jugar una partida completa y responder el cuestionario.
3. Recoja las 5 respuestas y descargue el reporte de Google Forms.

## 5. Anexar la evidencia

- Adjunte el **reporte de analítica** (PDF del formulario) al informe de hallazgos
  (`GA5-AA2-EV01_Informe_Hallazgos.docx`).
- Con las conclusiones, aplique los ajustes y capture el **antes/después** con
  **Evidence Capture Tool** para las infografías (`GA5-AA3-EV01` y `GA5-AA4-EV01`).
