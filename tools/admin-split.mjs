// Generator for the admin dashboard split (step 1.2). Reads the ORIGINAL AdminComponent (from git) and produces
// services, routed pages and child components. Run from repo root:  node tools/admin-split.mjs
import fs from 'node:fs';
import path from 'node:path';
import { execSync } from 'node:child_process';
import { createRequire } from 'node:module';
const require = createRequire(path.resolve('frontend/package.json'));
const ts = require('typescript');

const ROOT = path.resolve('frontend/src/app/features/admin');
const ORIG_DIR = 'frontend/src/app/features/admin/admin';
const tsSrc = fs.readFileSync(path.join('tools/admin-orig', 'admin.component.ts'), 'utf8');
const htmlLines = fs.readFileSync(path.join('tools/admin-orig', 'admin.component.html'), 'utf8').split(/\r?\n/);
const EOL = '\r\n';
const kebab = s => s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();

// ------------------------------------------------------------------ parse original class
const sf = ts.createSourceFile('admin.ts', tsSrc, ts.ScriptTarget.Latest, true);
const imports = [];
let cls;
for (const st of sf.statements) {
  if (ts.isImportDeclaration(st)) {
    const nb = st.importClause?.namedBindings;
    if (nb && ts.isNamedImports(nb)) for (const el of nb.elements)
      imports.push({ name: el.name.text, orig: el.propertyName?.text, module: st.moduleSpecifier.text, type: el.isTypeOnly });
  }
  if (ts.isClassDeclaration(st) && st.name?.text === 'AdminComponent') cls = st;
}
const members = new Map(); // name -> [{text}]
const memberOrder = [];
for (const m of cls.members) {
  if (ts.isConstructorDeclaration(m)) continue;
  const name = m.name?.getText(sf);
  if (!name) continue;
  const cr = ts.getLeadingCommentRanges(tsSrc, m.pos) || [];
  const comments = cr.map(r => tsSrc.slice(r.pos, r.end)).filter(c => !c.includes('──') && !c.startsWith('// ──'));
  let text = tsSrc.slice(m.getStart(sf), m.end);
  // member indentation: re-indent to 2 spaces (already 2)
  const entry = { text, comments };
  if (!members.has(name)) { members.set(name, []); memberOrder.push(name); }
  members.get(name).push(entry);
}

