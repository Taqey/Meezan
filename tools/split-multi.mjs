// Splits files that hold several declarations (components, models, constants, functions) into one declaration per file.
// usage: node tools/split-multi.mjs <dir> <file.ts> [...]   (new files are created under <dir>)
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
const require = createRequire(path.resolve('frontend/package.json'));
const ts = require('typescript');

const dir = path.resolve(process.argv[2]);
const files = process.argv.slice(3).map(f => path.resolve(f));

const kebab = s => s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2').toLowerCase();
const posix = p => p.split(path.sep).join('/');

function scanLiteral(src, start) {
  for (let i = start + 1; i < src.length; i++) {
    if (src[i] === '\\') { i++; continue; }
    if (src[i] === '$' && src[i + 1] === '{') throw new Error('interpolation in literal');
    if (src[i] === '`') return i;
  }
  throw new Error('unterminated');
}
function extract(text, key, ext, outBase, urlKey) {
  const re = key === 'template' ? /(\n[ \t]*)template:\s*`/ : /(\n[ \t]*)styles:\s*(\[\s*)?`/;
  const m = re.exec(text);
  if (!m) return text;
  const open = m.index + m[0].length - 1;
  const close = scanLiteral(text, open);
  const content = Function('return `' + text.slice(open + 1, close) + '`')();
  let end = close + 1;
  if (key === 'styles' && m[2]) { const r = /^\s*\]/.exec(text.slice(end)); if (!r) throw new Error('multi styles'); end += r[0].length; }
  if (text[end] === ',') end++;
  fs.writeFileSync(outBase + ext, content, 'utf8');
  const bn = path.basename(outBase);
  return text.slice(0, m.index) + m[1] + `${urlKey}: './${bn}${ext}',` + text.slice(end);
}

// ---- pass 1: parse
const parsed = [];
const privates = [];
const symbolFile = new Map(); // name -> new absolute file
for (const file of files) {
  const text = fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '');
  const sf = ts.createSourceFile(file, text, ts.ScriptTarget.Latest, true);
  const imports = []; // {name, module(abs or pkg), type}
  const decls = [];
  for (const st of sf.statements) {
    if (ts.isImportDeclaration(st)) {
      const spec = st.moduleSpecifier.text;
      const mod = spec.startsWith('.') ? path.resolve(path.dirname(file), spec) : spec;
      const nb = st.importClause?.namedBindings;
      if (nb && ts.isNamedImports(nb)) for (const el of nb.elements) imports.push({ name: el.name.text, orig: el.propertyName?.text, module: mod, type: el.isTypeOnly || st.importClause.isTypeOnly });
      else if (st.importClause?.name) throw new Error('default import in ' + file);
      continue;
    }
    let name, kind, exported = !!ts.getModifiers?.(st)?.some(m => m.kind === ts.SyntaxKind.ExportKeyword);
    if (ts.isClassDeclaration(st)) { name = st.name.text; kind = 'component'; }
    else if (ts.isInterfaceDeclaration(st) || ts.isTypeAliasDeclaration(st)) { name = st.name.text; kind = 'model'; }
    else if (ts.isFunctionDeclaration(st)) { name = st.name.text; kind = 'util'; }
    else if (ts.isVariableStatement(st)) { name = st.declarationList.declarations[0].name.text; kind = 'constant'; if (st.declarationList.declarations.length > 1) throw new Error('multi var ' + name); }
    else throw new Error('unsupported statement in ' + file + ': ' + ts.SyntaxKind[st.kind]);
    if (!exported) {
      if (kind !== 'constant') throw new Error('non-exported declaration ' + name + ' in ' + file);
      privates.push({ name, text: text.slice(st.getStart(), st.end) });
      continue;
    }
    const comments = (ts.getLeadingCommentRanges(text, st.pos) || []).map(r => text.slice(r.pos, r.end)).filter(c => !c.includes('â”€'));
    let body = text.slice(st.getStart(), st.end);
    let outFile;
    if (kind === 'component') {
      const bn = kebab(name.replace(/Component$/, ''));
      outFile = path.join(dir, bn, bn + '.component.ts');
    } else if (kind === 'model') outFile = path.join(dir, 'models', kebab(name) + '.model.ts');
    else if (kind === 'constant') outFile = path.join(dir, 'constants', kebab(name) + '.constants.ts');
    else outFile = path.join(dir, 'mappers', kebab(name) + '.util.ts');
    decls.push({ name, kind, comments, body, outFile });
    symbolFile.set(name, outFile);
  }
  parsed.push({ file, imports, decls });
}

