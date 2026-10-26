import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { navigateQueryParams } from '../../utils/navigation-utils';
import { IndexSummaryDto, StockListItemDto } from '../../models/api.models';
import { INDEX_ARABIC_NAMES } from '../../models/api.models';
import { matchesNonComplianceType, getNonComplianceReasons, normalizeStatusValue } from '../../models/shariah-standards';
import { MzListingPageComponent, type MzListingConfig } from '../../components/ui/ui-listing-page';
import { comparisonRibbon, fmtNumber, pctTone, signedPct, statusTag } from '../../components/ui/ui-mappers';
import type { MzListingCardData } from '../../components/ui/ui-listing';

@Component({
  selector: 'app-stocks-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MzListingPageComponent],
  template: `
    <mz-listing-page
      [config]="config"
      (tabChange)="onTabChange($event)"
      (searchChange)="onSearchInput($event)"
      (controlChange)="onControlChange($event)"
      (sortChange)="onSortSelect($event)"
      (advancedClick)="advancedOpen = !advancedOpen; rebuild()"
      (pageChange)="setPage($event)">

      <div resultsExtra class="mz-active-filters">
        <span class="mz-badge pill neutral" *ngFor="let code of indexCodes">المؤشر: {{ code }} <button type="button" class="mz-x" (click)="removeIndex(code)" aria-label="إزالة">×</button></span>
        <span class="mz-badge pill neutral" *ngFor="let st of shariahStatuses">الحكم: {{ getShariahLabel(st) }} <button type="button" class="mz-x" (click)="removeShariah(st)" aria-label="إزالة">×</button></span>
        <span class="mz-badge pill neutral" *ngIf="nonComplianceType">سبب عدم التوافق: {{ nonComplianceType === 'activity' ? 'النشاط' : 'النسب المالية' }} <button type="button" class="mz-x" (click)="clearFilter('nonComplianceType')" aria-label="إزالة">×</button></span>
        <span class="mz-badge pill neutral" *ngIf="priceComparison">التقييم: {{ getPriceComparisonLabel(priceComparison) }} <button type="button" class="mz-x" (click)="clearFilter('priceComparison')" aria-label="إزالة">×</button></span>
        <span class="mz-badge pill neutral" *ngIf="minCompliantSources">الجهات: ≥ {{ minCompliantSources }} <button type="button" class="mz-x" (click)="clearFilter('minCompliantSources')" aria-label="إزالة">×</button></span>
        <span class="mz-badge pill neutral" *ngIf="minPe !== null || maxPe !== null">مكرر الربحية: {{ minPe ?? 0 }} - {{ maxPe ?? '∞' }} <button type="button" class="mz-x" (click)="clearPeFilter()" aria-label="إزالة">×</button></span>
        <span class="mz-badge pill neutral" *ngIf="minPb !== null || maxPb !== null">مضاعف الدفترية: {{ minPb ?? 0 }} - {{ maxPb ?? '∞' }} <button type="button" class="mz-x" (click)="clearPbFilter()" aria-label="إزالة">×</button></span>
      </div>

      <div advanced class="mz-adv-grid">
        <select class="mz-select" *ngIf="shariahStatuses.includes('NonCompliant')" [(ngModel)]="nonComplianceType" (change)="onFilterChange()" aria-label="سبب عدم التوافق">
          <option value="">الكل (نشاط + نسب)</option>
          <option value="activity">النشاط الأساسي</option>
          <option value="ratios">النسب المالية</option>
        </select>
        <select class="mz-select" [(ngModel)]="minCompliantSources" (change)="onFilterChange()" aria-label="الحد الأدنى للمصادر المتوافقة">
          <option value="">الجهات المتوافقة: الكل</option>
          <option value="3">3 جهات فأكثر</option>
          <option value="5">5 جهات فأكثر</option>
          <option value="7">إجماع كامل (7 من 7)</option>
        </select>
        <div class="mz-slicer">
          <div class="mz-slicer-head"><span>مكرر الربحية (P/E)</span><span>{{ minPe !== null ? minPe : 0 }} - {{ maxPe !== null ? maxPe : 100 }}</span></div>
          <div class="mz-slicer-inputs">
            <input class="mz-select" type="number" placeholder="من" [ngModel]="minPe" (ngModelChange)="onPeMinChange($event)" min="0" max="100" aria-label="الحد الأدنى لمكرر الربحية" />
            <span>—</span>
            <input class="mz-select" type="number" placeholder="إلى" [ngModel]="maxPe" (ngModelChange)="onPeMaxChange($event)" min="0" max="100" aria-label="الحد الأقصى لمكرر الربحية" />
          </div>
          <input type="range" min="0" max="100" [ngModel]="maxPe !== null ? maxPe : 100" (ngModelChange)="onPeMaxChange($event)" aria-label="شريط أقصى مكرر ربحية" />
        </div>
        <div class="mz-slicer">
          <div class="mz-slicer-head"><span>مضاعف القيمة الدفترية (P/B)</span><span>{{ minPb !== null ? minPb : 0 }} - {{ maxPb !== null ? maxPb : 20 }}</span></div>
          <div class="mz-slicer-inputs">
            <input class="mz-select" type="number" step="0.5" placeholder="من" [ngModel]="minPb" (ngModelChange)="onPbMinChange($event)" min="0" max="20" aria-label="الحد الأدنى لمضاعف القيمة الدفترية" />
            <span>—</span>
            <input class="mz-select" type="number" step="0.5" placeholder="إلى" [ngModel]="maxPb" (ngModelChange)="onPbMaxChange($event)" min="0" max="20" aria-label="الحد الأقصى لمضاعف القيمة الدفترية" />
          </div>
          <input type="range" min="0" max="20" step="0.5" [ngModel]="maxPb !== null ? maxPb : 20" (ngModelChange)="onPbMaxChange($event)" aria-label="شريط أقصى مضاعف قيمة دفترية" />
        </div>
        <label class="mz-slicer">
          <span class="mz-slicer-head"><span>لكل صفحة</span></span>
          <select class="mz-select" [(ngModel)]="pageSize" (change)="onPageSizeChange()" aria-label="عدد الأسهم في الصفحة">
            <option [ngValue]="5">5</option><option [ngValue]="10">10</option><option [ngValue]="20">20</option>
            <option [ngValue]="50">50</option><option [ngValue]="100">100</option>
          </select>
        </label>
      </div>
    </mz-listing-page>
  `
})
export class StocksListComponent implements OnInit {
  readonly shariahOptions = [
    { value: 'Compliant', label: 'متوافق شرعاً' },
    { value: 'NonCompliant', label: 'غير متوافق' },
    { value: 'Pending', label: 'البيانات غير كافية' },
    { value: 'Blocked', label: 'محظور' }
  ];

