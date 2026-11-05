// Accurate Split script for admin-classification.component (Step 1.2)
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
const require = createRequire(path.resolve('frontend/package.json'));
const ts = require('typescript');

const ROOT = path.resolve('frontend/src/app/features/admin');
const ORIG_DIR = 'frontend/src/app/features/admin/admin-classification';
const tsSrc = fs.readFileSync(path.join('tools/admin-class-orig', 'admin-classification.component.ts'), 'utf8');
const htmlLines = fs.readFileSync(path.join('tools/admin-class-orig', 'admin-classification.component.html'), 'utf8').split(/\r?\n/);
const EOL = '\r\n';
const kebab = s => s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();

function write(rel, content) {
  const p = path.join(ROOT, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content, 'utf8');
}

function extractInlineStyles(rawHtml, classPrefix = 'ac') {
  const styles = new Map();
  let counter = 0;
  let html = rawHtml.replace(/\sstyle="([^"]+)"/g, (full, cssText) => {
    const norm = cssText.trim().replace(/\s+/g, ' ');
    if (!styles.has(norm)) {
      counter++;
      styles.set(norm, `${classPrefix}${counter}`);
    }
    const clsName = styles.get(norm);
    return ` __INLINE_CLS__="${clsName}"`;
  });
  html = html.replace(/<([a-zA-Z0-9-]+)([^>]*?)__INLINE_CLS__="([^"]+)"([^>]*?)>/g, (m, tag, before, inlCls, after) => {
    const combined = before + after;
    if (/\bclass="([^"]*)"/.test(combined)) {
      return `<${tag}${combined.replace(/\bclass="([^"]*)"/, (_, c) => `class="${c} ${inlCls}"`)}>`;
    }
    return `<${tag}${before}class="${inlCls}"${after}>`;
  });
  const cssLines = [];
  for (const [cssText, clsName] of styles) {
    cssLines.push(`.${clsName}.${clsName} { ${cssText} }`);
  }
  return { html, css: cssLines.join(EOL) + (cssLines.length ? EOL : '') };
}

function makeSpec(clsName, fileBase) {
  return [
    `import { ComponentFixture, TestBed } from '@angular/core/testing';`,
    `import { provideRouter } from '@angular/router';`,
    `import { provideHttpClient } from '@angular/common/http';`,
    `import { provideHttpClientTesting } from '@angular/common/http/testing';`,
    `import { ${clsName} } from './${fileBase}';`,
    ``,
    `describe('${clsName}', () => {`,
    `  let component: ${clsName};`,
    `  let fixture: ComponentFixture<${clsName}>;`,
    ``,
    `  beforeEach(async () => {`,
    `    await TestBed.configureTestingModule({`,
    `      imports: [${clsName}],`,
    `      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]`,
    `    }).compileComponents();`,
    `    fixture = TestBed.createComponent(${clsName});`,
    `    component = fixture.componentInstance;`,
    `  });`,
    ``,
    `  it('should create', () => {`,
    `    expect(component).toBeTruthy();`,
    `  });`,
    `});`,
    ``
  ].join(EOL);
}

// Parse original class
const sf = ts.createSourceFile('admin-classification.ts', tsSrc, ts.ScriptTarget.Latest, true);
const imports = [];
let cls;
for (const st of sf.statements) {
  if (ts.isImportDeclaration(st)) {
    const nb = st.importClause?.namedBindings;
    if (nb && ts.isNamedImports(nb)) for (const el of nb.elements)
      imports.push({ name: el.name.text, orig: el.propertyName?.text, module: st.moduleSpecifier.text, type: el.isTypeOnly });
  }
  if (ts.isClassDeclaration(st) && st.name?.text === 'AdminClassificationComponent') cls = st;
}
const members = new Map();
const memberOrder = [];
for (const m of cls.members) {
  if (ts.isConstructorDeclaration(m)) continue;
  const name = m.name?.getText(sf);
  if (!name) continue;
  const cr = ts.getLeadingCommentRanges(tsSrc, m.pos) || [];
  const comments = cr.map(r => tsSrc.slice(r.pos, r.end)).filter(c => !c.includes('──') && !c.startsWith('// ──'));
  let text = tsSrc.slice(m.getStart(sf), m.end);
  const entry = { text, comments };
  if (!members.has(name)) { members.set(name, []); memberOrder.push(name); }
  members.get(name).push(entry);
}

