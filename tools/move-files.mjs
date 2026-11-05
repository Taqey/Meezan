// Moves files according to rules and rewrites every relative import/ import() specifier.
import fs from 'node:fs';
import path from 'node:path';
const app = path.resolve('frontend/src/app');
const rules = [
  ['services/api.service.ts', 'core/api/api.service.ts'],
  ['services/admin-auth.', 'core/auth/admin-auth.'],
  ['guards/', 'core/auth/'],
  ['services/session-cache', 'core/services/session-cache'],
  ['services/toast', 'core/services/toast'],
  ['services/favorites', 'core/services/favorites'],
  ['services/portfolio-simplifier', 'features/portfolio/services/portfolio-simplifier'],
  ['components/', 'shared/components/'],
  ['directives/', 'shared/directives/'],
  ['models/', 'shared/models/'],
  ['utils/', 'shared/utils/'],
  ['layouts/', 'core/layouts/'],
  ['admin/admin.routes.ts', 'features/admin/admin.routes.ts'],
  ['pages/stocks-list/', 'features/stocks/stocks-list/'],
  ['pages/stock-detail/', 'features/stocks/stock-detail/'],
  ['pages/sectors-list/', 'features/sectors/sectors-list/'],
  ['pages/sector-detail/', 'features/sectors/sector-detail/'],
  ['pages/indices-list/', 'features/indices/indices-list/'],
  ['pages/index-detail/', 'features/indices/index-detail/'],
  ['pages/funds-list/', 'features/funds/funds-list/'],
  ['pages/fund-detail/', 'features/funds/fund-detail/'],
  ['pages/fund-manager-detail/', 'features/funds/fund-manager-detail/'],
  ['pages/portfolio-simplifier/', 'features/portfolio/portfolio-simplifier/'],
  ['pages/portfolio/', 'features/portfolio/portfolio/'],
  ['pages/shariah-standards/', 'features/sharia/shariah-standards/'],
  ['pages/purification/', 'features/sharia/purification/'],
  ['pages/favorites/', 'features/favorites/favorites/'],
  ['pages/home/', 'features/home/home/'],
  ['pages/admin-classification/', 'features/admin/admin-classification/'],
  ['pages/admin-funds/', 'features/admin/admin-funds/'],
  ['pages/admin-login/', 'features/admin/admin-login/'],
  ['pages/admin/', 'features/admin/admin/'],
];
const all = [];
(function walk(d) { for (const e of fs.readdirSync(d, { withFileTypes: true })) { const p = path.join(d, e.name); e.isDirectory() ? walk(p) : all.push(p); } })(app);
const moveMap = new Map();
for (const f of all) {
  const rel = path.relative(app, f).split(path.sep).join('/');
  const r = rules.find(([from]) => rel.startsWith(from));
  moveMap.set(f, r ? path.join(app, r[1] + rel.slice(r[0].length)) : f);
}
const exists = new Set(all);
const re = /(from\s*|import\s*\(\s*|import\s+)(['"])(\.{1,2}\/[^'"]*|\.{1,2})\2/g;
const posix = p => p.split(path.sep).join('/');
function rewrite(text, oldFile, newFile) {
  return text.replace(re, (m, pre, q, spec) => {
    const abs = path.resolve(path.dirname(oldFile), spec);
    let cand = null, kind;
    if (exists.has(abs + '.ts')) { cand = abs + '.ts'; kind = 'ts'; }
    else if (exists.has(path.join(abs, 'index.ts'))) { cand = path.join(abs, 'index.ts'); kind = 'index'; }
    else if (exists.has(abs)) { cand = abs; kind = 'file'; }
    if (!cand) { if (abs.startsWith(app)) console.log('UNRESOLVED', oldFile, spec); return m; }
    const nt = moveMap.get(cand);
    let target = kind === 'ts' ? nt.slice(0, -3) : kind === 'index' ? path.dirname(nt) : nt;
    let rel = posix(path.relative(path.dirname(newFile), target));
    if (!rel.startsWith('.')) rel = './' + rel;
    return `${pre}${q}${rel}${q}`;
  });
}
const outputs = [];
for (const f of all) {
  let t = f.endsWith('.ts') ? fs.readFileSync(f, 'utf8') : null;
  if (t !== null) t = rewrite(t, f, moveMap.get(f));
  outputs.push([f, moveMap.get(f), t]);
}
for (const [oldF, newF, t] of outputs) {
  if (t !== null) fs.writeFileSync(oldF, t, 'utf8');
}
let moved = 0;
for (const [oldF, newF] of outputs) {
  if (oldF === newF) continue;
  fs.mkdirSync(path.dirname(newF), { recursive: true });
  fs.renameSync(oldF, newF); moved++;
}
// remove empty dirs
(function prune(d) { for (const e of fs.readdirSync(d, { withFileTypes: true })) if (e.isDirectory()) { prune(path.join(d, e.name)); } if (d !== app && fs.readdirSync(d).length === 0) fs.rmdirSync(d); })(app);
console.log('moved', moved);

