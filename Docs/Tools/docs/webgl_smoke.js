// Prueba de humo de la build WebGL: la sirve el llamador (python3 -m http.server) y
// este script la abre en Chromium headless, espera la carga y reporta errores de consola.
// Uso: NODE_PATH=<ruta a node_modules con playwright-core> node webgl_smoke.js http://localhost:8091 salida.png
const { chromium } = require("playwright-core");

(async () => {
  const url = process.argv[2] || "http://localhost:8091";
  const out = process.argv[3] || "webgl_smoke.png";
  const browser = await chromium.launch({
    args: ["--use-angle=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist", "--enable-webgl"],
  });
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => { if (m.type() === "error") errors.push("console.error: " + m.text()); });
  await page.goto(url, { waitUntil: "load" });
  await page.waitForTimeout(45000);
  const hasCanvas = await page.evaluate(() => !!document.querySelector("canvas"));
  await page.screenshot({ path: out });
  console.log(JSON.stringify({ hasCanvas, errorCount: errors.length, errors: errors.slice(0, 8) }, null, 2));
  await browser.close();
})();