// ------------------------------------------------------------------ service definitions
const S = {
  jobs: { cls: 'AdminScrapingJobsService', members: 'scrapeStatus activeTriggerBucket jobSettings jobSettingsLoading togglingJobKey jobToggleError isSnapshotsRunning snapshotsRunResult snapshotsRunError snapshotsLastRunText loadJobSettings loadSnapshotsLastRun toggleJobSetting triggerSnapshotsRefresh isJobEnabled isDailyRunning isQuarterlyRunning isMarketSnapshotsRunning isScrapingRunning runningJobs recentlyFinishedJobs getJobNameAr getJobStatusLabel getBucketLastRun triggerDailyScrape triggerQuarterlyScrape fetchScrapeStatus' },
  sectorDaily: { cls: 'AdminSectorDailyService', members: 'sectorDailyStatus sectorDailyRecalculating sectorDailyRangeRunning sectorDailyMessage sectorDailyIsError rangeFromDate rangeToDate fetchSectorDailyStatus recalculateSectorDailyToday recomputeSectorDailyRange getSectorName' },
  fundsSync: { cls: 'AdminFundsSyncService', members: 'fundSyncSettings fundSyncLoading fundSyncRunning fundPriceSyncRunning fundHistoricalSyncRunning fundCombinedSyncRunning fundCheckNewRunning fundSyncStatus fundPriceSyncResult fundPriceSyncError fundCheckNewResult fundCheckNewError fundCombinedMessage fundSyncRunResult fundSyncRunError fundSyncHistory fundSyncHistoryLoading fundSyncHistoryPage fundSyncHistoryPageSize fundSyncHistoryTotalPages fundSyncHistoryTotalCount adminFundPickerList loadFundSyncSettings normalizeSyncSettings toggleFundSync onFundSyncTimeChange saveFundSyncSettings fetchFundSyncStatus isAnyFundOperationRunning loadAdminFundPickerList runFundPriceSyncNow getFundPriceSyncLastRunText getFundHistoricalSyncLastRunText getFundNewCheckLastRunText runPricesAndHistoricalSync runCheckNewFundsNow loadFundSyncHistory getFundSyncLastRunText getFundSyncStatusLabel getFundSyncStatusColor fundSyncRunResultText isFundSyncRunning' },
  historical: { cls: 'AdminHistoricalSyncService', members: 'showHistoricalModal historicalSelectedFundId historicalPreviewLoading historicalCommitLoading historicalPreview historicalCommitResult historicalError overwriteDifferentChoice openHistoricalSyncModal closeHistoricalSyncModal fetchHistoricalPreview commitHistoricalSync confirmOverwriteDifferent' },
  compare: { cls: 'AdminFundCompareService', members: 'showCompareChartModal compareChartSelectedFundId compareChartTimeframe compareChartLoading compareChartData compareTimeframes openCompareChartModal closeCompareChartModal selectCompareTimeframe loadCompareChartData' },
  lookup: { cls: 'AdminStockLookupService', members: 'allStocks loadStocks' },
  marketData: { cls: 'AdminMarketDataService', members: 'selectedTicker selectedStockMarketData loadingStock savingMarketData saveError saveSuccessResult marketForm loadStockMarketData populateForm resetMarketForm saveMarketData' },
  overrides: { cls: 'AdminShariahOverridesService', members: 'shariahOverrides shariahOverrideForm savingShariahOverrides shariahOverridesError shariahOverridesSuccess bourseHalalStandards scholarsStandardNameAr scholarsStandardNameEn scholarsDebtMaxVal shariahSelectedTicker shariahSelectedStockMarketData shariahLoadingStock loadShariahStockMarketData loadShariahOverrides populateShariahOverrideForm _coreActivityCheckboxValue coreActivityCheckboxValue adminEvaluation adminIsViaScholarsOnly adminOverallVerdict updateBourseHalalStandards saveShariahOverrides resetShariahOverride getFieldLabel passedStandardsCount' },
  indexUpload: { cls: 'AdminIndexUploadService', members: 'indices selectedUploadIndex uploadFile uploading uploadResult loadIndices onFileSelected onUploadSubmit' },
  seed: { cls: 'AdminShariahSeedService', members: 'shariahRefreshing shariahSeeding verdictRecomputing backfillingActivity recomputeResult refreshResult seedResult seedJsonOverride triggerVerdictRecompute triggerShariahRefresh triggerShariahSeed backfillCoreActivityCompliant' },
  pdf: { cls: 'AdminSourcePdfService', members: 'selectedPdfSource pdfReportDate selectedPdfFile pdfUploading pdfUploadResult pdfUploadError sourcePdfStatuses sourcePdfLoading sourcePdfError onPdfFileSelected formatFileSize uploadPdfReport loadSourcePdfStatus getSourcePdfState getSourcePdfViewUrl formatPdfDate' },
  review: { cls: 'AdminRemovalCandidatesService', members: 'removalCandidates selectedTickers candidatesLoading candidatesBusy candidatesMessage candidatesError refreshSelectedResult loadRemovalCandidates toggleCandidate allCandidatesSelected toggleSelectAllCandidates refreshSelectedCandidates confirmSelectedCandidates reactivateSelectedCandidates reasonLabel' },
  mgmt: { cls: 'AdminStocksManagementService', members: 'mgmtSearch mgmtStocks mgmtSectors mgmtIndices mgmtLoading mgmtBusy mgmtMessage mgmtError mgmtPickedSector mgmtPickedIndex showCreateStockModal newStock loadMgmtData onMgmtSearchChange createStock assignSector removeFromSector addToIndex removeFromIndex' },
};
const memberOwner = new Map();
for (const [alias, def] of Object.entries(S)) {
  def.alias = alias; def.names = def.members.split(/\s+/);
  def.file = `services/${kebab(def.cls.replace(/Service$/, ''))}.service.ts`;
  for (const n of def.names) { if (!members.has(n)) throw new Error('missing member ' + n); memberOwner.set(n, alias); }
}
// extra members (not in the original class) that are also owned by services: none.
const unassigned = memberOrder.filter(n => !memberOwner.has(n));
console.log('UNASSIGNED members (go to pages / dropped):', unassigned.join(' '));

