// Rewrites imports of the ui barrel (".../components/ui" or "../ui") into direct per-file imports (avoids circular barrel imports).
import fs from 'node:fs';
import path from 'node:path';
const uiDir = path.resolve('frontend/src/app/components/ui');
const root = path.resolve('frontend/src');
const sym = new Map();
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (p.endsWith('.ts') && !p.endsWith('.spec.ts') && e.name !== 'index.ts') {
      const t = fs.readFileSync(p, 'utf8');
      for (const m of t.matchAll(/^export (?:abstract )?(?:class|interface|type|const|function) (\w+)/gm)) sym.set(m[1], p);
    }
  }
})(uiDir);
let changed = 0;
(function walk(d) {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) { if (e.name !== 'node_modules') walk(p); continue; }
    if (!p.endsWith('.ts')) continue;
    if (p.startsWith(uiDir + path.sep)) continue;
    let t = fs.readFileSync(p, 'utf8');
    const re = /import\s*\{([^}]*)\}\s*from\s*(['"])((?:\.\.?\/)+(?:components\/)?ui)\2;?/g;
    const out = t.replace(re, (m, names, q, spec) => {
      if (path.resolve(path.dirname(p), spec) !== uiDir) return m;
      const byFile = new Map();
      for (const raw of names.split(',').map(s => s.trim()).filter(Boolean)) {
        const nm = raw.replace(/^type\s+/, '').split(/\s+as\s+/)[0];
        const f = sym.get(nm);
        if (!f) throw new Error('unknown symbol ' + nm + ' in ' + p);
        if (!byFile.has(f)) byFile.set(f, []);
        byFile.get(f).push(raw);
      }
      return [...byFile].map(([f, list]) => {
        let rel = path.relative(path.dirname(p), f.replace(/\.ts$/, '')).split(path.sep).join('/');
        if (!rel.startsWith('.')) rel = './' + rel;
        return `import { ${list.join(', ')} } from '${rel}';`;
      }).join('\n');
    });
    if (out !== t) { fs.writeFileSync(p, out, 'utf8'); changed++; console.log(path.relative(root, p)); }
  }
})(root);
console.log('changed', changed);

