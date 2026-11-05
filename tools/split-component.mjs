// Splits inline template/styles of Angular components into .html/.css and creates a basic spec.
// usage: node tools/split-component.mjs <component.ts> [...]
import fs from 'node:fs';
import path from 'node:path';

function scanLiteral(src, start) {
  // start = index of opening backtick; returns index of closing backtick
  for (let i = start + 1; i < src.length; i++) {
    if (src[i] === '\\') { i++; continue; }
    if (src[i] === '$' && src[i + 1] === '{') throw new Error('interpolation in literal');
    if (src[i] === '`') return i;
  }
  throw new Error('unterminated literal');
}

for (const file of process.argv.slice(2)) {
  let src = fs.readFileSync(file, 'utf8');
  const bom = src.charCodeAt(0) === 0xFEFF;
  if (bom) src = src.slice(1);
  const dir = path.dirname(file);
  const base = path.basename(file, '.ts');
  const eol = src.includes('\r\n') ? '\r\n' : '\n';
  if (!/@Component\(/.test(src)) { console.log('SKIP (no component)', file); continue; }
  try {
    const tm = /(\n[ \t]*)template:\s*`/.exec(src);
    if (tm) {
      const open = tm.index + tm[0].length - 1;
      const close = scanLiteral(src, open);
      const content = Function('return `' + src.slice(open + 1, close) + '`')();
      let end = close + 1;
      if (src[end] === ',') end++;
      fs.writeFileSync(path.join(dir, base + '.html'), content, 'utf8');
      src = src.slice(0, tm.index) + tm[1] + `templateUrl: './${base}.html',` + src.slice(end);
    }
    const sm = /(\n[ \t]*)styles:\s*(\[\s*)?`/.exec(src);
    if (sm) {
      const open = sm.index + sm[0].length - 1;
      const close = scanLiteral(src, open);
      const content = Function('return `' + src.slice(open + 1, close) + '`')();
      let end = close + 1;
      if (sm[2]) {
        const rest = /^\s*\]/.exec(src.slice(end));
        if (!rest) throw new Error('multiple styles entries');
        end += rest[0].length;
      }
      if (src[end] === ',') end++;
      fs.writeFileSync(path.join(dir, base + '.css'), content, 'utf8');
      src = src.slice(0, sm.index) + sm[1] + `styleUrl: './${base}.css',` + src.slice(end);
    }
    fs.writeFileSync(file, (bom ? '\uFEFF' : '') + src, 'utf8');
    const specPath = path.join(dir, base + '.spec.ts');
    if (!fs.existsSync(specPath)) {
      const cls = /export class (\w+)/.exec(src)?.[1];
      if (cls) {
        const spec = [
          `import { ComponentFixture, TestBed } from '@angular/core/testing';`,
          `import { provideRouter } from '@angular/router';`,
          `import { provideHttpClient } from '@angular/common/http';`,
          `import { provideHttpClientTesting } from '@angular/common/http/testing';`,
          `import { ${cls} } from './${base}';`,
          ``,
          `describe('${cls}', () => {`,
          `  let fixture: ComponentFixture<${cls}>;`,
          ``,
          `  beforeEach(async () => {`,
          `    await TestBed.configureTestingModule({`,
          `      imports: [${cls}],`,
          `      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]`,
          `    }).compileComponents();`,
          `    fixture = TestBed.createComponent(${cls});`,
          `  });`,
          ``,
          `  it('should create', () => {`,
          `    expect(fixture.componentInstance).toBeTruthy();`,
          `  });`,
          `});`,
          ``
        ].join(eol);
        fs.writeFileSync(specPath, spec, 'utf8');
      }
    }
    console.log('OK  ', file);
  } catch (e) {
    console.log('FAIL', file, e.message);
  }
}