// ------------------------------------------------------------------ helpers
function idsOf(text) { return new Set(text.match(/[A-Za-z_$][\w$]*/g) || []); }
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
function emitImports(by, extra = []) {
  const lines = [];
  for (const [mod, names] of by) lines.push(`import { ${[...new Set(names)].join(', ')} } from '${mod}';`);
  for (const e of extra) lines.push(e);
  return lines.join(EOL);
}
function write(rel, content) {
  const p = path.join(ROOT, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content, 'utf8');
}
const relFromTo = (fromFile, toFile) => { let r = path.relative(path.dirname(path.join(ROOT, fromFile)), path.join(ROOT, toFile)).split(path.sep).join('/').replace(/\.ts$/, ''); return r.startsWith('.') ? r : './' + r; };

// ------------------------------------------------------------------ services
const serviceInfo = {};
for (const def of Object.values(S)) {
  let body = [];
  const deps = new Set(), otherSvcs = new Set();
  for (const n of memberOrder.filter(x => def.names.includes(x))) {
    for (const e of members.get(n)) {
      let t = e.text;
      t = t.replace(/this\.(\w+)/g, (m, nm) => {
        const owner = memberOwner.get(nm);
        if (owner && owner !== def.alias) { otherSvcs.add(owner); return `this.${owner}.${nm}`; }
        return m;
      });
      for (const d of ['api', 'toast', 'cache', 'auth']) if (new RegExp(`this\\.${d}\\b`).test(t)) deps.add(d);
      const cm = e.comments.length ? e.comments.map(c => '  ' + c.trim()).join(EOL) + EOL : '';
      body.push(cm + '  ' + t);
    }
  }
  serviceInfo[def.alias] = { deps, otherSvcs };
}

function genService(def) {
  const { deps, otherSvcs } = serviceInfo[def.alias];
  let body = [];
  for (const n of memberOrder.filter(x => def.names.includes(x))) {
    for (const e of members.get(n)) {
      let t = e.text.replace(/this\.(\w+)/g, (m, nm) => {
        const owner = memberOwner.get(nm);
        return owner && owner !== def.alias ? `this.${owner}.${nm}` : m;
      });
      const cm = e.comments.length ? e.comments.map(c => '  ' + c.trim()).join(EOL) + EOL : '';
      body.push(cm + '  ' + t);
    }
  }
  const ctorParams = [];
  const depTypes = { api: ['ApiService', 'private api'], toast: ['ToastService', 'private toast'], cache: ['SessionCacheService', 'private cache'], auth: ['AdminAuthService', 'private auth'] };
  for (const d of ['api', 'toast', 'cache', 'auth']) if (deps.has(d)) ctorParams.push(`${depTypes[d][1]}: ${depTypes[d][0]}`);
  for (const o of otherSvcs) ctorParams.push(`private ${o}: ${S[o].cls}`);
  const bodyText = body.join(EOL + EOL);
  const by = importLines(bodyText + ' ' + ctorParams.join(' '), path.join(ROOT, def.file), []);
  const extraImports = [`import { Injectable } from '@angular/core';`];
  for (const o of otherSvcs) extraImports.push(`import { ${S[o].cls} } from '${relFromTo(def.file, S[o].file)}';`);
  const head = emitImports(by, []);
  const ctor = ctorParams.length ? `  constructor(${EOL}    ${ctorParams.join(',' + EOL + '    ')}${EOL}  ) {}${EOL}${EOL}` : '';
  const src = extraImports.slice(0, 1).join(EOL) + EOL + head + (extraImports.length > 1 ? EOL + extraImports.slice(1).join(EOL) : '') + EOL + EOL +
    `@Injectable()${EOL}export class ${def.cls} {${EOL}${ctor}${bodyText}${EOL}}${EOL}`;
  write(def.file, src);
}
for (const def of Object.values(S)) genService(def);

