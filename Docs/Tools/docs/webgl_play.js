// Juega la build WebGL: menú -> selección -> carrera, con capturas y errores de consola.
// Uso: NODE_PATH=<node_modules> node webgl_play.js http://localhost:8080 carpeta_salida
const { chromium } = require("playwright-core");
(async () => {
  const url = process.argv[2] || "http://localhost:8080";
  const out = process.argv[3] || ".";
  const browser = await chromium.launch({ args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "--enable-webgl"] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => { if (m.type() === "error" || m.type() === "warning") errors.push(m.type() + ": " + m.text().slice(0, 300)); });
  await page.goto(url);
  await page.waitForSelector("canvas", { timeout: 60000 });
  await page.waitForTimeout(25000);
  const box = await (await page.$("canvas")).boundingBox();
  const at = async (fx, fy) => { await page.mouse.click(box.x + box.width * fx, box.y + box.height * fy); };
  const shot = async (n) => page.screenshot({ path: `${out}/${n}.png` });
  await shot("1_menu");
  await at(0.18, 0.51);               // JUGAR
  await page.waitForTimeout(8000);
  await shot("2_seleccion");
  await at(0.5, 0.94);                // INICIAR CARRERA
  for (let k = 0; k < 6; k++) { await page.waitForTimeout(900); await shot(`2b_intro_${k}`); }   // tarjeta de introducción del nivel
  for (const [i, t] of [[3, 9000], [4, 8000], [5, 8000]]) {
    await page.waitForTimeout(t);
    if (i === 4) await page.keyboard.down("w");
    await shot(`${i}_carrera`);
  }
  console.log(JSON.stringify({ errorCount: errors.length, errors: [...new Set(errors.filter(e => !e.includes("Mismatch")).map(e => e.slice(0,1500)))].slice(0, 30) }, null, 1));
  await browser.close();
})();
