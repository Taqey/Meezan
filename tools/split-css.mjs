// Splits a CSS file into contiguous chunks (cascade order is preserved when re-imported in order).
// usage: node tools/split-css.mjs <file.css> <outDir> <importPrefix> <skipFirstLine?0|1> start:name [start:name ...]
import fs from 'node:fs';
import path from 'node:path';
const [file, outDir, prefix, skip, ...specs] = process.argv.slice(2);
const raw = fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '');
const eol = raw.includes('\r\n') ? '\r\n' : '\n';
const lines = raw.split(/\r?\n/);
// depth per line start (ignoring comments and strings)
const depthAt = [];
let depth = 0, inC = false, q = null;
for (const ln of lines) {
  depthAt.push(inC ? -1 : depth);
  for (let i = 0; i < ln.length; i++) {
    const c = ln[i], n = ln[i + 1];
    if (inC) { if (c === '*' && n === '/') { inC = false; i++; } continue; }
    if (q) { if (c === '\\') i++; else if (c === q) q = null; continue; }
    if (c === '/' && n === '*') { inC = true; i++; continue; }
    if (c === '"' || c === "'") { q = c; continue; }
    if (c === '{') depth++; else if (c === '}') depth--;
  }
}
const marks = specs.map(s => { const [l, n] = s.split(':'); return { line: +l, name: n }; });
for (const m of marks) if (depthAt[m.line - 1] !== 0) throw new Error(`line ${m.line} (${m.name}) is not at top level (depth ${depthAt[m.line - 1]})`);
fs.mkdirSync(outDir, { recursive: true });
const imports = [];
const start0 = +skip ? 1 : 0; // lines before first chunk (the @import header) are kept in the entry file
const head = lines.slice(0, start0);
let out = [];
marks.forEach((m, i) => {
  const from = m.line - 1;
  const to = i + 1 < marks.length ? marks[i + 1].line - 1 : lines.length;
  const body = lines.slice(from, to).join(eol);
  const name = String(i + 1).padStart(2, '0') + '-' + m.name + '.css';
  fs.writeFileSync(path.join(outDir, name), body.replace(/(\r?\n)*$/, '') + eol, 'utf8');
  imports.push(`@import '${prefix}/${name}';`);
});
if (marks[0].line - 1 > start0) throw new Error('uncovered lines before first mark');
console.log(imports.join('\n'));
fs.writeFileSync(file + '.imports.txt', head.concat(imports).join(eol) + eol, 'utf8');