// ------------------------------------------------------------------ template processing
const iconFields = new Map(); // XxxIcon -> expression
for (const [n, arr] of members) if (/Icon$/.test(n)) iconFields.set(n, arr[0].text.replace(/^readonly\s+\w+\s*=\s*/, '').replace(/;$/, ''));

function prefixExpr(expr, used) {
  const re = /('(?:\\.|[^'\\])*'|"(?:\\.|[^"\\])*")|([A-Za-z_$][\w$]*)/g;
  let out = '', last = 0, m;
  while ((m = re.exec(expr))) {
    out += expr.slice(last, m.index);
    last = m.index + m[0].length;
    if (m[1]) { out += m[0]; continue; }
    const id = m[2];
    const before = expr.slice(0, m.index).replace(/\s+$/, '');
    const prevCh = before.slice(-1);
    const after = expr.slice(last).replace(/^\s+/, '');
    const isProp = prevCh === '.';
    const isKey = after.startsWith(':') && (prevCh === '{' || prevCh === ',');
    const isDecl = /\b(let|as)$/.test(before);
    const owner = memberOwner.get(id);
    if (owner && !isProp && !isKey && !isDecl) { used.add(owner); out += `${owner}.${id}`; }
    else out += m[0];
  }
  return out + expr.slice(last);
}

