// Complete generator for the AdminComponent split (Step 1.2)
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
const require = createRequire(path.resolve('frontend/package.json'));
const ts = require('typescript');

const ROOT = path.resolve('frontend/src/app/features/admin');
const APP_ROOT = path.resolve('frontend/src/app');
const ORIG_DIR = 'frontend/src/app/features/admin/admin';
const tsSrc = fs.readFileSync(path.join('tools/admin-orig', 'admin.component.ts'), 'utf8');
const htmlLines = fs.readFileSync(path.join('tools/admin-orig', 'admin.component.html'), 'utf8').split(/\r?\n/);
const EOL = '\r\n';
const kebab = s => s.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();

function write(rel, content) {
  const p = path.join(ROOT, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content, 'utf8');
}
function writeApp(rel, content) {
  const p = path.join(APP_ROOT, rel);
  fs.mkdirSync(path.dirname(p), { recursive: true });
  fs.writeFileSync(p, content, 'utf8');
}
const relFromTo = (fromFile, toFile) => {
  let r = path.relative(path.dirname(path.join(ROOT, fromFile)), path.join(ROOT, toFile)).split(path.sep).join('/').replace(/\.ts$/, '');
  return r.startsWith('.') ? r : './' + r;
};

// 1. Write model file for EditableMarketForm
write('models/editable-market-form.model.ts', [
  `export interface EditableMarketForm {`,
  `  closingPrice: number | null;`,
  `  high: number | null;`,
  `  low: number | null;`,
  `  open: number | null;`,
  `  nominalValue: number | null;`,
  `  bookValue: number | null;`,
  `  eps: number | null;`,
  `  marketValue: number | null;`,
  `  peRatio: number | null;`,
  `  pbRatio: number | null;`,
  `  sharesOutstanding: number | null;`,
  `  earningsGrowthRate: number | null;`,
  `  epsGrowthRate: number | null;`,
  `  freeCashFlow: number | null;`,
  `  currency: string;`,
  `  sourceLastUpdateText: string;`,
  `  recalculateRatios: boolean;`,
  `}`,
  ``
].join(EOL));

// Parse original class
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

