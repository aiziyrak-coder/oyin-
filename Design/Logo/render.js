// compose.html dagi har bir qatlamni shaffof fonda 2x o'lchamda PNG qilib chiqaradi.
// Ishlatish: node render.js <chiqish_papkasi>
const { chromium } = require('playwright');
const path = require('path');
(async () => {
  const outDir = path.resolve(process.argv[2] || path.join(__dirname, '.out'));
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1920, height: 1080 }, deviceScaleFactor: 2 });
  await page.goto('file://' + path.join(__dirname, 'compose.html'));
  await page.evaluate(() => document.fonts.ready);
  await page.screenshot({ path: path.join(outDir, '_preview_2x.png') });
  await page.evaluate(() => document.body.classList.add('transparent'));
  for (const id of ['emblem', 'wordglow', 'wordmark', 'divider', 'tagline']) {
    await page.evaluate((id) => {
      document.querySelectorAll('.layer').forEach(e => { e.style.visibility = e.id === id ? 'visible' : 'hidden'; });
    }, id);
    await page.screenshot({ path: path.join(outDir, `_full_${id}.png`), omitBackground: true });
  }
  await browser.close();
})();