// ---- pass 2: write
const eol = '\r\n';
const barrel = [];
for (const { imports, decls } of parsed) {
  for (const d of decls) {
    const base = d.outFile.replace(/\.ts$/, '');
    fs.mkdirSync(path.dirname(d.outFile), { recursive: true });
    let body = d.body;
    for (const pv of privates) if (new Set(body.match(/[A-Za-z_$][\w$]*/g)).has(pv.name)) body = pv.text + eol + eol + body;
    if (d.kind === 'component') {
      body = extract(body, 'template', '.html', base, 'templateUrl');
      body = extract(body, 'styles', '.css', base, 'styleUrl');
    }
    const used = new Set(body.match(/[A-Za-z_$][\w$]*/g));
    const byModule = new Map();
    const order = [];
    const add = (mod, name, orig, type) => {
      if (!byModule.has(mod)) { byModule.set(mod, []); order.push(mod); }
      const list = byModule.get(mod);
      if (!list.some(x => x.name === name)) list.push({ name, orig, type });
    };
    for (const im of imports) {
      if (!used.has(im.name)) continue;
      if (typeof im.module === 'string' && !im.module.startsWith(path.sep) && !/^[A-Za-z]:/.test(im.module)) add(im.module, im.name, im.orig, im.type);
      else {
        const tgt = symbolFile.get(im.orig ?? im.name);
        if (tgt) add(tgt, im.name, im.orig, im.type);
        else add(im.module, im.name, im.orig, im.type); // outside ui folder
      }
    }
    for (const [name, f] of symbolFile) if (f !== d.outFile && used.has(name) && !imports.some(i => i.name === name) ) {
      const kind = parsed.flatMap(p => p.decls).find(x => x.name === name).kind;
      add(f, name, undefined, kind === 'model');
    }
    const lines = [];
    for (const mod of order) {
      let spec;
      if (/^[A-Za-z]:/.test(mod) || mod.startsWith(path.sep) || mod.startsWith(dir) || path.isAbsolute(mod)) {
        let rel = posix(path.relative(path.dirname(d.outFile), mod.replace(/\.ts$/, '')));
        if (!rel.startsWith('.')) rel = './' + rel;
        spec = rel;
      } else spec = mod;
      const names = byModule.get(mod).map(x => (x.type ? 'type ' : '') + (x.orig ? `${x.orig} as ${x.name}` : x.name));
      lines.push(`import { ${names.join(', ')} } from '${spec}';`);
    }
    const header = lines.length ? lines.join(eol) + eol + eol : '';
    const cm = d.comments.length ? d.comments.join(eol) + eol : '';
    fs.writeFileSync(d.outFile, header + cm + body + eol, 'utf8');
    if (d.kind === 'component') {
      const specPath = base + '.spec.ts';
      const cls = d.name;
      const bn = path.basename(base);
      if (!fs.existsSync(specPath)) fs.writeFileSync(specPath, [
        `import { ComponentFixture, TestBed } from '@angular/core/testing';`,
        `import { provideRouter } from '@angular/router';`,
        `import { provideHttpClient } from '@angular/common/http';`,
        `import { provideHttpClientTesting } from '@angular/common/http/testing';`,
        `import { ${cls} } from './${bn}';`, ``,
        `describe('${cls}', () => {`,
        `  let fixture: ComponentFixture<${cls}>;`, ``,
        `  beforeEach(async () => {`,
        `    await TestBed.configureTestingModule({`,
        `      imports: [${cls}],`,
        `      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]`,
        `    }).compileComponents();`,
        `    fixture = TestBed.createComponent(${cls});`,
        `  });`, ``,
        `  it('should create', () => {`,
        `    expect(fixture.componentInstance).toBeTruthy();`,
        `  });`,
        `});`, ``].join(eol), 'utf8');
    }
    let rel = posix(path.relative(dir, base));
    barrel.push(`export * from './${rel}';`);
  }
}
fs.writeFileSync(path.join(dir, 'index.ts'), barrel.join(eol) + eol, 'utf8');
for (const f of files) fs.unlinkSync(f);
console.log('done', barrel.length, 'files');