// Services
const S = {
  review: {
    cls: 'AdminClassificationReviewService',
    members: 'review reviewLoading reviewBadge checkedReview trackReview loadReview toggleReviewCheck allReviewChecked toggleAllReview'
  },
  tree: {
    cls: 'AdminClassificationTreeService',
    members: 'tree flatTree childIds treeLoading showDeleted expanded publicTree autoEnabled seedBusy reclassBusy lastReport editorOpen editingNode editorParent editorParentHasFunds editorParentFundCount editorFundDisposition editorForm editorKeywords nodeBusy moveNode moveParentId moveTargets mergeSource mergeTargetId deletePreview deleteFundsChoice confirmBox trackNode trackIndex refreshAll loadTree toggleAuto runReclassify runSeedMigrate rebuildFlatTree hasChildren toggleExpand indentLabel openEditor keywordsFromRule saveNode openMove confirmMove openMerge confirmMerge openDelete confirmDelete restoreNode runConfirm'
  },
  funds: {
    cls: 'AdminClassificationFundsService',
    members: 'fundsFor fundsItems fundsTotal fundsPage fundsTotalPages fundsSearch fundsSource checkedFunds addSearch addResults checkedAdd bulkMoveOpen bulkMovePath bulkMoveCount trackFund sourceAr openFunds closeFunds loadNodeFunds toggleFundCheck resetFund assignFromCascade searchFundsToAdd toggleAddCheck bulkAdd confirmBulkMove bulkReset'
  }
};

members.set('closeFunds', [{ text: `closeFunds(): void { this.fundsFor = null; this.fundsItems = []; }`, comments: [] }]);
if (!memberOrder.includes('closeFunds')) memberOrder.push('closeFunds');

const memberOwner = new Map();
for (const [alias, def] of Object.entries(S)) {
  def.alias = alias; def.names = def.members.split(/\s+/);
  def.file = `services/${kebab(def.cls.replace(/Service$/, ''))}.service.ts`;
  for (const n of def.names) {
    if (!members.has(n)) throw new Error('missing member ' + n);
    memberOwner.set(n, alias);
  }
}

