// Render every ./docs/mocks/*.html to PNG screenshots at three viewport widths.
// Usage from repo root: `node ./docs/mocks/render.mjs`
// Output: ./docs/mocks/screenshots/<basename>.<width>.png  (basename = filename without .html)

import { chromium } from 'playwright';
import { readdir, mkdir, stat } from 'node:fs/promises';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join, basename, extname, resolve } from 'node:path';

const WIDTHS = [360, 768, 1440];
const HEIGHT = 2200; // tall enough; fullPage:true captures the rest
const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Resolve mocks dir from this script's location so it works no matter where you launch from.
const mocksDir = __dirname;
const screenshotsDir = join(mocksDir, 'screenshots');

async function ensureDir(dir) {
  try { await stat(dir); } catch { await mkdir(dir, { recursive: true }); }
}

async function listHtmlFiles(dir) {
  const entries = await readdir(dir, { withFileTypes: true });
  return entries
    .filter((e) => e.isFile() && extname(e.name).toLowerCase() === '.html')
    .map((e) => join(dir, e.name))
    .sort();
}

async function main() {
  await ensureDir(screenshotsDir);

  const htmlFiles = await listHtmlFiles(mocksDir);
  if (htmlFiles.length === 0) {
    console.error(`No HTML files found in ${mocksDir}`);
    process.exit(1);
  }

  console.log(`Found ${htmlFiles.length} mock(s) in ${mocksDir}`);
  console.log(`Writing PNGs to ${screenshotsDir}`);
  console.log(`Widths: ${WIDTHS.join(', ')}`);
  console.log('---');

  const browser = await chromium.launch();
  let savedCount = 0;
  try {
    for (const width of WIDTHS) {
      const context = await browser.newContext({
        viewport: { width, height: HEIGHT },
        deviceScaleFactor: 1,
      });
      const page = await context.newPage();

      for (const htmlPath of htmlFiles) {
        const base = basename(htmlPath, '.html');
        const outPath = resolve(screenshotsDir, `${base}.${width}.png`);
        const url = pathToFileURL(htmlPath).href;

        await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 });
        // Allow web fonts / icon font / any Material Web component CSS to settle.
        await page.waitForTimeout(400);
        await page.screenshot({ path: outPath, fullPage: true });
        savedCount += 1;
        console.log(`  saved ${outPath}`);
      }

      await context.close();
    }
  } finally {
    await browser.close();
  }

  console.log('---');
  console.log(`Done. Wrote ${savedCount} PNG(s).`);
}

main().catch((err) => {
  console.error('Render failed:', err);
  process.exit(1);
});