const S = {
  jobs: { cls: 'AdminScrapingJobsService', members: 'scrapeStatus activeTriggerBucket jobSettings jobSettingsLoading togglingJobKey jobToggleError isSnapshotsRunning snapshotsRunResult snapshotsRunError snapshotsLastRunText loadJobSettings loadSnapshotsLastRun toggleJobSetting triggerSnapshotsRefresh isJobEnabled isDailyRunning isQuarterlyRunning isMarketSnapshotsRunning isScrapingRunning runningJobs recentlyFinishedJobs getJobNameAr getJobStatusLabel getBucketLastRun triggerDailyScrape triggerQuarterlyScrape fetchScrapeStatus' },
  sectorDaily: { cls: 'AdminSectorDailyService', members: 'sectorDailyStatus sectorDailyRecalculating sectorDailyRangeRunning sectorDailyMessage sectorDailyIsError rangeFromDate rangeToDate fetchSectorDailyStatus recalculateSectorDailyToday recomputeSectorDailyRange getSectorName' },
  fundsSync: { cls: 'AdminFundsSyncService', members: 'fundSyncSettings fundSyncLoading fundSyncRunning fundPriceSyncRunning fundHistoricalSyncRunning fundCombinedSyncRunning fundCheckNewRunning fundSyncStatus fundPriceSyncResult fundPriceSyncError fundCheckNewResult fundCheckNewError fundCombinedMessage fundSyncRunResult fundSyncRunError fundSyncHistory fundSyncHistoryLoading fundSyncHistoryPage fundSyncHistoryPageSize fundSyncHistoryTotalPages fundSyncHistoryTotalCount adminFundPickerList loadFundSyncSettings normalizeSyncSettings toggleFundSync onFundSyncTimeChange saveFundSyncSettings fetchFundSyncStatus isAnyFundOperationRunning loadAdminFundPickerList runFundPriceSyncNow getFundPriceSyncLastRunText getFundHistoricalSyncLastRunText getFundNewCheckLastRunText runPricesAndHistoricalSync runCheckNewFundsNow loadFundSyncHistory getFundSyncLastRunText getFundSyncStatusLabel getFundSyncStatusColor fundSyncRunResultText isFundSyncRunning' },
  historical: { cls: 'AdminHistoricalSyncService', members: 'showHistoricalModal historicalSelectedFundId historicalPreviewLoading historicalCommitLoading historicalPreview historicalCommitResult historicalError overwriteDifferentChoice openHistoricalSyncModal closeHistoricalSyncModal fetchHistoricalPreview commitHistoricalSync confirmOverwriteDifferent' },
  compare: { cls: 'AdminFundCompareService', members: 'showCompareChartModal compareChartSelectedFundId compareChartTimeframe compareChartLoading compareChartData compareChartInstance compareTimeframes openCompareChartModal closeCompareChartModal selectCompareTimeframe openFundCompareFromPreview openHistoricalSyncForSelectedFund loadCompareChartData renderCompareChart destroyCompareChart' },
  lookup: { cls: 'AdminStockLookupService', members: 'allStocks loadStocks' },
  marketData: { cls: 'AdminMarketDataService', members: 'filteredStocks stockSearchQuery selectedTicker selectedStockMarketData loadingStock savingMarketData saveError saveSuccessResult marketForm onSearchInput selectStockDirectly onStockSelected loadStockMarketData populateForm resetMarketForm saveMarketData' },
  overrides: { cls: 'AdminShariahOverridesService', members: 'shariahOverrides shariahOverrideForm savingShariahOverrides shariahOverridesError shariahOverridesSuccess bourseHalalStandards scholarsStandardNameAr scholarsStandardNameEn scholarsDebtMaxVal filteredShariahStocks shariahStockSearchQuery shariahSelectedTicker shariahSelectedStockMarketData shariahLoadingStock onShariahSearchInput selectShariahStockDirectly onShariahStockSelected loadShariahStockMarketData loadShariahOverrides populateShariahOverrideForm _coreActivityCheckboxValue coreActivityCheckboxValue adminEvaluation adminIsViaScholarsOnly adminOverallVerdict updateBourseHalalStandards saveShariahOverrides resetShariahOverride getFieldLabel passedStandardsCount' },
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

// Generate 13 services
for (const def of Object.values(S)) {
  const otherSvcs = new Set(), deps = new Set();
  const body = [];
  for (const n of memberOrder.filter(x => def.names.includes(x))) {
    for (const e of members.get(n)) {
      let t = e.text;
      if (def.alias === 'lookup' && n === 'loadStocks') {
        t = t.replace(/this\.filteredStocks = \[\.\.\.this\.allStocks\];/g, 'this.onLoaded();');
      }
      if (def.alias === 'seed' && n === 'backfillCoreActivityCompliant') {
        t = t.replace(`this.activeTab === 'shariah-edit' && this.shariahSelectedTicker`, `this.shariahSelectedTicker`);
      }
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
  const ctorParams = [];
  const depTypes = { api: ['ApiService', 'private api'], toast: ['ToastService', 'private toast'], cache: ['SessionCacheService', 'private cache'], auth: ['AdminAuthService', 'private auth'] };
  for (const d of ['api', 'toast', 'cache', 'auth']) if (deps.has(d)) ctorParams.push(`${depTypes[d][1]}: ${depTypes[d][0]}`);
  for (const o of otherSvcs) ctorParams.push(`private ${o}: ${S[o].cls}`);
  let bodyText = body.join(EOL + EOL);
  if (def.alias === 'lookup') {
    bodyText = `  private listeners: Array<() => void> = [];${EOL}${EOL}  onStocksLoaded(fn: () => void): void {${EOL}    this.listeners.push(fn);${EOL}    if (this.allStocks.length > 0) fn();${EOL}  }${EOL}${EOL}  private onLoaded(): void {${EOL}    for (const fn of this.listeners) fn();${EOL}  }${EOL}${EOL}` + bodyText;
  }
  if (def.alias === 'jobs') {
    bodyText += `${EOL}${EOL}  get isAnyJobRunning(): boolean {${EOL}    return this.isDailyRunning || this.isQuarterlyRunning || this.isMarketSnapshotsRunning || this.fundsSync.isFundSyncRunning;${EOL}  }`;
    if (!otherSvcs.has('fundsSync')) {
      otherSvcs.add('fundsSync');
      ctorParams.push(`private fundsSync: ${S.fundsSync.cls}`);
    }
  }
  let ctorBody = '{}';
  if (def.alias === 'marketData') {
    ctorBody = `{${EOL}    this.lookup.onStocksLoaded(() => {${EOL}      if (!this.stockSearchQuery.trim()) {${EOL}        this.filteredStocks = [...this.lookup.allStocks];${EOL}      }${EOL}    });${EOL}  }`;
  }
  if (def.alias === 'overrides') {
    ctorBody = `{${EOL}    this.lookup.onStocksLoaded(() => {${EOL}      if (!this.shariahStockSearchQuery.trim()) {${EOL}        this.filteredShariahStocks = [...this.lookup.allStocks];${EOL}      }${EOL}    });${EOL}  }`;
  }
  const by = importLines(bodyText + ' ' + ctorParams.join(' '), path.join(ROOT, def.file), []);
  const extraImports = [`import { Injectable } from '@angular/core';`];
  for (const o of otherSvcs) extraImports.push(`import { ${S[o].cls} } from '${relFromTo(def.file, S[o].file)}';`);
  if (/\bEditableMarketForm\b/.test(bodyText)) {
    extraImports.push(`import { EditableMarketForm } from '${relFromTo(def.file, 'models/editable-market-form.model.ts')}';`);
  }
  const head = emitImports(by);
  const ctor = ctorParams.length ? `  constructor(${EOL}    ${ctorParams.join(',' + EOL + '    ')}${EOL}  ) ${ctorBody}${EOL}${EOL}` : '';
  const src = extraImports[0] + EOL + (head ? head + EOL : '') + (extraImports.length > 1 ? extraImports.slice(1).join(EOL) + EOL : '') + EOL +
    `@Injectable({ providedIn: 'root' })${EOL}export class ${def.cls} {${EOL}${ctor}${bodyText}${EOL}}${EOL}`;
  write(def.file, src);
}

console.log('Generated 13 admin services with providedIn: root');

// =========================================================================
// Helper: extract inline style="..." attributes into CSS classes (.s1.s1, etc.)
// =========================================================================
function extractInlineStyles(rawHtml, classPrefix = 's') {
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
  // Merge __INLINE_CLS__ into existing class="..." or convert to class="..."
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

// =========================================================================
// 2. Shared Components: StockPickerComponent & StockHeaderBannerComponent
// =========================================================================
{
  const rawHtml = [
    `<div class="stock-picker-box">`,
    `  <label class="stock-picker-label">`,
    `    البحث عن سهم بالرمز أو الاسم:`,
    `  </label>`,
    `  <div class="stock-picker-row">`,
    `    <div class="stock-picker-input-wrap">`,
    `      <input`,
    `        type="text"`,
    `        [ngModel]="searchQuery"`,
    `        (ngModelChange)="searchQueryChange.emit($event)"`,
    `        (input)="searchInput.emit()"`,
    `        placeholder="أدخل رمز السهم (مثال: COMI, ORAS, ESRS)..."`,
    `        class="admin-form input stock-picker-input" />`,
    `      <lucide-icon [img]="SearchIcon" size="18" class="stock-picker-icon"></lucide-icon>`,
    `    </div>`,
    `    <select`,
    `      [ngModel]="selectedTicker"`,
    `      (ngModelChange)="selectedTickerChange.emit($event)"`,
    `      (change)="stockSelected.emit()"`,
    `      class="admin-form select stock-picker-select">`,
    `      <option value="">-- اختر من قائمة الأسهم ({{ filteredStocks.length }} سهم) --</option>`,
    `      <option *ngFor="let s of filteredStocks" [value]="s.ticker">`,
    `        {{ s.ticker }} - {{ s.nameAr || s.nameEn }}`,
    `      </option>`,
    `    </select>`,
    `  </div>`,
    `  <div *ngIf="searchQuery.trim() && filteredStocks.length > 0" class="stock-picker-chips">`,
    `    <span class="stock-picker-chips-label">نتائج مطابقة سريعة:</span>`,
    `    <button`,
    `      *ngFor="let match of filteredStocks.slice(0, 10)"`,
    `      type="button"`,
    `      (click)="directSelect.emit(match.ticker)"`,
    `      class="btn btn-outline stock-picker-chip"`,
    `      [style.borderColor]="selectedTicker === match.ticker ? 'var(--primary)' : 'var(--border)'"`,
    `      [style.color]="selectedTicker === match.ticker ? 'var(--primary)' : 'inherit'">`,
    `      <strong>{{ match.ticker }}</strong> <span class="stock-picker-chip-name">({{ match.nameAr || match.nameEn }})</span>`,
    `    </button>`,
    `  </div>`,
    `</div>`,
    ``
  ].join(EOL);
  const css = [
    `.stock-picker-box.stock-picker-box { background: #fbfcfb; border: 1px solid var(--border); border-radius: 12px; padding: 16px; margin: 20px 0; }`,
    `.stock-picker-label.stock-picker-label { font-size: 13px; font-weight: 600; display: block; margin-bottom: 8px; }`,
    `.stock-picker-row.stock-picker-row { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }`,
    `.stock-picker-input-wrap.stock-picker-input-wrap { position: relative; flex: 1; min-width: 240px; }`,
    `.stock-picker-input.stock-picker-input { min-height: 42px; padding-inline-start: 36px; }`,
    `.stock-picker-icon.stock-picker-icon { position: absolute; right: 10px; top: 12px; color: var(--muted-foreground); }`,
    `.stock-picker-select.stock-picker-select { min-height: 42px; min-width: 260px; flex: 1; }`,
    `.stock-picker-chips.stock-picker-chips { margin-top: 10px; display: flex; flex-wrap: wrap; gap: 6px; }`,
    `.stock-picker-chips-label.stock-picker-chips-label { font-size: 11px; color: var(--muted-foreground); align-self: center; margin-left: 4px; }`,
    `.stock-picker-chip.stock-picker-chip { font-size: 11px; padding: 4px 8px; border-radius: 6px; background: white; }`,
    `.stock-picker-chip-name.stock-picker-chip-name { margin-right: 4px; font-size: 10px; color: var(--muted-foreground); }`,
    ``
  ].join(EOL);
  const tsCode = [
    `import { Component, EventEmitter, Input, Output } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { FormsModule } from '@angular/forms';`,
    `import { LucideAngularModule, Search } from 'lucide-angular';`,
    `import { AdminStockLookupItem } from '../../models/api.models';`,
    ``,
    `@Component({`,
    `  selector: 'app-stock-picker',`,
    `  standalone: true,`,
    `  imports: [CommonModule, FormsModule, LucideAngularModule],`,
    `  templateUrl: './stock-picker.component.html',`,
    `  styleUrl: './stock-picker.component.css'`,
    `})`,
    `export class StockPickerComponent {`,
    `  readonly SearchIcon = Search;`,
    `  @Input() filteredStocks: AdminStockLookupItem[] = [];`,
    `  @Input() searchQuery = '';`,
    `  @Input() selectedTicker = '';`,
    `  @Output() searchQueryChange = new EventEmitter<string>();`,
    `  @Output() selectedTickerChange = new EventEmitter<string>();`,
    `  @Output() searchInput = new EventEmitter<void>();`,
    `  @Output() stockSelected = new EventEmitter<void>();`,
    `  @Output() directSelect = new EventEmitter<string>();`,
    `}`,
    ``
  ].join(EOL);
  writeApp('shared/components/stock-picker/stock-picker.component.html', rawHtml);
  writeApp('shared/components/stock-picker/stock-picker.component.css', css);
  writeApp('shared/components/stock-picker/stock-picker.component.ts', tsCode);
  writeApp('shared/components/stock-picker/stock-picker.component.spec.ts', makeSpec('StockPickerComponent', 'stock-picker.component'));
}

// StockHeaderBannerComponent
{
  const snippet = htmlLines.slice(847, 889).join(EOL).replace(/selectedStockMarketData/g, 'stockMarketData');
  const { html, css } = extractInlineStyles(snippet, 'shb');
  const tsCode = [
    `import { Component, Input } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { MarketDataDto } from '../../models/api.models';`,
    ``,
    `@Component({`,
    `  selector: 'app-stock-header-banner',`,
    `  standalone: true,`,
    `  imports: [CommonModule],`,
    `  templateUrl: './stock-header-banner.component.html',`,
    `  styleUrl: './stock-header-banner.component.css'`,
    `})`,
    `export class StockHeaderBannerComponent {`,
    `  @Input() stockMarketData!: MarketDataDto;`,
    `}`,
    ``
  ].join(EOL);
  writeApp('shared/components/stock-header-banner/stock-header-banner.component.html', html + EOL);
  writeApp('shared/components/stock-header-banner/stock-header-banner.component.css', css);
  writeApp('shared/components/stock-header-banner/stock-header-banner.component.ts', tsCode);
  writeApp('shared/components/stock-header-banner/stock-header-banner.component.spec.ts', makeSpec('StockHeaderBannerComponent', 'stock-header-banner.component'));
}

// =========================================================================
// 3. Admin Feature Child Components
// =========================================================================
function writeAdminComp(folder, selector, clsName, rawHtml, svcAliases, iconNames = [], extraImports = [], extraBody = '', useRouterLink = false, parentDir = 'components/') {
  const { html, css } = extractInlineStyles(rawHtml, 'ac');
  const rewrittenHtml = html.replace(/(?<=[^.\w$]|^)([A-Za-z_$][\w$]*)\b/g, (m, id) => {
    const owner = memberOwner.get(id);
    if (owner && svcAliases.includes(owner)) return `${owner}.${id}`;
    return m;
  });
  const base = `${folder}.component`;
  const dir = `${parentDir}${folder}`;
  const angularCore = ['Component'];
  const commonImports = [`import { CommonModule } from '@angular/common';`, `import { FormsModule } from '@angular/forms';`];
  if (useRouterLink) commonImports.push(`import { RouterLink } from '@angular/router';`);
  const lucideImports = iconNames.length
    ? `import { LucideAngularModule, ${iconNames.map(ic => ic === 'UploadFileIcon' ? 'Upload as UploadIcon' : ic.replace(/Icon$/, '')).join(', ')} } from 'lucide-angular';`
    : '';
  const svcRelPrefix = parentDir.startsWith('pages') ? '../../services/' : '../../services/';
  const svcImportLines = svcAliases.map(a => `import { ${S[a].cls} } from '${svcRelPrefix}${kebab(S[a].cls.replace(/Service$/, ''))}.service';`);
  const compImports = ['CommonModule', 'FormsModule'];
  if (iconNames.length) compImports.push('LucideAngularModule');
  if (useRouterLink) compImports.push('RouterLink');
  for (const ex of extraImports) compImports.push(ex.cls);
  const extraImportLines = extraImports.map(ex => `import { ${ex.cls} } from '${ex.path}';`);
  const iconProps = iconNames.map(ic => `  readonly ${ic} = ${ic === 'UploadFileIcon' ? 'UploadIcon' : ic.replace(/Icon$/, '')};`).join(EOL);
  const ctorParams = svcAliases.map(a => `public ${a}: ${S[a].cls}`).join(', ');
  const tsCode = [
    `import { ${angularCore.join(', ')} } from '@angular/core';`,
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
    ...(ctorParams ? [`  constructor(${ctorParams}) {}`] : []),
    ...(extraBody ? [extraBody] : []),
    `}`,
    ``
  ].join(EOL);
  write(`${dir}/${base}.html`, rewrittenHtml + EOL);
  write(`${dir}/${base}.css`, css);
  write(`${dir}/${base}.ts`, tsCode);
  write(`${dir}/${base}.spec.ts`, makeSpec(clsName, base));
}

// 3.1 AdminPageHeadingComponent (lines 3-9)
writeAdminComp(
  'admin-page-heading',
  'app-admin-page-heading',
  'AdminPageHeadingComponent',
  htmlLines.slice(2, 9).join(EOL),
  [],
  ['DatabaseIcon']
);

// 3.2 JobCardComponent (reusable for Daily Live-Price, Quarterly Slow, Market Snapshots)
{
  const jobCardHtml = [
    `<div class="jc-card">`,
    `  <div>`,
    `    <div class="jc-header">`,
    `      <strong class="jc-title" [style.color]="accentColor">`,
    `        <lucide-icon [img]="icon" size="18"></lucide-icon>`,
    `        {{ title }}`,
    `      </strong>`,
    `      <span class="live-dot" *ngIf="isRunning"></span>`,
    `    </div>`,
    `    <p class="jc-desc">{{ description }}</p>`,
    `    <div class="jc-schedule-box">`,
    `      <div class="jc-schedule-row"><strong>الجدول التلقائي:</strong> {{ scheduleText }}</div>`,
    `      <div><strong>آخر تشغيل مسجل:</strong> {{ lastRunText }}</div>`,
    `    </div>`,
    `    <div class="jc-toggle-box"`,
    `         [style.background]="isEnabled ? enabledBg : '#fef2f2'"`,
    `         [style.borderColor]="isEnabled ? enabledBorder : '#fecaca'">`,
    `      <div>`,
    `        <div class="jc-toggle-title" [style.color]="isEnabled ? enabledTextColor : '#991b1b'">`,
    `          {{ isEnabled ? 'التشغيل التلقائي مُفعَّل' : 'التشغيل التلقائي مُعطَّل' }}`,
    `        </div>`,
    `        <div class="jc-toggle-sub">`,
    `          {{ isEnabled ? enabledSubText : 'لن يتم تنفيذ الجدول التلقائي' }}`,
    `        </div>`,
    `      </div>`,
    `      <button`,
    `        type="button"`,
    `        (click)="toggleJob.emit()"`,
    `        [disabled]="isToggling"`,
    `        class="btn btn-outline jc-toggle-btn"`,
    `        [style.borderColor]="isEnabled ? enabledBtnColor : '#dc2626'"`,
    `        [style.color]="isEnabled ? enabledBtnColor : '#dc2626'">`,
    `        {{ showSavingText && isToggling ? 'جارٍ الحفظ...' : (isEnabled ? 'تعطيل الآلي' : 'تفعيل الآلي') }}`,
    `      </button>`,
    `    </div>`,
    `  </div>`,
    `  <div class="jc-footer">`,
    `    <div *ngIf="isRunning" class="progress-bar-container jc-progress">`,
    `      <div class="progress-bar-fill indeterminate"></div>`,
    `    </div>`,
    `    <button`,
    `      [class]="runBtnClass"`,
    `      (click)="runJob.emit()"`,
    `      [disabled]="runDisabled"`,
    `      class="jc-run-btn"`,
    `      [style.borderColor]="runBtnBorderColor"`,
    `      [style.color]="runBtnTextColor">`,
    `      <lucide-icon *ngIf="!isRunning" [img]="PlayIcon" size="16"></lucide-icon>`,
    `      <lucide-icon *ngIf="isRunning" [img]="RotateCcwIcon" size="16" class="spin-anim"></lucide-icon>`,
    `      {{ isRunning ? runningLabel : runLabel }}`,
    `    </button>`,
    `    <ng-content></ng-content>`,
    `  </div>`,
    `</div>`,
    ``
  ].join(EOL);
  const jobCardCss = [
    `:host { display: flex; }`,
    `.jc-card.jc-card { background: #fbfcfb; border: 1px solid var(--border); border-radius: 14px; padding: 20px; display: flex; flex-direction: column; justify-content: space-between; width: 100%; }`,
    `.jc-header.jc-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }`,
    `.jc-title.jc-title { font-size: 16px; display: flex; align-items: center; gap: 8px; }`,
    `.jc-desc.jc-desc { font-size: 13px; color: var(--muted-foreground); margin: 0 0 14px; line-height: 1.5; }`,
    `.jc-schedule-box.jc-schedule-box { font-size: 12px; background: white; border: 1px solid var(--border); border-radius: 8px; padding: 10px 14px; margin-bottom: 16px; }`,
    `.jc-schedule-row.jc-schedule-row { margin-bottom: 6px; }`,
    `.jc-toggle-box.jc-toggle-box { display: flex; align-items: center; justify-content: space-between; padding: 10px 14px; border: 1px solid #bbf7d0; border-radius: 8px; margin-bottom: 16px; }`,
    `.jc-toggle-title.jc-toggle-title { font-size: 13px; font-weight: 600; }`,
    `.jc-toggle-sub.jc-toggle-sub { font-size: 11px; color: var(--muted-foreground); }`,
    `.jc-toggle-btn.jc-toggle-btn { font-size: 12px; padding: 4px 10px; border-radius: 6px; }`,
    `.jc-footer.jc-footer { display: flex; flex-direction: column; gap: 8px; }`,
    `.jc-progress.jc-progress { margin-bottom: 4px; }`,
    `.jc-run-btn.jc-run-btn { width: 100%; justify-content: center; gap: 8px; }`,
    ``
  ].join(EOL);
  const jobCardTs = [
    `import { Component, EventEmitter, Input, Output } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { LucideAngularModule, Clock, Play, RotateCcw } from 'lucide-angular';`,
    ``,
    `@Component({`,
    `  selector: 'app-job-card',`,
    `  standalone: true,`,
    `  imports: [CommonModule, LucideAngularModule],`,
    `  templateUrl: './job-card.component.html',`,
    `  styleUrl: './job-card.component.css'`,
    `})`,
    `export class JobCardComponent {`,
    `  readonly PlayIcon = Play;`,
    `  readonly RotateCcwIcon = RotateCcw;`,
    `  @Input() icon: any = Clock;`,
    `  @Input() title = '';`,
    `  @Input() description = '';`,
    `  @Input() scheduleText = '';`,
    `  @Input() lastRunText = '';`,
    `  @Input() accentColor = 'var(--primary)';`,
    `  @Input() enabledBg = '#f0fdf4';`,
    `  @Input() enabledBorder = '#bbf7d0';`,
    `  @Input() enabledTextColor = '#166534';`,
    `  @Input() enabledBtnColor = '#16a34a';`,
    `  @Input() enabledSubText = '';`,
    `  @Input() isEnabled = true;`,
    `  @Input() isRunning = false;`,
    `  @Input() isToggling = false;`,
    `  @Input() showSavingText = false;`,
    `  @Input() runDisabled = false;`,
    `  @Input() runBtnClass = 'btn btn-primary';`,
    `  @Input() runBtnBorderColor: string | null = null;`,
    `  @Input() runBtnTextColor: string | null = null;`,
    `  @Input() runLabel = '';`,
    `  @Input() runningLabel = '';`,
    `  @Output() toggleJob = new EventEmitter<void>();`,
    `  @Output() runJob = new EventEmitter<void>();`,
    `}`,
    ``
  ].join(EOL);
  write('components/job-card/job-card.component.html', jobCardHtml);
  write('components/job-card/job-card.component.css', jobCardCss);
  write('components/job-card/job-card.component.ts', jobCardTs);
  write('components/job-card/job-card.component.spec.ts', makeSpec('JobCardComponent', 'job-card.component'));
}

// 3.3 FundsSyncCardComponent (lines 191-315)
writeAdminComp(
  'funds-sync-card',
  'app-funds-sync-card',
  'FundsSyncCardComponent',
  htmlLines.slice(190, 315).join(EOL),
  ['fundsSync', 'historical', 'compare'],
  ['DatabaseIcon', 'PlayIcon', 'RotateCcwIcon', 'HistoryIcon', 'SearchIcon', 'LineChartIcon'],
  [],
  '  // Host flex styling in CSS',
  true
);
// Add :host { display: flex; } and width: 100% on root card of funds-sync-card
{
  const p = path.join(ROOT, 'components/funds-sync-card/funds-sync-card.component.css');
  fs.writeFileSync(p, `:host { display: flex; }${EOL}.ac1.ac1 { width: 100%; }${EOL}` + fs.readFileSync(p, 'utf8'), 'utf8');
}

// 3.4 ScrapeProgressComponent (lines 323-390)
writeAdminComp(
  'scrape-progress',
  'app-scrape-progress',
  'ScrapeProgressComponent',
  htmlLines.slice(322, 390).join(EOL),
  ['jobs'],
  []
);

// 3.5 SectorDailyPanelComponent (lines 393-483)
writeAdminComp(
  'sector-daily-panel',
  'app-sector-daily-panel',
  'SectorDailyPanelComponent',
  htmlLines.slice(392, 483).join(EOL),
  ['sectorDaily'],
  ['LayersIcon', 'RotateCcwIcon']
);

// 3.6 LastRunTableComponent (lines 486-518)
writeAdminComp(
  'last-run-table',
  'app-last-run-table',
  'LastRunTableComponent',
  htmlLines.slice(485, 518).join(EOL),
  ['jobs'],
  ['HistoryIcon']
);

// 3.7 HistoricalSyncModalComponent (lines 523-672)
writeAdminComp(
  'historical-sync-modal',
  'app-historical-sync-modal',
  'HistoricalSyncModalComponent',
  htmlLines.slice(522, 672).join(EOL),
  ['historical', 'fundsSync', 'compare'],
  ['HistoryIcon', 'XIcon', 'RotateCcwIcon', 'SearchIcon', 'CheckCircleIcon', 'AlertTriangleIcon']
);

// 3.8 FundCompareChartModalComponent (lines 677-786)
writeAdminComp(
  'fund-compare-chart-modal',
  'app-fund-compare-chart-modal',
  'FundCompareChartModalComponent',
  htmlLines.slice(676, 786).join(EOL),
  ['compare', 'fundsSync'],
  ['LineChartIcon', 'XIcon', 'HistoryIcon']
);

// 3.9 MarketDataFormComponent (lines 892-1176)
writeAdminComp(
  'market-data-form',
  'app-market-data-form',
  'MarketDataFormComponent',
  htmlLines.slice(891, 1176).join(EOL),
  ['marketData'],
  ['SaveIcon', 'RotateCcwIcon']
);

// 3.10 ShariahOverridesPanelComponent (lines 1284-1485)
writeAdminComp(
  'shariah-overrides-panel',
  'app-shariah-overrides-panel',
  'ShariahOverridesPanelComponent',
  htmlLines.slice(1283, 1485).join(EOL) + EOL + `            </div>` + EOL + `          </div>`,
  ['overrides'],
  ['Edit3Icon', 'SaveIcon', 'RotateCcwIcon']
);

// 3.11 BourseHalalStandardsTableComponent (lines 1488-1553)
writeAdminComp(
  'bourse-halal-standards-table',
  'app-bourse-halal-standards-table',
  'BourseHalalStandardsTableComponent',
  `<div *ngIf="shariahOverrides">` + EOL + htmlLines.slice(1487, 1553).join(EOL) + EOL + `</div>`,
  ['overrides'],
  ['CheckCircleIcon']
);

// 3.12 SourcePdfStatusCardComponent (lines 1694-1728)
writeAdminComp(
  'source-pdf-status-card',
  'app-source-pdf-status-card',
  'SourcePdfStatusCardComponent',
  htmlLines.slice(1693, 1728).join(EOL),
  ['pdf'],
  []
);

// 3.13 PdfUploadFormComponent (lines 1731-1799)
writeAdminComp(
  'pdf-upload-form',
  'app-pdf-upload-form',
  'PdfUploadFormComponent',
  htmlLines.slice(1730, 1799).join(EOL),
  ['pdf'],
  ['UploadFileIcon']
);

// 3.14 CreateStockFormComponent (lines 1923-1950)
writeAdminComp(
  'create-stock-form',
  'app-create-stock-form',
  'CreateStockFormComponent',
  htmlLines.slice(1922, 1950).join(EOL),
  ['mgmt'],
  []
);

// 3.15 StocksManagementTableComponent (lines 1953-2014)
writeAdminComp(
  'stocks-management-table',
  'app-stocks-management-table',
  'StocksManagementTableComponent',
  htmlLines.slice(1952, 2014).join(EOL),
  ['mgmt'],
  []
);

console.log('Generated 15 admin child & shared components');

// =========================================================================
// 4. Generate the 8 Routed Tab Components (Pages)
// =========================================================================

// 4.1 AdminScrapingPageComponent (path: 'market-data', tab: 'scraping')
{
  const html = [
    `<div class="admin-card">`,
    `  <h2>مهام السحب الآلي لبيانات السوق</h2>`,
    `  <p class="muted">`,
    `    نظام السحب مقسّم إلى حزمتين: حزمة الأسعار اليومية الحية (Live Daily) عند الساعة 4:00 عصراً، وحزمة البيانات الربع سنوية بطيئة التغير (Quarterly Fundamentals) في بداية كل ربع سنوي.`,
    `  </p>`,
    ``,
    `  <div class="asp-grid">`,
    `    <!-- Bucket 1: Daily Live-Price Scraper -->`,
    `    <app-job-card`,
    `      [icon]="ClockIcon"`,
    `      title="سحب الأسعار المباشرة (Daily Live-Price)"`,
    `      description="يسحب أسعار الإغلاق، الافتتاح، الأدنى والأعلى، ومستويات الدعم والمقاومة لجميع الأسهم."`,
    `      scheduleText="يومياً في تمام 4:00 عصراً بتوقيت القاهرة"`,
    `      [lastRunText]="jobs.getBucketLastRun('LiveDaily')"`,
    `      accentColor="var(--primary)"`,
    `      enabledBg="#f0fdf4"`,
    `      enabledBorder="#bbf7d0"`,
    `      enabledTextColor="#166534"`,
    `      enabledBtnColor="#16a34a"`,
    `      enabledSubText="يعمل مجدولاً في الساعة 4:00 عصراً"`,
    `      [isEnabled]="jobs.isJobEnabled('LiveDaily')"`,
    `      [isRunning]="jobs.isDailyRunning"`,
    `      [isToggling]="jobs.togglingJobKey === 'LiveDaily'"`,
    `      [runDisabled]="jobs.isScrapingRunning"`,
    `      runBtnClass="btn btn-primary"`,
    `      runLabel="تشغيل السحب اليومي الآن (Run Daily)"`,
    `      runningLabel="جارٍ السحب اليومي..."`,
    `      (toggleJob)="jobs.toggleJobSetting('LiveDaily')"`,
    `      (runJob)="jobs.triggerDailyScrape()">`,
    `    </app-job-card>`,
    ``,
    `    <!-- Bucket 2: Quarterly Fundamentals Scraper -->`,
    `    <app-job-card`,
    `      [icon]="CalendarIcon"`,
    `      title="سحب الأساسيات الربع سنوية (Quarterly Slow)"`,
    `      description="يسحب مكررات الربحية P/E، مضاعف القيمة الدفترية P/B، ربحية السهم EPS، القيمة الدفترية، الاسمية، والسوقية."`,
    `      scheduleText="أول يوم في (يناير/أبريل/يوليو/أكتوبر) 9:00 صباحاً"`,
    `      [lastRunText]="jobs.getBucketLastRun('SlowQuarterly')"`,
    `      accentColor="#1e40af"`,
    `      enabledBg="#eff6ff"`,
    `      enabledBorder="#bfdbfe"`,
    `      enabledTextColor="#1e40af"`,
    `      enabledBtnColor="#2563eb"`,
    `      enabledSubText="يعمل مجدولاً ربع سنوياً"`,
    `      [isEnabled]="jobs.isJobEnabled('SlowQuarterly')"`,
    `      [isRunning]="jobs.isQuarterlyRunning"`,
    `      [isToggling]="jobs.togglingJobKey === 'SlowQuarterly'"`,
    `      [runDisabled]="jobs.isScrapingRunning"`,
    `      runBtnClass="btn btn-outline"`,
    `      runBtnBorderColor="#93c5fd"`,
    `      runBtnTextColor="#1d4ed8"`,
    `      runLabel="تشغيل السحب الربع سنوي الآن (Run Quarterly)"`,
    `      runningLabel="جارٍ السحب الربع سنوي..."`,
    `      (toggleJob)="jobs.toggleJobSetting('SlowQuarterly')"`,
    `      (runJob)="jobs.triggerQuarterlyScrape()">`,
    `    </app-job-card>`,
    ``,
    `    <!-- Bucket 3: Market Snapshots Scraper -->`,
    `    <app-job-card`,
    `      [icon]="ClockIcon"`,
    `      title="لقطات السوق والمؤشرات (Market Snapshots)"`,
    `      description="تحديث أسعار المؤشرات والقطاعات من مباشر يومياً بعد إغلاق جلسة التداول."`,
    `      scheduleText="يومياً في تمام 4:00 عصراً بتوقيت القاهرة (الأحد–الخميس)"`,
    `      [lastRunText]="jobs.snapshotsLastRunText"`,
    `      accentColor="#047857"`,
    `      enabledBg="#ecfdf5"`,
    `      enabledBorder="#a7f3d0"`,
    `      enabledTextColor="#065f46"`,
    `      enabledBtnColor="#059669"`,
    `      enabledSubText="يعمل مجدولاً في الساعة 4:00 عصراً"`,
    `      [isEnabled]="jobs.isJobEnabled('MarketSnapshots')"`,
    `      [isRunning]="jobs.isMarketSnapshotsRunning"`,
    `      [isToggling]="jobs.togglingJobKey === 'MarketSnapshots'"`,
    `      [showSavingText]="true"`,
    `      [runDisabled]="jobs.isMarketSnapshotsRunning"`,
    `      runBtnClass="btn btn-outline"`,
    `      runBtnBorderColor="#a7f3d0"`,
    `      runBtnTextColor="#047857"`,
    `      runLabel="تشغيل يدوي الآن (Run Snapshots)"`,
    `      runningLabel="جارٍ تحديث اللقطات..."`,
    `      (toggleJob)="jobs.toggleJobSetting('MarketSnapshots')"`,
    `      (runJob)="jobs.triggerSnapshotsRefresh()">`,
    `      <div *ngIf="jobs.snapshotsRunResult" class="asp-snap-res" [style.color]="jobs.snapshotsRunResult.failed > 0 ? '#b45309' : '#047857'">`,
    `        اكتمل: نجح {{ jobs.snapshotsRunResult.succeeded }} من {{ jobs.snapshotsRunResult.totalTargets }}`,
    `      </div>`,
    `      <div *ngIf="jobs.snapshotsRunError" class="asp-snap-err">`,
    `        {{ jobs.snapshotsRunError }}`,
    `      </div>`,
    `    </app-job-card>`,
    ``,
    `    <!-- Bucket 4: Funds Sync -->`,
    `    <app-funds-sync-card></app-funds-sync-card>`,
    `  </div>`,
    ``,
    `  <div *ngIf="jobs.jobToggleError" class="admin-result error asp-toggle-err">`,
    `    {{ jobs.jobToggleError }}`,
    `  </div>`,
    ``,
    `  <!-- Live Jobs Progress View -->`,
    `  <app-scrape-progress></app-scrape-progress>`,
    ``,
    `  <!-- Daily Sector Changes Calculation Panel -->`,
    `  <app-sector-daily-panel></app-sector-daily-panel>`,
    ``,
    `  <!-- Last Run Summary -->`,
    `  <app-last-run-table></app-last-run-table>`,
    ``,
    `  <!-- Historical Sync & Comparison Chart Modals -->`,
    `  <app-historical-sync-modal></app-historical-sync-modal>`,
    `  <app-fund-compare-chart-modal></app-fund-compare-chart-modal>`,
    `</div>`,
    ``
  ].join(EOL);
  const css = [
    `.asp-grid.asp-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 20px; margin: 24px 0; }`,
    `.asp-snap-res.asp-snap-res { font-size: 11px; text-align: center; margin-top: 4px; }`,
    `.asp-snap-err.asp-snap-err { font-size: 11px; text-align: center; color: var(--bad); margin-top: 4px; }`,
    `.asp-toggle-err.asp-toggle-err { margin-top: 12px; margin-bottom: 12px; }`,
    ``
  ].join(EOL);
  const tsCode = [
    `import { Component, OnInit, OnDestroy } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { Clock, Calendar } from 'lucide-angular';`,
    `import { AdminScrapingJobsService } from '../../services/admin-scraping-jobs.service';`,
    `import { AdminFundsSyncService } from '../../services/admin-funds-sync.service';`,
    `import { JobCardComponent } from '../../components/job-card/job-card.component';`,
    `import { FundsSyncCardComponent } from '../../components/funds-sync-card/funds-sync-card.component';`,
    `import { ScrapeProgressComponent } from '../../components/scrape-progress/scrape-progress.component';`,
    `import { SectorDailyPanelComponent } from '../../components/sector-daily-panel/sector-daily-panel.component';`,
    `import { LastRunTableComponent } from '../../components/last-run-table/last-run-table.component';`,
    `import { HistoricalSyncModalComponent } from '../../components/historical-sync-modal/historical-sync-modal.component';`,
    `import { FundCompareChartModalComponent } from '../../components/fund-compare-chart-modal/fund-compare-chart-modal.component';`,
    ``,
    `@Component({`,
    `  selector: 'app-admin-scraping-page',`,
    `  standalone: true,`,
    `  imports: [`,
    `    CommonModule,`,
    `    JobCardComponent,`,
    `    FundsSyncCardComponent,`,
    `    ScrapeProgressComponent,`,
    `    SectorDailyPanelComponent,`,
    `    LastRunTableComponent,`,
    `    HistoricalSyncModalComponent,`,
    `    FundCompareChartModalComponent`,
    `  ],`,
    `  templateUrl: './admin-scraping-page.component.html',`,
    `  styleUrl: './admin-scraping-page.component.css'`,
    `})`,
    `export class AdminScrapingPageComponent implements OnInit, OnDestroy {`,
    `  readonly ClockIcon = Clock;`,
    `  readonly CalendarIcon = Calendar;`,
    `  private pollInterval?: any;`,
    ``,
    `  constructor(`,
    `    public jobs: AdminScrapingJobsService,`,
    `    public fundsSync: AdminFundsSyncService`,
    `  ) {}`,
    ``,
    `  ngOnInit(): void {`,
    `    this.jobs.fetchScrapeStatus();`,
    `    this.fundsSync.fetchFundSyncStatus();`,
    `    this.jobs.loadJobSettings();`,
    `    this.fundsSync.loadFundSyncSettings();`,
    `    this.fundsSync.loadFundSyncHistory();`,
    `    this.fundsSync.loadAdminFundPickerList();`,
    `    this.pollInterval = setInterval(() => {`,
    `      this.jobs.fetchScrapeStatus();`,
    `      this.fundsSync.fetchFundSyncStatus();`,
    `    }, 2000);`,
    `  }`,
    ``,
    `  ngOnDestroy(): void {`,
    `    if (this.pollInterval) clearInterval(this.pollInterval);`,
    `  }`,
    `}`,
    ``
  ].join(EOL);
  write('pages/admin-scraping-page/admin-scraping-page.component.html', html);
  write('pages/admin-scraping-page/admin-scraping-page.component.css', css);
  write('pages/admin-scraping-page/admin-scraping-page.component.ts', tsCode);
  write('pages/admin-scraping-page/admin-scraping-page.component.spec.ts', makeSpec('AdminScrapingPageComponent', 'admin-scraping-page.component'));
}

// 4.2 AdminMarketDataPageComponent (path: 'manual-edit', tab: 'market-data')
{
  const html = [
    `<div class="admin-card">`,
    `  <h2>تعديل بيانات السوق والتقييم للأسهم يدوياً</h2>`,
    `  <p class="muted">`,
    `    ابحث عن السهم وحدده لتعديل أسعار السوق ومؤشرات التقييم الأساسية. يتم حفظ كامل الحقول مع خيار إعادة حساب مكررات الربحية P/E ومضاعف القيمة الدفترية P/B تلقائياً.`,
    `  </p>`,
    ``,
    `  <app-stock-picker`,
    `    [filteredStocks]="marketData.filteredStocks"`,
    `    [(searchQuery)]="marketData.stockSearchQuery"`,
    `    [(selectedTicker)]="marketData.selectedTicker"`,
    `    (searchInput)="marketData.onSearchInput()"`,
    `    (stockSelected)="marketData.onStockSelected()"`,
    `    (directSelect)="marketData.selectStockDirectly($event)">`,
    `  </app-stock-picker>`,
    ``,
    `  <div *ngIf="marketData.loadingStock" class="amd-loading">`,
    `    جارٍ تحميل بيانات السهم...`,
    `  </div>`,
    ``,
    `  <div *ngIf="marketData.selectedStockMarketData && !marketData.loadingStock" class="amd-content">`,
    `    <app-stock-header-banner [stockMarketData]="marketData.selectedStockMarketData"></app-stock-header-banner>`,
    `    <app-market-data-form></app-market-data-form>`,
    `  </div>`,
    `</div>`,
    ``
  ].join(EOL);
  const css = [
    `.amd-loading.amd-loading { padding: 30px; text-align: center; color: var(--muted-foreground); }`,
    `.amd-content.amd-content { margin-top: 24px; }`,
    ``
  ].join(EOL);
  const tsCode = [
    `import { Component, OnInit } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { AdminMarketDataService } from '../../services/admin-market-data.service';`,
    `import { AdminStockLookupService } from '../../services/admin-stock-lookup.service';`,
    `import { StockPickerComponent } from '../../../../shared/components/stock-picker/stock-picker.component';`,
    `import { StockHeaderBannerComponent } from '../../../../shared/components/stock-header-banner/stock-header-banner.component';`,
    `import { MarketDataFormComponent } from '../../components/market-data-form/market-data-form.component';`,
    ``,
    `@Component({`,
    `  selector: 'app-admin-market-data-page',`,
    `  standalone: true,`,
    `  imports: [`,
    `    CommonModule,`,
    `    StockPickerComponent,`,
    `    StockHeaderBannerComponent,`,
    `    MarketDataFormComponent`,
    `  ],`,
    `  templateUrl: './admin-market-data-page.component.html',`,
    `  styleUrl: './admin-market-data-page.component.css'`,
    `})`,
    `export class AdminMarketDataPageComponent implements OnInit {`,
    `  constructor(`,
    `    public marketData: AdminMarketDataService,`,
    `    public lookup: AdminStockLookupService`,
    `  ) {}`,
    ``,
    `  ngOnInit(): void {`,
    `    this.lookup.loadStocks();`,
    `  }`,
    `}`,
    ``
  ].join(EOL);
  write('pages/admin-market-data-page/admin-market-data-page.component.html', html);
  write('pages/admin-market-data-page/admin-market-data-page.component.css', css);
  write('pages/admin-market-data-page/admin-market-data-page.component.ts', tsCode);
  write('pages/admin-market-data-page/admin-market-data-page.component.spec.ts', makeSpec('AdminMarketDataPageComponent', 'admin-market-data-page.component'));
}

// 4.3 AdminShariahEditPageComponent (path: 'shariah-edit', tab: 'shariah-edit')
{
  const html = [
    `<div class="admin-card">`,
    `  <h2>تعديل البيانات الشرعية للأسهم يدوياً</h2>`,
    `  <p class="muted">`,
    `    ابحث عن السهم وحدده لتعديل البيانات الشرعية يدوياً (تصنيف النشاط، مطابقة النشاط الأساسي، نسبة الإيراد المحرم، نسبة القروض والفوائد).`,
    `    القيم المدخلة هنا تطبق على "بورصة حلال" فقط (EGX 33، DFM، AAOIFI، S&P، KLSI) وتجاوز قيم التغذية.`,
    `  </p>`,
    ``,
    `  <app-stock-picker`,
    `    [filteredStocks]="overrides.filteredShariahStocks"`,
    `    [(searchQuery)]="overrides.shariahStockSearchQuery"`,
    `    [(selectedTicker)]="overrides.shariahSelectedTicker"`,
    `    (searchInput)="overrides.onShariahSearchInput()"`,
    `    (stockSelected)="overrides.onShariahStockSelected()"`,
    `    (directSelect)="overrides.selectShariahStockDirectly($event)">`,
    `  </app-stock-picker>`,
    ``,
    `  <div *ngIf="overrides.shariahLoadingStock" class="ase-loading">`,
    `    جارٍ تحميل بيانات السهم...`,
    `  </div>`,
    ``,
    `  <div *ngIf="overrides.shariahSelectedStockMarketData && !overrides.shariahLoadingStock" class="ase-content">`,
    `    <app-stock-header-banner [stockMarketData]="overrides.shariahSelectedStockMarketData"></app-stock-header-banner>`,
    `    <app-shariah-overrides-panel></app-shariah-overrides-panel>`,
    `    <app-bourse-halal-standards-table></app-bourse-halal-standards-table>`,
    `  </div>`,
    `</div>`,
    ``
  ].join(EOL);
  const css = [
    `.ase-loading.ase-loading { padding: 30px; text-align: center; color: var(--muted-foreground); }`,
    `.ase-content.ase-content { margin-top: 24px; }`,
    ``
  ].join(EOL);
  const tsCode = [
    `import { Component, OnInit } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { AdminShariahOverridesService } from '../../services/admin-shariah-overrides.service';`,
    `import { AdminStockLookupService } from '../../services/admin-stock-lookup.service';`,
    `import { StockPickerComponent } from '../../../../shared/components/stock-picker/stock-picker.component';`,
    `import { StockHeaderBannerComponent } from '../../../../shared/components/stock-header-banner/stock-header-banner.component';`,
    `import { ShariahOverridesPanelComponent } from '../../components/shariah-overrides-panel/shariah-overrides-panel.component';`,
    `import { BourseHalalStandardsTableComponent } from '../../components/bourse-halal-standards-table/bourse-halal-standards-table.component';`,
    ``,
    `@Component({`,
    `  selector: 'app-admin-shariah-edit-page',`,
    `  standalone: true,`,
    `  imports: [`,
    `    CommonModule,`,
    `    StockPickerComponent,`,
    `    StockHeaderBannerComponent,`,
    `    ShariahOverridesPanelComponent,`,
    `    BourseHalalStandardsTableComponent`,
    `  ],`,
    `  templateUrl: './admin-shariah-edit-page.component.html',`,
    `  styleUrl: './admin-shariah-edit-page.component.css'`,
    `})`,
    `export class AdminShariahEditPageComponent implements OnInit {`,
    `  constructor(`,
    `    public overrides: AdminShariahOverridesService,`,
    `    public lookup: AdminStockLookupService`,
    `  ) {}`,
    ``,
    `  ngOnInit(): void {`,
    `    this.lookup.loadStocks();`,
    `  }`,
    `}`,
    ``
  ].join(EOL);
  write('pages/admin-shariah-edit-page/admin-shariah-edit-page.component.html', html);
  write('pages/admin-shariah-edit-page/admin-shariah-edit-page.component.css', css);
  write('pages/admin-shariah-edit-page/admin-shariah-edit-page.component.ts', tsCode);
  write('pages/admin-shariah-edit-page/admin-shariah-edit-page.component.spec.ts', makeSpec('AdminShariahEditPageComponent', 'admin-shariah-edit-page.component'));
}

// 4.4 AdminIndexUploadPageComponent (path: 'index-files', tab: 'upload')
{
  const rawHtml = [
    `<div class="admin-card">`,
    htmlLines.slice(1560, 1626).join(EOL),
    `</div>`,
    ``
  ].join(EOL);
  writeAdminComp(
    'admin-index-upload-page',
    'app-admin-index-upload-page',
    'AdminIndexUploadPageComponent',
    rawHtml,
    ['indexUpload'],
    ['UploadIcon'],
    [],
    `  ngOnInit(): void { this.indexUpload.loadIndices(); }`,
    false,
    'pages/'
  );
}

// 4.5 AdminShariahSeedPageComponent (path: 'shariah-seed', tab: 'shariah')
{
  const rawHtml = [
    `<div class="admin-card">`,
    htmlLines.slice(1630, 1682).join(EOL),
    `</div>`,
    ``
  ].join(EOL);
  writeAdminComp(
    'admin-shariah-seed-page',
    'app-admin-shariah-seed-page',
    'AdminShariahSeedPageComponent',
    rawHtml,
    ['seed'],
    ['RefreshCwIcon', 'DatabaseIcon', 'CheckCircleIcon'],
    [],
    '',
    false,
    'pages/'
  );
}

// 4.6 AdminPdfReportsPageComponent (path: 'shariah-reports', tab: 'pdf-upload')
{
  const html = [
    `<div class="admin-card">`,
    `  <h2>رفع تقارير فيصل / أسطول (PDF)</h2>`,
    `  <p class="muted">`,
    `    رفع ملف PDF جديد لأحد المصدرين (بنك فيصل الإسلامي / أسطول) واستبدال الملف المخزن مسبقاً لهذا المصدر.`,
    `    بعد الرفع، شغّل "تحديث الشريعة المدمجة" (import-faisal-osoul) لتحديث روابط PDF في آراء الأسهم.`,
    `  </p>`,
    `  <app-source-pdf-status-card></app-source-pdf-status-card>`,
    `  <app-pdf-upload-form></app-pdf-upload-form>`,
    `</div>`,
    ``
  ].join(EOL);
  const tsCode = [
    `import { Component, OnInit } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { AdminSourcePdfService } from '../../services/admin-source-pdf.service';`,
    `import { SourcePdfStatusCardComponent } from '../../components/source-pdf-status-card/source-pdf-status-card.component';`,
    `import { PdfUploadFormComponent } from '../../components/pdf-upload-form/pdf-upload-form.component';`,
    ``,
    `@Component({`,
    `  selector: 'app-admin-pdf-reports-page',`,
    `  standalone: true,`,
    `  imports: [CommonModule, SourcePdfStatusCardComponent, PdfUploadFormComponent],`,
    `  templateUrl: './admin-pdf-reports-page.component.html',`,
    `  styleUrl: './admin-pdf-reports-page.component.css'`,
    `})`,
    `export class AdminPdfReportsPageComponent implements OnInit {`,
    `  constructor(public pdf: AdminSourcePdfService) {}`,
    `  ngOnInit(): void { this.pdf.loadSourcePdfStatus(); }`,
    `}`,
    ``
  ].join(EOL);
  write('pages/admin-pdf-reports-page/admin-pdf-reports-page.component.html', html);
  write('pages/admin-pdf-reports-page/admin-pdf-reports-page.component.css', '');
  write('pages/admin-pdf-reports-page/admin-pdf-reports-page.component.ts', tsCode);
  write('pages/admin-pdf-reports-page/admin-pdf-reports-page.component.spec.ts', makeSpec('AdminPdfReportsPageComponent', 'admin-pdf-reports-page.component'));
}

// 4.7 AdminReviewChecklistPageComponent (path: 'review-queue', tab: 'review')
{
  const rawHtml = [
    `<div class="admin-card">`,
    htmlLines.slice(1803, 1888).join(EOL),
    `</div>`,
    ``
  ].join(EOL);
  writeAdminComp(
    'admin-review-checklist-page',
    'app-admin-review-checklist-page',
    'AdminReviewChecklistPageComponent',
    rawHtml,
    ['review'],
    ['RefreshCwIcon', 'PlayIcon', 'AlertTriangleIcon', 'RotateCcwIcon'],
    [],
    `  ngOnInit(): void { this.review.loadRemovalCandidates(); }`,
    false,
    'pages/'
  );
}

// 4.8 AdminStocksMgmtPageComponent (path: 'stocks', tab: 'stocks')
{
  const html = [
    `<div class="admin-card asm-card">`,
    `  <div class="asm-heading-box">`,
    `    <h2 class="asm-heading-title">إدارة الأسهم والقطاعات والمؤشرات</h2>`,
    `    <p class="muted asm-heading-desc">ابحث عن سهم لتعيينه لقطاع أو مؤشر أو لإنشاء سهم جديد. التعيين اليدوي لا يُستبدل بالرفع التلقائي.</p>`,
    `  </div>`,
    `  <div class="asm-search-row">`,
    `    <div class="asm-search-wrap">`,
    `      <label class="asm-search-label">بحث بالرمز أو الاسم</label>`,
    `      <div class="asm-search-input-box">`,
    `        <lucide-icon [img]="SearchIcon" size="15" class="asm-search-icon"></lucide-icon>`,
    `        <input type="text" class="form-input asm-search-input"`,
    `               [(ngModel)]="mgmt.mgmtSearch" (ngModelChange)="mgmt.onMgmtSearchChange($event)"`,
    `               placeholder="مثال: COMI أو التجاري الدولي" />`,
    `      </div>`,
    `    </div>`,
    `    <button class="btn btn-primary" (click)="mgmt.showCreateStockModal = true">+ سهم جديد</button>`,
    `    <button class="btn btn-outline" (click)="mgmt.loadMgmtData()" [disabled]="mgmt.mgmtLoading">`,
    `      <lucide-icon [img]="RefreshCwIcon" size="15"></lucide-icon>`,
    `      تحديث`,
    `    </button>`,
    `  </div>`,
    `  <div *ngIf="mgmt.mgmtMessage" class="asm-msg"`,
    `       [style.background]="mgmt.mgmtError ? '#fff5f5' : '#f0fdf4'"`,
    `       [style.border]="mgmt.mgmtError ? '1px solid #fecaca' : '1px solid #bbf7d0'"`,
    `       [style.color]="mgmt.mgmtError ? 'var(--bad)' : 'var(--good)'">{{ mgmt.mgmtMessage }}</div>`,
    `  <app-create-stock-form></app-create-stock-form>`,
    `  <app-stocks-management-table></app-stocks-management-table>`,
    `</div>`,
    ``
  ].join(EOL);
  const css = [
    `.asm-card.asm-card { padding: 0; }`,
    `.asm-heading-box.asm-heading-box { padding: 24px 28px 16px; }`,
    `.asm-heading-title.asm-heading-title { margin: 0 0 6px; }`,
    `.asm-heading-desc.asm-heading-desc { margin: 0; }`,
    `.asm-search-row.asm-search-row { padding: 0 28px 20px; display: flex; gap: 10px; flex-wrap: wrap; align-items: flex-end; }`,
    `.asm-search-wrap.asm-search-wrap { flex: 1; min-width: 220px; }`,
    `.asm-search-label.asm-search-label { font-size: 12px; font-weight: 600; color: var(--muted-foreground); display: block; margin-bottom: 4px; }`,
    `.asm-search-input-box.asm-search-input-box { position: relative; }`,
    `.asm-search-icon.asm-search-icon { position: absolute; right: 10px; top: 50%; transform: translateY(-50%); color: var(--muted-foreground); }`,
    `.asm-search-input.asm-search-input { padding-right: 32px; width: 100%; box-sizing: border-box; }`,
    `.asm-msg.asm-msg { margin: 0 28px 16px; padding: 12px 16px; border-radius: 10px; font-size: 13px; }`,
    ``
  ].join(EOL);
  const tsCode = [
    `import { Component, OnInit } from '@angular/core';`,
    `import { CommonModule } from '@angular/common';`,
    `import { FormsModule } from '@angular/forms';`,
    `import { LucideAngularModule, Search, RefreshCw } from 'lucide-angular';`,
    `import { AdminStocksManagementService } from '../../services/admin-stocks-management.service';`,
    `import { CreateStockFormComponent } from '../../components/create-stock-form/create-stock-form.component';`,
    `import { StocksManagementTableComponent } from '../../components/stocks-management-table/stocks-management-table.component';`,
    ``,
    `@Component({`,
    `  selector: 'app-admin-stocks-mgmt-page',`,
    `  standalone: true,`,
    `  imports: [CommonModule, FormsModule, LucideAngularModule, CreateStockFormComponent, StocksManagementTableComponent],`,
    `  templateUrl: './admin-stocks-mgmt-page.component.html',`,
    `  styleUrl: './admin-stocks-mgmt-page.component.css'`,
    `})`,
    `export class AdminStocksMgmtPageComponent implements OnInit {`,
    `  readonly SearchIcon = Search;`,
    `  readonly RefreshCwIcon = RefreshCw;`,
    `  constructor(public mgmt: AdminStocksManagementService) {}`,
    `  ngOnInit(): void { this.mgmt.loadMgmtData(); }`,
    `}`,
    ``
  ].join(EOL);
  write('pages/admin-stocks-mgmt-page/admin-stocks-mgmt-page.component.html', html);
  write('pages/admin-stocks-mgmt-page/admin-stocks-mgmt-page.component.css', css);
  write('pages/admin-stocks-mgmt-page/admin-stocks-mgmt-page.component.ts', tsCode);
  write('pages/admin-stocks-mgmt-page/admin-stocks-mgmt-page.component.spec.ts', makeSpec('AdminStocksMgmtPageComponent', 'admin-stocks-mgmt-page.component'));
}

console.log('Generated 8 admin routed pages');