function stripStringsAndComments(code) {
  return code
    .replace(/\/\*[\s\S]*?\*\//g, ' ')
    .replace(/\/\/[^\n]*/g, ' ')
    .replace(/'[^'\\]*(?:\\.[^'\\]*)*'/g, ' ')
    .replace(/"[^"\\]*(?:\\.[^"\\]*)*"/g, ' ')
    .replace(/`[^`\\]*(?:\\.[^`\\]*)*`/g, ' ');
}
function idsOf(text) { return new Set(stripStringsAndComments(text).match(/[A-Za-z_$][\w$]*/g) || []); }
function importLines(text, fromFile, extraNames = []) {
  const used = idsOf(text);
  const by = new Map();
  for (const n of extraNames) used.add(n);
  for (const im of imports) {
    if (im.name === 'ApiService' || im.name === 'ToastService') continue;
    if (!used.has(im.name)) continue;
    let mod = im.module;
    if (mod.startsWith('.')) {
      const abs = path.resolve(ORIG_DIR, mod);
      let rel = path.relative(path.dirname(fromFile), abs).split(path.sep).join('/');
      if (!rel.startsWith('.')) rel = './' + rel;
      mod = rel;
    }
    if (!by.has(mod)) by.set(mod, []);
    by.get(mod).push((im.type ? 'type ' : '') + (im.orig ? `${im.orig} as ${im.name}` : im.name));
  }
  return by;
}
function emitImports(by) {
  const lines = [];
  for (const [mod, names] of by) lines.push(`import { ${[...new Set(names)].join(', ')} } from '${mod}';`);
  return lines.join(EOL);
}

// 1. Generate ReviewService
{
  const def = S.review;
  const body = [];
  for (const n of memberOrder.filter(x => def.names.includes(x))) {
    for (const e of members.get(n)) body.push('  ' + e.text);
  }
  const bodyText = body.join(EOL + EOL);
  const by = importLines(bodyText, path.join(ROOT, def.file), ['ClassificationReview', 'ClassificationReviewRow']);
  const head = emitImports(by);
  const src = [
    `import { Injectable } from '@angular/core';`,
    `import { ApiService } from '../../../core/api/api.service';`,
    head,
    ``,
    `@Injectable({ providedIn: 'root' })`,
    `export class ${def.cls} {`,
    `  constructor(private api: ApiService) {}`,
    ``,
    bodyText,
    `}`
  ].filter(Boolean).join(EOL) + EOL;
  write(def.file, src);
}

// 2. Generate TreeService
{
  const def = S.tree;
  const body = [];
  for (const n of memberOrder.filter(x => def.names.includes(x))) {
    for (const e of members.get(n)) {
      let t = e.text;
      t = t.replace(/this\.review\./g, 'this.review.');
      t = t.replace(/this\.loadReview\(\)/g, 'this.review.loadReview()');
      t = t.replace(/this\.funds\.fundsFor/g, 'this.onReclassifyDone?.()');
      t = t.replace(/this\.funds\.loadNodeFunds\(1\)/g, '/* handled by callback */');
      if (n === 'runReclassify') {
        t = t.replace(/if\s*\(\s*this\.onReclassifyDone\?\.\(\)\s*\)\s*\/\* handled by callback \*\/\s*;?/, 'this.onReclassifyDone?.();');
      }
      body.push('  ' + t);
    }
  }
  let bodyText = body.join(EOL + EOL);
  const by = importLines(bodyText, path.join(ROOT, def.file), ['AdminClassificationNode']);
  const head = emitImports(by);
  const src = [
    `import { Injectable } from '@angular/core';`,
    `import { ApiService } from '../../../core/api/api.service';`,
    `import { ToastService } from '../../../core/services/toast.service';`,
    `import { AdminClassificationReviewService } from './admin-classification-review.service';`,
    head,
    ``,
    `@Injectable({ providedIn: 'root' })`,
    `export class ${def.cls} {`,
    `  /** Optional callback registered by FundsService to avoid circular DI */`,
    `  onReclassifyDone?: () => void;`,
    ``,
    `  constructor(`,
    `    private api: ApiService,`,
    `    private toast: ToastService,`,
    `    private review: AdminClassificationReviewService`,
    `  ) {}`,
    ``,
    bodyText,
    `}`
  ].filter(Boolean).join(EOL) + EOL;
  write(def.file, src);
}

// 3. Generate FundsService
{
  const def = S.funds;
  const body = [];
  for (const n of memberOrder.filter(x => def.names.includes(x))) {
    for (const e of members.get(n)) {
      let t = e.text;
      t = t.replace(/this\.(\w+)/g, (m, nm) => {
        const owner = memberOwner.get(nm);
        if (owner && owner !== 'funds') return `this.${owner}.${nm}`;
        return m;
      });
      body.push('  ' + t);
    }
  }
  let bodyText = body.join(EOL + EOL);
  const by = importLines(bodyText, path.join(ROOT, def.file), ['AdminClassificationNode', 'ClassificationNodeFundRow']);
  const head = emitImports(by);
  const src = [
    `import { Injectable } from '@angular/core';`,
    `import { ApiService } from '../../../core/api/api.service';`,
    `import { ToastService } from '../../../core/services/toast.service';`,
    `import { AdminClassificationTreeService } from './admin-classification-tree.service';`,
    `import { AdminClassificationReviewService } from './admin-classification-review.service';`,
    head,
    ``,
    `@Injectable({ providedIn: 'root' })`,
    `export class ${def.cls} {`,
    `  constructor(`,
    `    private api: ApiService,`,
    `    private toast: ToastService,`,
    `    private tree: AdminClassificationTreeService,`,
    `    private review: AdminClassificationReviewService`,
    `  ) {`,
    `    this.tree.onReclassifyDone = () => { if (this.fundsFor) this.loadNodeFunds(1); };`,
    `  }`,
    ``,
    bodyText,
    `}`
  ].filter(Boolean).join(EOL) + EOL;
  write(def.file, src);
}

// Child components generator
function writeClassComp(folder, selector, clsName, rawHtml, svcAliases, iconMap = {}, extraImports = [], extraFields = '') {
  const { html, css } = extractInlineStyles(rawHtml, 'ac');

  let rewrittenHtml = html;

  // Replace exact modal expressions
  rewrittenHtml = rewrittenHtml
    .replace(/\*ngIf="editorOpen"/g, '*ngIf="tree.editorOpen"')
    .replace(/\*ngIf="moveNode"/g, '*ngIf="tree.moveNode"')
    .replace(/\*ngIf="mergeSource"/g, '*ngIf="tree.mergeSource"')
    .replace(/\*ngIf="deletePreview"/g, '*ngIf="tree.deletePreview"')
    .replace(/\*ngIf="confirmBox"/g, '*ngIf="tree.confirmBox"')
    .replace(/\*ngIf="bulkMoveOpen"/g, '*ngIf="funds.bulkMoveOpen"')
    .replace(/"moveNode\./g, '"tree.moveNode.')
    .replace(/"mergeSource\./g, '"tree.mergeSource.')
    .replace(/"deletePreview\./g, '"tree.deletePreview.')
    .replace(/"confirmBox\./g, '"tree.confirmBox.')
    .replace(/\bmoveNode=null\b/g, 'tree.moveNode=null')
    .replace(/\bmergeSource=null\b/g, 'tree.mergeSource=null')
    .replace(/\bdeletePreview=null\b/g, 'tree.deletePreview=null')
    .replace(/\bconfirmBox=null\b/g, 'tree.confirmBox=null')
    .replace(/\beditorOpen=false\b/g, 'tree.editorOpen=false')
    .replace(/\bbulkMoveOpen=true\b/g, 'funds.bulkMoveOpen=true')
    .replace(/\bbulkMoveOpen=false\b/g, 'funds.bulkMoveOpen=false')
    .replace(/\bbulkMoveCount\b/g, 'funds.bulkMoveCount')
    .replace(/\b(fundsFor=null;fundsItems=\[\])/g, 'funds.closeFunds()')
    .replace(/\{\{\s*sourceAr\(([^)]+)\)\s*\}\}/g, '{{ funds.sourceAr($1) }}')
    .replace(/\[tree\]="publicTree"/g, '[tree]="tree.publicTree"')
    .replace(/\[tree\]="tree\.publicTree"/g, '[tree]="tree.publicTree"')
    .replace(/\[tree\.tree\]/g, '[tree]');

  // Replace service members across expressions
  for (const [alias, def] of Object.entries(S)) {
    for (const name of def.names) {
      if (['publicTree', 'sourceAr', 'bulkMoveOpen', 'bulkMovePath', 'bulkMoveCount', 'editorOpen', 'moveNode', 'mergeSource', 'deletePreview', 'confirmBox'].includes(name)) continue;
      const rx = new RegExp(`(?<![\\w$.])${name}(?![\\w$])`, 'g');
      rewrittenHtml = rewrittenHtml.replace(rx, `${alias}.${name}`);
    }
  }

  // Clean double prefixes
  rewrittenHtml = rewrittenHtml
    .replace(/tree\.tree\.publicTree/g, 'tree.publicTree')
    .replace(/funds\.funds\./g, 'funds.')
    .replace(/tree\.tree\./g, 'tree.')
    .replace(/review\.review\./g, 'review.')
    .replace(/\[tree\.publicTree\]=/g, '[tree]=')
    .replace(/\[tree\.tree\]=/g, '[tree]=');

  // Fix review?.conflicting / review?.missingChild
  rewrittenHtml = rewrittenHtml
    .replace(/review\.conflictingCount/g, 'review.review?.conflictingCount')
    .replace(/review\.missingChildCount/g, 'review.review?.missingChildCount')
    .replace(/review\.conflicting\b/g, 'review.review?.conflicting')
    .replace(/review\.missingChild\b/g, 'review.review?.missingChild')
    .replace(/review\.review\?\.review\?\./g, 'review.review?.');

  const base = `${folder}.component`;
  const dir = `components/${folder}`;
  const commonImports = [`import { CommonModule } from '@angular/common';`, `import { FormsModule } from '@angular/forms';`];
  const iconNames = Object.keys(iconMap);
  const lucideImports = iconNames.length
    ? `import { LucideAngularModule, ${Object.entries(iconMap).map(([prop, lucideName]) => lucideName === prop ? lucideName : `${lucideName} as ${prop}`).join(', ')} } from 'lucide-angular';`
    : '';
  const svcImportLines = svcAliases.map(a => `import { ${S[a].cls} } from '../../services/${kebab(S[a].cls.replace(/Service$/, ''))}.service';`);
  const extraImportLines = extraImports.map(ex => `import { ${ex.cls} } from '${ex.path}';`);
  const compImports = ['CommonModule', 'FormsModule'];
  if (iconNames.length) compImports.push('LucideAngularModule');
  for (const ex of extraImports) compImports.push(ex.cls);
  const iconProps = Object.keys(iconMap).map(p => `  readonly ${p} = ${p};`).join(EOL);
  const ctorParams = svcAliases.map(a => `public ${a}: ${S[a].cls}`).join(', ');
  const tsCode = [
    `import { Component } from '@angular/core';`,
    ...commonImports,
    ...(lucideImports ? [lucideImports] : []),
    ...svcImportLines,
    ...extraImportLines,
    ``,
    `@Component({`,
    `  selector: '${selector}',`,
    `  standalone: true,`,
    `  imports: [${compImports.join(', ')}],`,
    `  templateUrl: './${base}.html',`,
    `  styleUrl: './${base}.css'`,
    `})`,
    `export class ${clsName} {`,
    ...(iconProps ? [iconProps] : []),
    ...(extraFields ? [extraFields] : []),
    ...(ctorParams ? [`  constructor(${ctorParams}) {}`] : []),
    `}`,
    ``
  ].join(EOL);

  write(`${dir}/${base}.html`, rewrittenHtml + EOL);
  write(`${dir}/${base}.css`, css);
  write(`${dir}/${base}.ts`, tsCode);
  write(`${dir}/${base}.spec.ts`, makeSpec(clsName, base));
}

// 1. AdminClassificationTreeTabComponent (lines 35-103)
writeClassComp(
  'admin-classification-tree-tab',
  'app-admin-classification-tree-tab',
  'AdminClassificationTreeTabComponent',
  htmlLines.slice(34, 103).join(EOL),
  ['tree', 'funds'],
  { EditIcon: 'Edit3', UndoIcon: 'Undo2', PlusIcon: 'Plus' }
);

// 2. AdminClassificationReviewTabComponent (lines 183-244)
writeClassComp(
  'admin-classification-review-tab',
  'app-admin-classification-review-tab',
  'AdminClassificationReviewTabComponent',
  htmlLines.slice(182, 243).join(EOL),
  ['review', 'tree', 'funds'],
  { RefreshIcon: 'RefreshCw' },
  [{ cls: 'ClassificationCascadeComponent', path: '../../../../shared/components/classification-cascade/classification-cascade.component' }],
  `  readonly emptyPath: string[] = [];`
);

// 3. AdminClassificationFundsPanelComponent (lines 104-179)
writeClassComp(
  'admin-classification-funds-panel',
  'app-admin-classification-funds-panel',
  'AdminClassificationFundsPanelComponent',
  htmlLines.slice(103, 178).join(EOL),
  ['funds', 'tree'],
  { XIcon: 'X', RefreshIcon: 'RefreshCw', PlusIcon: 'Plus' },
  [{ cls: 'ClassificationCascadeComponent', path: '../../../../shared/components/classification-cascade/classification-cascade.component' }],
  `  readonly emptyPath: string[] = [];`
);

// 4. AdminClassificationModalsComponent (Editor 273-349, Move 352-365, Merge 368-381, Delete 384-398, BulkMove 401-410, Confirm 413-422)
writeClassComp(
  'admin-classification-modals',
  'app-admin-classification-modals',
  'AdminClassificationModalsComponent',
  htmlLines.slice(272, 422).join(EOL),
  ['tree', 'funds', 'review'],
  { XIcon: 'X', SaveIcon: 'Save' },
  [{ cls: 'ClassificationCascadeComponent', path: '../../../../shared/components/classification-cascade/classification-cascade.component' }],
  `  readonly emptyPath: string[] = [];`
);

console.log('Successfully re-generated AdminClassification domain services and child components');
