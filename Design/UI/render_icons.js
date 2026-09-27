// icons.html dagi har bir ikonkani 96x96 (24 birlik x 4) shaffof oq PNG qilib chiqaradi.
// Ishlatish: node render_icons.js <chiqish_papkasi>
const { chromium } = require('playwright');
const path = require('path');
(async () => {
  const outDir = path.resolve(process.argv[2]);
  const browser = await chromium.launch();
  const page = await browser.newPage({ deviceScaleFactor: 2 });
  await page.goto('file://' + path.join(__dirname, 'icons.html'));
  const ids = await page.$$eval('svg[id]', els => els.map(e => e.id));
  for (const id of ids) {
    await page.locator('#' + id).screenshot({ path: path.join(outDir, `Icon_${id[0].toUpperCase()}${id.slice(1)}.png`), omitBackground: true });
  }
  await browser.close();
  console.log('ikonkalar:', ids.join(', '));
})();
