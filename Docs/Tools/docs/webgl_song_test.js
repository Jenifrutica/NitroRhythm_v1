// Prueba de punta a punta de "TU CANCIÓN" en la build WebGL:
// menú → selección → TU CANCIÓN → ELEGIR ARCHIVO (selector del navegador) → análisis → LISTO →
// INICIAR CARRERA, con capturas y el registro de consola de Unity ([SongLoader], [AudioReactive]).
//
// Uso (servir antes la build, p. ej. en el puerto 8091):
//   NODE_PATH=~/SENA/NitroRythm/Tools/node/node_modules \
//   node webgl_song_test.js http://localhost:8091 /ruta/salida /ruta/cancion.mp3 [ruta/chrome]
const { chromium } = require("playwright-core");

(async () => {
  const url = process.argv[2] || "http://localhost:8091";
  const out = process.argv[3] || ".";
  const song = process.argv[4];
  const exe = process.argv[5] || undefined;
  if (!song) { console.error("Falta la ruta de la canción."); process.exit(2); }

  const browser = await chromium.launch({
    executablePath: exe,
    args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "--enable-webgl", "--autoplay-policy=no-user-gesture-required"],
  });
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  const log = [];
  page.on("pageerror", (e) => log.push("pageerror: " + e.message));
  page.on("console", (m) => log.push(m.type() + ": " + m.text().slice(0, 400)));

  await page.goto(url);
  await page.waitForSelector("canvas", { timeout: 60000 });
  await page.waitForTimeout(25000);
  const box = await (await page.$("canvas")).boundingBox();
  // Canvas de referencia 1920×1080: (x, y) en unidades del canvas desde el centro → clic.
  const at = async (cx, cy) => page.mouse.click(box.x + box.width * (0.5 + cx / 1920), box.y + box.height * (0.5 - cy / 1080));
  const shot = (n) => page.screenshot({ path: `${out}/${n}.png` });

  await shot("s1_menu");
  await page.mouse.click(box.x + box.width * 0.18, box.y + box.height * 0.51);   // JUGAR
  await page.waitForTimeout(8000);
  await shot("s2_seleccion");

  await at(-418, -475);                       // TU CANCIÓN
  await page.waitForTimeout(1500);
  await shot("s3_panel_cancion");

  const chooser = page.waitForEvent("filechooser", { timeout: 15000 });
  await at(-250, -30);                        // ELEGIR ARCHIVO
  const fc = await chooser;
  await fc.setFiles(song);
  let ready = false;
  for (let i = 0; i < 60 && !ready; i++) {
    await page.waitForTimeout(1000);
    ready = log.some((l) => l.includes("[SongLoader]"));
  }
  await page.waitForTimeout(1500);
  await shot("s4_cancion_lista");

  await at(0, -260);                          // LISTO
  await page.waitForTimeout(1200);
  await shot("s5_seleccion_con_cancion");

  await at(0, -475);                          // INICIAR CARRERA
  for (let k = 0; k < 4; k++) { await page.waitForTimeout(1200); await shot(`s6_intro_${k}`); }
  await page.keyboard.press("Space");
  await page.waitForTimeout(3000);
  await page.keyboard.down("KeyW");
  for (const [i, t] of [[7, 4000], [8, 5000], [9, 5000]]) {
    await page.waitForTimeout(t);
    await shot(`s${i}_carrera`);
  }
  await page.keyboard.up("KeyW");

  const interesting = log.filter((l) => /SongLoader|AudioReactive|error|exception|NitroSong/i.test(l));
  require("fs").writeFileSync(`${out}/consola.txt`, log.join("\n"));
  console.log("Cancion analizada:", ready);
  console.log(interesting.slice(0, 40).join("\n"));
  await browser.close();
})();