// tag scanner
function processHtml(html, usedSvcs, styleCtx) {
  let out = '', i = 0;
  while (i < html.length) {
    if (html.startsWith('<!--', i)) { const e = html.indexOf('-->', i); out += html.slice(i, e + 3); i = e + 3; continue; }
    if (html[i] === '<' && /[A-Za-z]/.test(html[i + 1] || '')) {
      // scan tag
      let j = i + 1;
      while (j < html.length && !/[\s>\/]/.test(html[j])) j++;
      const name = html.slice(i + 1, j);
      const attrs = [];
      let selfClose = false;
      while (j < html.length) {
        while (/\s/.test(html[j])) j++;
        if (html[j] === '>') { j++; break; }
        if (html[j] === '/' && html[j + 1] === '>') { selfClose = true; j += 2; break; }
        let s = j;
        while (j < html.length && !/[\s=>\/]/.test(html[j])) j++;
        if (j === s) { j++; continue; }
        const an = html.slice(s, j);
        let val = null, q = null;
        if (html[j] === '=') {
          j++;
          q = html[j];
          if (q === '"' || q === "'") { const e = html.indexOf(q, j + 1); val = html.slice(j + 1, e); j = e + 1; }
          else { let s2 = j; while (j < html.length && !/[\s>]/.test(html[j])) j++; val = html.slice(s2, j); q = '"'; }
        }
        attrs.push({ n: an, v: val, q });
      }
      out += rebuildTag(name, attrs, selfClose, usedSvcs, styleCtx);
      i = j; continue;
    }
    // text node up to next '<'
    let e = html.indexOf('<', i + 1);
    if (e < 0) e = html.length;
    out += html.slice(i, e).replace(/\{\{([\s\S]*?)\}\}/g, (m, ex) => '{{' + prefixExpr(ex, usedSvcs) + '}}');
    i = e;
  }
  return out;
}
function rebuildTag(name, attrs, selfClose, usedSvcs, styleCtx) {
  // remove activeTab ngIf
  attrs = attrs.filter(a => !(a.n === '*ngIf' && /^\s*activeTab\s*===/.test(a.v || '')));
  let styleAttr = attrs.find(a => a.n === 'style');
  if (styleAttr && styleCtx && styleAttr.v !== null && !/\{\{/.test(styleAttr.v)) {
    const cn = styleCtx.classFor(styleAttr.v);
    attrs = attrs.filter(a => a !== styleAttr);
    const ca = attrs.find(a => a.n === 'class');
    if (ca) ca.v = (ca.v ? ca.v + ' ' : '') + cn; else attrs.push({ n: 'class', v: cn, q: '"' });
  }
  const parts = attrs.map(a => {
    if (a.v === null) return a.n;
    let v = a.v;
    if (/^(\[|\(|\*)/.test(a.n) && !a.n.startsWith('(') ) {
      v = a.n.startsWith('*') ? prefixMicro(v, usedSvcs) : prefixExpr(v, usedSvcs);
    } else if (a.n.startsWith('(')) v = prefixExpr(v, usedSvcs);
    else if (/\{\{/.test(v)) v = v.replace(/\{\{([\s\S]*?)\}\}/g, (m, ex) => '{{' + prefixExpr(ex, usedSvcs) + '}}');
    return `${a.n}=${a.q}${v}${a.q}`;
  });
  return `<${name}${parts.length ? ' ' + parts.join(' ') : ''}${selfClose ? ' /' : ''}>`;
}
function prefixMicro(v, usedSvcs) {
  // keep 'let x of' / '; else tpl' structure: prefix only expression portions
  return prefixExpr(v, usedSvcs).replace(/\blet (\w+) = /g, 'let $1 = ');
}

class StyleCtx {
  constructor(prefix) { this.prefix = prefix; this.map = new Map(); this.n = 0; }
  classFor(style) {
    const norm = style.replace(/\s+/g, ' ').trim().replace(/;$/, '');
    if (!this.map.has(norm)) this.map.set(norm, `${this.prefix}-${++this.n}`);
    return this.map.get(norm);
  }
  css() {
    const out = [];
    for (const [s, cn] of this.map) {
      const decls = s.split(/;(?![^(]*\))/).map(d => d.trim()).filter(Boolean).map(d => `  ${d};`).join(EOL);
      out.push(`.${cn}.${cn} {${EOL}${decls}${EOL}}`);
    }
    return out.join(EOL + EOL) + (out.length ? EOL : '');
  }
}

function slice(a, b) {
  const lines = htmlLines.slice(a - 1, b);
  // de-indent common leading whitespace
  const min = Math.min(...lines.filter(l => l.trim()).map(l => l.match(/^ */)[0].length));
  return lines.map(l => l.slice(Math.min(min, l.length))).join('\n');
}

// ------------------------------------------------------------------ components
const lucideImports = new Map();
const compFiles = [];
function genComponent(c) {
  // c: {dir, className, selector, html (string) , extraTs, imports:[class names], componentImports:[{cls,path}], providers, hostDisplayContents }
  const used = new Set();
  const style = new StyleCtx(c.stylePrefix || kebab(c.className.replace(/Component$/, '')).split('-').map(w => w[0]).join(''));
  const html = processHtml(c.html, used, style);
  for (const s of c.extraServices || []) used.add(s);
  const iconsUsed = [...new Set(html.match(/\b\w+Icon\b/g) || [])].filter(n => iconFields.has(n));
  const needsForms = /ngModel|<form|\(ngSubmit\)/.test(html);
  const needsRouterLink = /routerLink/.test(html);
  const needsLucide = /<lucide-icon/.test(html);
  const needsCommon = /\*ngIf|\*ngFor|ngSwitch|\[ngClass\]|\| *(date|number)|ngStyle|\*ngSwitch/.test(html);
  const compImports = [];
  const tsImports = [];
  const impArr = [];
  if (needsCommon) { impArr.push('CommonModule'); tsImports.push(`import { CommonModule } from '@angular/common';`); }
  if (needsForms) { impArr.push('FormsModule'); tsImports.push(`import { FormsModule } from '@angular/forms';`); }
  if (needsRouterLink) { impArr.push('RouterLink'); tsImports.push(`import { RouterLink } from '@angular/router';`); }
  if (needsLucide) { impArr.push('LucideAngularModule'); }
  const lucideNames = new Set();
  const iconCode = [];
  for (const ic of iconsUsed) {
    const ex = iconFields.get(ic); iconCode.push(`  readonly ${ic} = ${ex};`);
    lucideNames.add(ex);
  }
  for (const k of c.componentImports || []) { impArr.push(k.cls); tsImports.push(`import { ${k.cls} } from '${k.path}';`); }
  const svcList = [...used];
  const svcFields = svcList.map(a => `  readonly ${a} = inject(${S[a].cls});`);
  const svcImports = svcList.map(a => `import { ${S[a].cls} } from '${relFromTo(path.join(c.dir, 'x.ts'), S[a].file)}';`);
  const angularCore = ['Component', ...(svcList.length ? ['inject'] : []), ...(c.coreImports || [])];
  const lucideLine = (needsLucide || lucideNames.size) ? `import { LucideAngularModule${[...lucideNames].map(n => ', ' + (n === 'Upload' ? 'Upload' : n)).join('')} } from 'lucide-angular';` : null;
  const lines = [];
  lines.push(`import { ${angularCore.join(', ')} } from '@angular/core';`);
  lines.push(...tsImports);
  if (lucideLine) lines.push(needsLucide ? lucideLine : lucideLine.replace('LucideAngularModule, ', '').replace('LucideAngularModule', ''));
  lines.push(...svcImports);
  if (c.extraImports) lines.push(...c.extraImports);
  const classBody = [...svcFields, ...iconCode, ...(c.extraBody ? [c.extraBody] : [])].join(EOL);
  const decorator = `@Component({${EOL}  selector: '${c.selector}',${EOL}  standalone: true,${EOL}  imports: [${impArr.join(', ')}],${EOL}` +
    (c.providers ? `  providers: [${c.providers.join(', ')}],${EOL}` : '') +
    `  templateUrl: './${c.base}.component.html',${EOL}  styleUrl: './${c.base}.component.css'${EOL}})`;
  const src = lines.join(EOL) + EOL + EOL + decorator + EOL + `export class ${c.className}${c.implements ? ' implements ' + c.implements : ''} {${EOL}${classBody}${EOL}}${EOL}`;
  write(`${c.dir}/${c.base}.component.ts`, src);
  write(`${c.dir}/${c.base}.component.html`, html.replace(/\n/g, EOL) + EOL);
  let css = style.css();
  if (c.hostDisplayContents) css = `:host {${EOL}  display: contents;${EOL}}${EOL}${css ? EOL + css : ''}`;
  write(`${c.dir}/${c.base}.component.css`, css);
  // spec
  const provs = ['provideRouter([])', 'provideHttpClient()', 'provideHttpClientTesting()', ...svcList.map(a => S[a].cls)];
  const spec = [
    `import { ComponentFixture, TestBed } from '@angular/core/testing';`,
    `import { provideRouter } from '@angular/router';`,
    `import { provideHttpClient } from '@angular/common/http';`,
    `import { provideHttpClientTesting } from '@angular/common/http/testing';`,
    `import { ${c.className} } from './${c.base}.component';`,
    ...svcList.map(a => `import { ${S[a].cls} } from '${relFromTo(path.join(c.dir, 'x.ts'), S[a].file)}';`),
    ...(c.specImports || []),
    ``, `describe('${c.className}', () => {`,
    `  let fixture: ComponentFixture<${c.className}>;`, ``,
    `  beforeEach(async () => {`,
    `    await TestBed.configureTestingModule({`,
    `      imports: [${c.className}],`,
    `      providers: [${[...provs, ...(c.specProviders || [])].join(', ')}]`,
    `    }).compileComponents();`,
    `    fixture = TestBed.createComponent(${c.className});`,
    ...(c.specSetup || []),
    `  });`, ``,
    `  it('should create', () => {`,
    `    expect(fixture.componentInstance).toBeTruthy();`,
    `  });`,
    `});`, ``].join(EOL);
  write(`${c.dir}/${c.base}.component.spec.ts`, spec);
  return { used: svcList, lucideNames };
}

export { genComponent, slice, S, iconFields, prefixExpr, processHtml, StyleCtx, write, EOL, kebab, relFromTo, members, memberOrder, unassigned, ROOT };

