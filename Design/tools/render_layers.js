// HTML sahifadagi har bir qatlamni (id bo'yicha) shaffof fonda 2x o'lchamda PNG qilib chiqaradi.
// Ishlatish: node render_layers.js <sahifa.html> <chiqish_papkasi> <id1> <id2> ...
// Sahifada qatlamlar "layer" klassiga ega bo'lishi kerak. data-preview="off" belgili
// qatlamlar umumiy ko'rinishga (_preview_2x.png) kirmaydi.
const { chromium } = require('playwright');
const path = require('path');
(async () => {
  const [html, outArg, ...ids] = process.argv.slice(2);
  const outDir = path.resolve(outArg);
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 2 });
  await page.goto('file://' + path.resolve(html));
  await page.evaluate(() => document.fonts.ready);
  // Sahifa o'z joylashuvini hisoblab bo'lishini kutamiz (window.layersReady = false qilib qo'ygan bo'lsa)
  await page.waitForFunction(() => window.layersReady !== false);
  await page.evaluate(() => document.querySelectorAll('[data-preview="off"]').forEach(e => { e.style.visibility = 'hidden'; }));
  await page.screenshot({ path: path.join(outDir, '_preview_2x.png') });
  await page.evaluate(() => document.body.classList.add('transparent'));
  for (const id of ids) {
    await page.evaluate((id) => {
      document.querySelectorAll('.layer').forEach(e => { e.style.visibility = e.id === id ? 'visible' : 'hidden'; });
    }, id);
    await page.screenshot({ path: path.join(outDir, `_full_${id}.png`), omitBackground: true });
  }
  await browser.close();
})();
