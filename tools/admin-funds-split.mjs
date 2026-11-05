// Split script for admin-funds.component (Step 1.2)
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
const require = createRequire(path.resolve('frontend/package.json'));
const ts = require('typescript');

const ROOT = path.resolve('frontend/src/app/features/admin');
const ORIG_DIR = 'frontend/src/app/features/admin/admin-funds';
const tsSrc = fs.readFileSync(path.join('tools/admin-funds-orig', 'admin-funds.component.ts'), 'utf8');
const htmlLines = fs.readFileSync(path.join('tools/admin-funds-orig', 'admin-funds.component.html'), 'utf8').split(/\r?\n/);
const EOL = '\r\n';
const kebab = s => s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();

function write(rel, content) {
  const p = path.join(ROOT, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content, 'utf8');
}

function extractInlineStyles(rawHtml, classPrefix = 'af') {
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

// Write FundCategoryRow model
write('models/fund-category-row.model.ts', [
  `import { FundCategory } from '../../../shared/models/api.models';`,
  ``,
  `export interface FundCategoryRow {`,
  `  category: FundCategory;`,
  `  isIncluded: boolean;`,
  `  source: string;`,
  `  overrideId?: number | null;`,
  `}`,
  ``
].join(EOL));

// Parse original class
const sf = ts.createSourceFile('admin-funds.ts', tsSrc, ts.ScriptTarget.Latest, true);
const imports = [];
let cls;
for (const st of sf.statements) {
  if (ts.isImportDeclaration(st)) {
    const nb = st.importClause?.namedBindings;
    if (nb && ts.isNamedImports(nb)) for (const el of nb.elements)
      imports.push({ name: el.name.text, orig: el.propertyName?.text, module: st.moduleSpecifier.text, type: el.isTypeOnly });
  }
  if (ts.isClassDeclaration(st) && st.name?.text === 'AdminFundsComponent') cls = st;
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

// Define the 4 services for admin-funds
const S = {
  sync: {
    cls: 'AdminFundsSyncLogService',
    members: 'syncRuns syncPage syncTotalPages syncLoading selectedRunId runDetails runDetailsLoading loadSyncHistory toggleRunDetails syncStatusLabel syncStatusColor detailStatusLabel'
  },
  categories: {
    cls: 'AdminFundCategoriesService',
    members: 'categories counts catsLoading catsBusy showDeleted selectedCategory members memberTotal memberPage memberPageSize memberTotalPages memberSearch memberSource checkedMembers allMembersChecked addSearch addResults checkedAdd editorOpen editingCategory editorKeywords editorForm mergeSource mergeTargetId mergeTargets confirmBox loadCategories refreshCounts openEditor keywordsFromRule saveCategory askDeleteCategory afterCategoryGone restoreCategory openMerge doMerge selectCategory loadMembers toggleMemberCheck toggleAllMembers bulkRemoveFromCategory finishBulkRemove removeFundFromCategory searchFundsToAdd toggleAddCheck bulkAddToCategory finishBulkAdd resetCategory runConfirm changeLog logLoading logPage logPageSize logTotalPages logCategoryId loadChangeLog changeActionLabel'
  },
  types: {
    cls: 'AdminFundTypesService',
    members: 'types typeCounts typeDraft typeChanges typeMergeSource typeMergeTargetId fundTypeChangeLabelAr typeDirty typeOriginal typeFundsFor typeFundsItems typeFundsTotal typeFundsPage typeFundsTotalPages typeFundsSearch activeTypes loadTypes createType updateType toggleTypeVisibility deleteType restoreType openMergeSource confirmMerge onTypeChange saveType revertType viewTypeFunds loadTypeFunds setFundTypeFromPanel resetFundTypeFromPanel'
  },
  funds: {
    cls: 'AdminFundDetailsService',
    members: 'fundQuery fundResults fundPage fundTotalPages selectedFundId fundCatRows fetchingMissingPrices missingPricesResults missingPricesErrors editingFundId editingFundCode isFundDetailsDirty savingFundDetails fundDetailsSaveError fundDetailsSaveSuccess fundDetailsDraft fundDetailsOriginal onBeforeUnload searchAdminFunds fetchMissingPrices openFundDetailsEditor closeFundDetailsEditor revertFundDetailsDraft saveFundDetails openFund setFundCategory resetFund'
  }
};

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

// Generate the 4 services
for (const def of Object.values(S)) {
  const otherSvcs = new Set(), deps = new Set();
  const body = [];
  for (const n of memberOrder.filter(x => def.names.includes(x))) {
    for (const e of members.get(n)) {
      let t = e.text;
      t = t.replace(/this\.(\w+)/g, (m, nm) => {
        const owner = memberOwner.get(nm);
        if (owner && owner !== def.alias) { otherSvcs.add(owner); return `this.${owner}.${nm}`; }
        return m;
      });
      for (const d of ['api', 'toast', 'sessionCache', 'cache']) if (new RegExp(`this\\.${d}\\b`).test(t)) deps.add(d);
      const cm = e.comments.length ? e.comments.map(c => '  ' + c.trim()).join(EOL) + EOL : '';
      body.push(cm + '  ' + t);
    }
  }
  const ctorParams = [];
  const depTypes = { api: ['ApiService', 'private api'], toast: ['ToastService', 'private toast'], sessionCache: ['SessionCacheService', 'private sessionCache'], cache: ['SessionCacheService', 'private sessionCache'] };
  for (const d of ['api', 'toast', 'sessionCache']) if (deps.has(d)) ctorParams.push(`${depTypes[d][1]}: ${depTypes[d][0]}`);
  for (const o of otherSvcs) {
    const otherDef = S[o];
    const rel = path.relative(path.dirname(path.join(ROOT, def.file)), path.join(ROOT, otherDef.file)).split(path.sep).join('/').replace(/\.ts$/, '');
    ctorParams.push(`private ${o}: ${otherDef.cls}`);
  }
  let bodyText = body.join(EOL + EOL);
  const by = importLines(bodyText + ' ' + ctorParams.join(' '), path.join(ROOT, def.file), []);
  const extraImports = [`import { Injectable } from '@angular/core';`];
  for (const o of otherSvcs) {
    const otherDef = S[o];
    let rel = path.relative(path.dirname(path.join(ROOT, def.file)), path.join(ROOT, otherDef.file)).split(path.sep).join('/').replace(/\.ts$/, '');
    if (!rel.startsWith('.')) rel = './' + rel;
    extraImports.push(`import { ${otherDef.cls} } from '${rel}';`);
  }
  if (/\bFundCategoryRow\b/.test(bodyText)) {
    let rel = path.relative(path.dirname(path.join(ROOT, def.file)), path.join(ROOT, 'models/fund-category-row.model.ts')).split(path.sep).join('/').replace(/\.ts$/, '');
    if (!rel.startsWith('.')) rel = './' + rel;
    extraImports.push(`import { FundCategoryRow } from '${rel}';`);
  }
  const head = emitImports(by);
  const ctor = ctorParams.length ? `  constructor(${EOL}    ${ctorParams.join(',' + EOL + '    ')}${EOL}  ) {}${EOL}${EOL}` : '';
  const src = extraImports[0] + EOL + (head ? head + EOL : '') + (extraImports.length > 1 ? extraImports.slice(1).join(EOL) + EOL : '') + EOL +
    `@Injectable({ providedIn: 'root' })${EOL}export class ${def.cls} {${EOL}${ctor}${bodyText}${EOL}}${EOL}`;
  write(def.file, src);
}
console.log('Generated 4 admin funds services');

// Child components generator for admin-funds
function writeFundsComp(folder, selector, clsName, rawHtml, svcAliases, iconMap = {}) {
  const { html, css } = extractInlineStyles(rawHtml, 'af');
  const rewrittenHtml = html.replace(/(?<=[^.\w$]|^)([A-Za-z_$][\w$]*)\b/g, (m, id) => {
    const owner = memberOwner.get(id);
    if (owner && svcAliases.includes(owner)) return `${owner}.${id}`;
    return m;
  });
  const base = `${folder}.component`;
  const dir = `components/${folder}`;
  const commonImports = [`import { CommonModule } from '@angular/common';`, `import { FormsModule } from '@angular/forms';`];
  const iconNames = Object.keys(iconMap);
  const lucideImports = iconNames.length
    ? `import { LucideAngularModule, ${Object.entries(iconMap).map(([prop, lucideName]) => lucideName === prop ? lucideName : `${lucideName} as ${prop}`).join(', ')} } from 'lucide-angular';`
    : '';
  const svcImportLines = svcAliases.map(a => `import { ${S[a].cls} } from '../../${S[a].file.replace(/\.ts$/, '')}';`);
  const compImports = ['CommonModule', 'FormsModule'];
  if (iconNames.length) compImports.push('LucideAngularModule');
  const iconProps = Object.keys(iconMap).map(p => `  readonly ${p} = ${p};`).join(EOL);
  const ctorParams = svcAliases.map(a => `public ${a}: ${S[a].cls}`).join(', ');
  const tsCode = [
    `import { Component } from '@angular/core';`,
    ...commonImports,
    ...(lucideImports ? [lucideImports] : []),
    ...svcImportLines,
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
    ...(ctorParams ? [`  constructor(${ctorParams}) {}`] : []),
    `}`,
    ``
  ].join(EOL);
  write(`${dir}/${base}.html`, rewrittenHtml + EOL);
  write(`${dir}/${base}.css`, css);
  write(`${dir}/${base}.ts`, tsCode);
  write(`${dir}/${base}.spec.ts`, makeSpec(clsName, base));
}

// 1. AdminFundsSyncTabComponent (lines 187-241)
writeFundsComp(
  'admin-funds-sync-tab',
  'app-admin-funds-sync-tab',
  'AdminFundsSyncTabComponent',
  `<div class="admin-card">${EOL}` + htmlLines.slice(187, 240).join(EOL) + `${EOL}</div>`,
  ['sync'],
  {}
);

// 2. AdminFundsCategoriesTabComponent (lines 244-395)
writeFundsComp(
  'admin-funds-categories-tab',
  'app-admin-funds-categories-tab',
  'AdminFundsCategoriesTabComponent',
  `<div class="admin-card">${EOL}` + htmlLines.slice(244, 394).join(EOL) + `${EOL}</div>`,
  ['categories'],
  { RefreshIcon: 'RefreshCw', PlusIcon: 'Plus', EditIcon: 'Edit3', UndoIcon: 'Undo2', RotateIcon: 'RotateCcw', XIcon: 'X' }
);

// 3. AdminFundsTableTabComponent (lines 398-608)
writeFundsComp(
  'admin-funds-table-tab',
  'app-admin-funds-table-tab',
  'AdminFundsTableTabComponent',
  `<div class="admin-card">${EOL}` + htmlLines.slice(398, 607).join(EOL) + `${EOL}</div>`,
  ['funds'],
  { SearchIcon: 'Search', EditIcon: 'Edit3', RefreshIcon: 'RefreshCw', XIcon: 'X', UndoIcon: 'Undo2', SaveIcon: 'Save', RotateIcon: 'RotateCcw' }
);

// 4. AdminFundsChangeLogTabComponent (lines 611-646)
writeFundsComp(
  'admin-funds-change-log-tab',
  'app-admin-funds-change-log-tab',
  'AdminFundsChangeLogTabComponent',
  `<div class="admin-card">${EOL}` + htmlLines.slice(611, 645).join(EOL) + `${EOL}</div>`,
  ['categories'],
  { RefreshIcon: 'RefreshCw' }
);

// 5. AdminFundsModalsComponent (Editor modal 649-705, Merge modal 708-729, Confirm modal 732-741)
writeFundsComp(
  'admin-funds-modals',
  'app-admin-funds-modals',
  'AdminFundsModalsComponent',
  htmlLines.slice(648, 741).join(EOL),
  ['categories'],
  { XIcon: 'X', SaveIcon: 'Save' }
);

console.log('Generated 5 admin funds child components');
