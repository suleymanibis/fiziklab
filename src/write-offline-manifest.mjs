import {createHash} from 'node:crypto';
import {readFile, readdir, writeFile} from 'node:fs/promises';
import {dirname, relative, resolve, sep} from 'node:path';
import {fileURLToPath} from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), 'dist');
const manifest = 'service-worker-assets.js';
async function collect(dir) {
  const assets = [];
  for (const entry of await readdir(dir, {withFileTypes:true})) {
    const path = resolve(dir, entry.name);
    if (entry.isDirectory()) assets.push(...await collect(path));
    else if (entry.name !== manifest && entry.name !== 'service-worker.js') {
      const hash = createHash('sha256').update(await readFile(path)).digest('base64');
      assets.push({url:relative(root, path).split(sep).join('/'), hash:'sha256-' + hash});
    }
  }
  return assets;
}
const assets = (await collect(root)).sort((a,b) => a.url.localeCompare(b.url, 'en'));
const version = createHash('sha256').update(JSON.stringify(assets)).digest('hex').slice(0, 20);
await writeFile(resolve(root, manifest), 'self.assetsManifest = ' + JSON.stringify({version, assets}) + ';\n');
