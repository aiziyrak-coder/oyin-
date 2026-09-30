// No network access: a complete, lockfile-matching installation is kept usable offline.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { join, resolve, sep } from 'node:path';
import { createRequire } from 'node:module';

const root = fileURLToPath(new URL('../../Server/', import.meta.url));
const require = createRequire(join(root, 'package.json'));
try {
  const lock = JSON.parse(readFileSync(join(root, 'package-lock.json'), 'utf8'));
  const pkg = JSON.parse(readFileSync(join(root, 'package.json'), 'utf8'));
  for (const [name, version] of Object.entries(pkg.dependencies ?? {})) {
    if (lock.packages[''].dependencies?.[name] !== version) throw new Error('package-lock is out of date');
    require.resolve(name);
  }
  for (const [path, item] of Object.entries(lock.packages)) {
    if (!path) continue;
    const target = resolve(root, path);
    if (!target.startsWith(resolve(root, 'node_modules') + sep)) throw new Error('unexpected dependency path');
    const installed = JSON.parse(readFileSync(join(target, 'package.json'), 'utf8'));
    if (installed.version !== item.version) throw new Error('dependency version mismatch: ' + path);
  }
} catch (error) {
  if (process.argv.includes('--verbose')) console.error('Server dependencies need npm ci --ignore-scripts: ' + error.message);
  process.exitCode = 1;
}