  /** Config object consumed by the shared ListingPage; rebuilt whenever state changes. */
  config: MzListingConfig = {
    header: { badge: 'البورصة المصرية', title: 'الأسهم', description: '' },
    cards: [],
    empty: { title: '', message: '' }
  };
  advancedOpen = false;

  stocks: StockListItemDto[] = [];
  indices: IndexSummaryDto[] = [];
  totalCount = 0;
  totalPages = 1;
  loading = false;

  // Filter params
  page = 1;
  pageSize = 20; // Default 20
  search = '';
  indexCodes: string[] = [];
  shariahStatuses: string[] = [];
  priceComparison = '';
  minCompliantSources = '';
  sortBy = 'changePct';
  sortDir = 'desc';

  // PE & PB filters
  minPe: number | null = null;
  maxPe: number | null = null;
  minPb: number | null = null;
  maxPb: number | null = null;

  /** Client-side sub-filter for non-compliance type: '' | 'activity' | 'ratios' */
  nonComplianceType = '';

  private searchDebounceTimer?: any;
  private slicerDebounceTimer?: any;

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.rebuild();
  }

  ngOnInit(): void {
    this.api.getIndices().subscribe({
      next: (data) => { this.indices = data || []; this.rebuild(); }
    });

    this.route.queryParams.subscribe((params) => {
      this.page = params['page'] ? Number(params['page']) : 1;
      this.pageSize = params['pageSize'] ? Number(params['pageSize']) : 20;
      this.search = params['search'] || '';

      // Index codes: support comma-delimited or array or legacy single indexCode
      if (params['indexCodes']) {
        const val = params['indexCodes'];
        this.indexCodes = Array.isArray(val)
          ? val
          : String(val).split(',').map(s => s.trim()).filter(Boolean);
      } else if (params['indexCode']) {
        this.indexCodes = [String(params['indexCode']).trim()];
      } else {
        this.indexCodes = [];
      }

      // Shariah statuses: support comma-delimited or array or legacy single shariahStatus
      if (params['shariahStatuses']) {
        const val = params['shariahStatuses'];
        this.shariahStatuses = Array.isArray(val)
          ? val
          : String(val).split(',').map(s => s.trim()).filter(Boolean);
      } else if (params['shariahStatus']) {
        this.shariahStatuses = [String(params['shariahStatus']).trim()];
      } else {
        this.shariahStatuses = [];
      }

      this.priceComparison = params['priceComparison'] || '';
      this.minCompliantSources = params['minCompliantSources'] || '';
      this.sortBy = params['sortBy'] || 'changePct';
      this.sortDir = params['sortDir'] || 'desc';

      this.minPe = params['minPe'] !== undefined && params['minPe'] !== null ? Number(params['minPe']) : null;
      this.maxPe = params['maxPe'] !== undefined && params['maxPe'] !== null ? Number(params['maxPe']) : null;
      this.minPb = params['minPb'] !== undefined && params['minPb'] !== null ? Number(params['minPb']) : null;
      this.maxPb = params['maxPb'] !== undefined && params['maxPb'] !== null ? Number(params['maxPb']) : null;
      this.nonComplianceType = params['nonComplianceType'] || '';

      this.fetchStocks();
    });
  }

  // ── Shared-component event adapters (thin: they only call the existing handlers) ──
  onTabChange(id: string): void {
    this.indexCodes = this.indexCodes;
    if (id === 'all') {
      this.shariahStatuses = [];
      this.priceComparison = '';
    } else if (id === 'compliant') {
      this.shariahStatuses = ['Compliant'];
      this.priceComparison = '';
    } else if (id === 'cheap') {
      this.shariahStatuses = [];
      this.priceComparison = 'Cheap';
    }
    this.onFilterChange();
  }

  onSearchInput(value: string): void {
    this.search = value;
    this.onSearchChange();
  }

  onControlChange(e: { id: string; values: string[] }): void {
    if (e.id === 'index') {
      this.indexCodes = e.values;
      this.page = 1;
      this.updateUrl();
    } else if (e.id === 'shariah') {
      this.shariahStatuses = e.values;
      this.page = 1;
      this.updateUrl();
    } else if (e.id === 'valuation') {
      this.priceComparison = e.values[0] || '';
      this.onFilterChange();
    }
  }

  onSortSelect(value: string): void {
    const [field, dir] = value.split(':');
    this.sortBy = field;
    this.sortDir = dir;
    this.onSortFieldChange();
  }

  get activeTab(): string {
    const noOther = !this.nonComplianceType;
    if (this.priceComparison === 'Cheap' && !this.shariahStatuses.length && noOther) return 'cheap';
    if (this.shariahStatuses.length === 1 && this.shariahStatuses[0] === 'Compliant' && !this.priceComparison && noOther) return 'compliant';
    if (!this.shariahStatuses.length && !this.priceComparison) return 'all';
    return '';
  }

  private readonly sortFields = [
    { value: 'changePct', label: 'نسبة التغير' },
    { value: 'closingPrice', label: 'السعر' },
    { value: 'fairValueDiffPct', label: 'فارق العادلة' },
    { value: 'ticker', label: 'رمز السهم' }
  ];

  /** Builds the single config object the ListingPage renders. */
  rebuild(): void {
    const activeFilterCount =
      (this.nonComplianceType ? 1 : 0) + (this.minCompliantSources ? 1 : 0) +
      (this.minPe !== null || this.maxPe !== null ? 1 : 0) + (this.minPb !== null || this.maxPb !== null ? 1 : 0);
    this.config = {
      header: {
        badge: 'البورصة المصرية',
        count: `${this.totalCount} سهم متوفر`,
        title: 'الأسهم',
        description: 'ابحث، صفِّ، وقارن تقييمات الأسهم وأحكام الشريعة بناءً على بيانات المؤشرات ونماذج القيمة العادلة المعتمدة.'
      },
      tabs: [
        { id: 'all', label: 'كل الأسهم' },
        { id: 'compliant', label: 'متوافق شرعاً' },
        { id: 'cheap', label: 'فرص صعود للقيمة العادلة' }
      ],
      activeTab: this.activeTab,
      filters: {
        searchValue: this.search,
        searchPlaceholder: 'ابحث باسم السهم أو رمزه (مثال: COMI أو فوري)...',
        controls: [
          {
            id: 'index', kind: 'multi', label: 'المؤشرات', allLabel: 'كل المؤشرات',
            options: this.indices.map((i) => ({ value: i.code, label: `${i.code} - ${i.nameAr}` })),
            selected: this.indexCodes
          },
          {
            id: 'shariah', kind: 'multi', label: 'الأحكام', allLabel: 'كل الأحكام الشرعية',
            options: this.shariahOptions, selected: this.shariahStatuses
          },
          {
            id: 'valuation', kind: 'select', label: 'مقارنة القيمة العادلة',
            options: [
              { value: '', label: 'القيمة العادلة: الكل' },
              { value: 'Cheap', label: 'أرخص من العادلة (فرصة)' },
              { value: 'Fair', label: 'قريبة من العادلة (±5%)' },
              { value: 'Expensive', label: 'أغلى من العادلة' },
              { value: 'Unavailable', label: 'لا يمكن حساب القيمة العادلة' }
            ],
            value: this.priceComparison
          }
        ],
        sort: {
          label: 'ترتيب النتائج',
          value: `${this.sortBy}:${this.sortDir}`,
          options: this.sortFields.flatMap((f) => [
            { value: `${f.value}:desc`, label: `الترتيب: ${f.label} (تنازلي)` },
            { value: `${f.value}:asc`, label: `الترتيب: ${f.label} (تصاعدي)` }
          ])
        },
        advanced: { label: 'تصفية متقدمة', open: this.advancedOpen, count: activeFilterCount }
      },
      results: {
        text: `${this.totalCount} نتيجة مطابقة (الصفحة ${this.page} من ${this.totalPages || 1})`,
        legend: [{ label: 'متوافق', tone: 'good' }, { label: 'غير متوافق', tone: 'bad' }]
      },
      cards: this.displayedStocks.map((s) => this.toCard(s)),
      loading: this.loading,
      empty: {
        title: 'لا توجد نتائج مطابقة للفلاتر المختارة',
        message: 'جرّب تعديل خيارات البحث أو إزالة أحد الفلاتر لتوسيع نطاق النتائج.'
      },
      page: this.page,
      totalPages: this.totalPages
    };
  }

  private toCard(s: StockListItemDto): MzListingCardData {
    const tag = statusTag(s.shariahStatus);
    const reasons = normalizeStatusValue(s.shariahStatus) === 'noncompliant'
      ? getNonComplianceReasons({
          coreActivityCompliant: s.coreActivityCompliant,
          categoryAr: s.categoryAr,
          spHaramEarningPercentage: s.spHaramEarningPercentage,
          loansPercentage: s.loansPercentage
        })
      : [];
    const indexLabels = (s.indices || []).map((code) =>
      code === 'Sectoral-Indices' ? (s.sectorNameAr || INDEX_ARABIC_NAMES['Sectoral-Indices'] || 'قطاعي') : (INDEX_ARABIC_NAMES[code] || code));
    const change = signedPct(s.changePct);
    return {
      id: s.ticker,
      title: s.nameAr || s.ticker,
      subtitle: `${s.ticker} • ${s.nameEn || '—'}`,
      favoriteSymbol: s.ticker,
      badges: change ? [{ label: change, tone: pctTone(s.changePct) === 'negative' ? 'danger' : 'default' }] : [{ label: '—', tone: 'neutral' }],
      mainValue: fmtNumber(s.closingPrice) ?? '—',
      mainUnit: s.currency && s.currency.includes('دولار') ? '$' : 'ج.م',
      ribbon: comparisonRibbon(s.priceComparison, s.fairValueDiffPct, s.closingPrice, s.fairValue),
      tags: [{ label: tag.label, tone: tag.tone, note: reasons.length ? reasons.join(' · ') : null }],
      footerItems: [s.sectorNameAr || '', indexLabels.join(' • ')].filter(Boolean),
      link: ['/stocks', s.ticker]
    };
  }

  getShariahLabel(status: string): string {
    const match = this.shariahOptions.find(o => o.value === status);
    return match ? match.label : status;
  }

  getPriceComparisonLabel(val: string): string {
    switch (val) {
      case 'Cheap': return 'أرخص من العادلة';
      case 'Fair': return 'قريبة من العادلة';
      case 'Expensive': return 'أغلى من العادلة';
      case 'Unavailable': return 'غير متوفرة';
      default: return val;
    }
  }

  removeIndex(code: string): void {
    this.indexCodes = this.indexCodes.filter(c => c !== code);
    this.page = 1;
    this.updateUrl();
  }

  removeShariah(val: string): void {
    this.shariahStatuses = this.shariahStatuses.filter(s => s !== val);
    this.page = 1;
    this.updateUrl();
  }

  allFetchedStocks: StockListItemDto[] = [];
  rawTotalCount = 0;

  get isNonComplianceFiltered(): boolean {
    return !!(this.nonComplianceType && this.shariahStatuses.includes('NonCompliant'));
  }

  fetchStocks(): void {
    this.loading = true;
    this.rebuild();

    // Stored verdicts only: the backend filters/sorts on the persisted verdict,
    // so the list reads it directly with no client-side recomputation and no
    // "fetch NonCompliant then filter locally" workaround.
    const queryStatuses = this.shariahStatuses;

    const queryPageSize = this.isNonComplianceFiltered ? 200 : this.pageSize;
    const queryPage = this.isNonComplianceFiltered ? 1 : this.page;

    this.api.getStocks({
      page: queryPage,
      pageSize: queryPageSize,
      search: this.search || undefined,
      indexCodes: this.indexCodes.length ? this.indexCodes : undefined,
      shariahStatuses: queryStatuses.length ? queryStatuses : undefined,
      priceComparison: this.priceComparison || undefined,
      minCompliantSources: this.minCompliantSources ? Number(this.minCompliantSources) : undefined,
      minPeRatio: this.minPe !== null ? this.minPe : undefined,
      maxPeRatio: this.maxPe !== null ? this.maxPe : undefined,
      minPbRatio: this.minPb !== null ? this.minPb : undefined,
      maxPbRatio: this.maxPb !== null ? this.maxPb : undefined,
      sortBy: this.sortBy,
      sortDir: this.sortDir
    }).subscribe({
      next: (res) => {
        this.stocks = res.items || [];
        this.rawTotalCount = res.totalCount || 0;

        if (this.isNonComplianceFiltered) {
          const filtered = this.stocks.filter((s) => matchesNonComplianceType(s, this.nonComplianceType));
          this.totalCount = filtered.length;
          this.totalPages = Math.max(1, Math.ceil(this.totalCount / this.pageSize));
          if (this.page > this.totalPages) {
            this.page = 1;
          }
        } else {
          this.totalCount = res.totalCount || 0;
          this.totalPages = res.totalPages || Math.ceil(this.totalCount / this.pageSize) || 1;
        }

        this.loading = false;
        this.rebuild();
      },
      error: () => {
        this.stocks = [];
        this.totalCount = 0;
        this.totalPages = 1;
        this.loading = false;
        this.rebuild();
      }
    });
  }

  onSearchChange(): void {
    if (this.searchDebounceTimer) clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => {
      this.page = 1;
      this.updateUrl();
    }, 350);
  }

  onFilterChange(): void {
    this.page = 1;
    this.updateUrl();
  }

  onSortFieldChange(): void {
    this.page = 1;
    this.updateUrl();
  }

  onSortDirChange(): void {
    this.page = 1;
    this.updateUrl();
  }

  onPageSizeChange(): void {
    this.page = 1;
    this.updateUrl();
  }

  onPeMinChange(val: any): void {
    this.minPe = val !== '' && val !== null ? Number(val) : null;
    this.triggerSlicerDebounce();
  }

  onPeMaxChange(val: any): void {
    this.maxPe = val !== '' && val !== null ? Number(val) : null;
    this.triggerSlicerDebounce();
  }

  onPbMinChange(val: any): void {
    this.minPb = val !== '' && val !== null ? Number(val) : null;
    this.triggerSlicerDebounce();
  }

  onPbMaxChange(val: any): void {
    this.maxPb = val !== '' && val !== null ? Number(val) : null;
    this.triggerSlicerDebounce();
  }

  clearPeFilter(): void {
    this.minPe = null;
    this.maxPe = null;
    this.page = 1;
    this.updateUrl();
  }

  clearPbFilter(): void {
    this.minPb = null;
    this.maxPb = null;
    this.page = 1;
    this.updateUrl();
  }

  private triggerSlicerDebounce(): void {
    if (this.slicerDebounceTimer) clearTimeout(this.slicerDebounceTimer);
    this.slicerDebounceTimer = setTimeout(() => {
      this.page = 1;
      this.updateUrl();
    }, 400);
  }

  clearFilter(key: string): void {
    if (key === 'priceComparison') this.priceComparison = '';
    if (key === 'minCompliantSources') this.minCompliantSources = '';
    if (key === 'nonComplianceType') this.nonComplianceType = '';
    this.page = 1;
    this.updateUrl();
  }

  setPage(p: number): void {
    if (p < 1 || p > this.totalPages || p === this.page) return;
    this.page = p;
    this.updateUrl();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private updateUrl(): void {
    const queryParams: any = {
      page: this.page > 1 ? this.page : null,
      pageSize: this.pageSize !== 20 ? this.pageSize : null,
      search: this.search || null,
      indexCodes: this.indexCodes.length ? this.indexCodes.join(',') : null,
      shariahStatuses: this.shariahStatuses.length ? this.shariahStatuses.join(',') : null,
      priceComparison: this.priceComparison || null,
      minCompliantSources: this.minCompliantSources || null,
      minPe: this.minPe !== null ? this.minPe : null,
      maxPe: this.maxPe !== null ? this.maxPe : null,
      minPb: this.minPb !== null ? this.minPb : null,
      maxPb: this.maxPb !== null ? this.maxPb : null,
      nonComplianceType: this.nonComplianceType || null,
      sortBy: this.sortBy !== 'changePct' ? this.sortBy : null,
      sortDir: this.sortDir !== 'desc' ? this.sortDir : null
    };

    // Clean null fields from route. replaceUrl + equality guard live in
    // the helper so Back always leaves the page instead of looping params.
    navigateQueryParams(this.router, this.route, queryParams);
  }

  /** Displayed stocks: when the nonComplianceType sub-filter is active, filters and paginates client-side. */
  get displayedStocks() {
    if (this.isNonComplianceFiltered) {
      const filtered = this.stocks.filter((s) => matchesNonComplianceType(s, this.nonComplianceType));
      const start = (this.page - 1) * this.pageSize;
      return filtered.slice(start, start + this.pageSize);
    }
    return this.stocks;
  }
}
