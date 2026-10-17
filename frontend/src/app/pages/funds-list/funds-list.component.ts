import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { LucideAngularModule, Search, ChevronDown, ShieldCheck, TrendingUp, Star, Building2, DollarSign, AlertCircle } from 'lucide-angular';
import { ApiService } from '../../services/api.service';
import { FundListItemDto, FundPagedResultDto, FundListFilter, FundType, FundClassificationTreeNode } from '../../models/api.models';
import {
  normFundType,
  normFundRiskLevel,
  fundRiskLabelAr,
  fundLogoUrl,
  FUND_SHARIA_BADGE_TOOLTIP
} from '../../models/fund-display';
import { PageHeaderComponent } from '../../components/page-header/page-header.component';
import { ClassificationCascadeComponent } from '../../components/classification-cascade/classification-cascade.component';
import { EntityCardComponent, EntityStat } from '../../components/entity-card/entity-card.component';
import { FilterChipsComponent, ChipOption } from '../../components/filter-chips/filter-chips.component';
import { SectionHeaderComponent } from '../../components/section-header/section-header.component';
import { LoadingSkeletonsComponent } from '../../components/loading-skeletons/loading-skeletons.component';
import { EmptyStateComponent } from '../../components/empty-state/empty-state.component';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { KpiStripComponent, KpiData } from '../../components/kpi-strip/kpi-strip.component';

@Component({
  selector: 'app-funds-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, PageHeaderComponent, ClassificationCascadeComponent, EntityCardComponent, FilterChipsComponent, SectionHeaderComponent, LoadingSkeletonsComponent, EmptyStateComponent, KpiStripComponent],
  template: `
    <div class="entity-page">
      <app-page-header
        [crumbs]="headerCrumbs"
        pill="الصناديق الاستثمارية"
        title="الصناديق الاستثمارية المصرية"
        description="استعرض الصناديق الاستثمارية المصرية مع فلاتر متقدمة للتصنيف، المخاطر، العوائد، والرسوم.">
      </app-page-header>

      <app-kpi-strip [kpis]="kpiList" [loading]="loading || statsLoading"></app-kpi-strip>

      <app-filter-chips
        *ngIf="!loading && !loadError"
        [chips]="chipOptions"
        [activeId]="activeChip"
        (selected)="setChip($event)">
      </app-filter-chips>

      <app-section-header
        *ngIf="!loading && !loadError"
        [lead]="totalCount + ' صندوقاً استثمارياً في قاعدة البيانات'"
        [sourceNote]="sourceNote">
      </app-section-header>

      <div class="filters" *ngIf="!loading && !loadError">
        <div class="filters-row-main">
          <div>
            <label class="filter-label" for="fund-search">بحث بالاسم أو الكود</label>
            <div class="search-wrap">
              <lucide-icon [img]="SearchIcon" size="17"></lucide-icon>
              <input
                id="fund-search"
                [(ngModel)]="searchText"
                (ngModelChange)="searchChanged.next($event)"
                placeholder="ابحث باسم الصندوق أو رمزه (مثال: PHF أو استثمار)..."
                aria-label="البحث عن صندوق" />
            </div>
          </div>

          <app-classification-cascade
            [tree]="classTree"
            [path]="classPath"
            rootLabel="نوع الصندوق"
            allLabel="كل الأنواع"
            (pathChange)="onClassPathChange($event)">
          </app-classification-cascade>

          <div>
            <label class="filter-label" for="fund-risk">مستوى المخاطر</label>
            <select id="fund-risk" [(ngModel)]="filter.riskLevel" (ngModelChange)="loadFunds()" aria-label="مستوى المخاطر">
              <option [ngValue]="undefined">كل المستويات</option>
              <option value="low">منخفضة</option>
              <option value="medium">متوسطة</option>
              <option value="high">عالية</option>
            </select>
          </div>

          <div>
            <label class="filter-label" for="fund-sharia">الحكم الشرعي (المصدر)</label>
            <select id="fund-sharia" [(ngModel)]="filter.isIslamic" (ngModelChange)="loadFunds()" aria-label="الحكم الشرعي">
              <option [ngValue]="undefined">الكل</option>
              <option [ngValue]="true">متوافق شرعياً</option>
              <option [ngValue]="false">غير متوافق</option>
            </select>
          </div>

        </div>

        <div class="filters-row-secondary">
          <div class="sort-group">
            <select [(ngModel)]="filter.sortBy" (ngModelChange)="loadFunds()" aria-label="حقل الترتيب">
              <option value="return1Y">العائد 1Y</option>
              <option value="return6M">العائد 6M</option>
              <option value="return3M">العائد 3M</option>
              <option value="return1M">العائد 1M</option>
              <option value="aum">الأصول (AUM)</option>
              <option value="totalScore">الدرجة الكلية</option>
              <option value="managementFee">الرسوم</option>
              <option value="name">أبجدي</option>
            </select>
            <select [(ngModel)]="filter.sortDir" (ngModelChange)="loadFunds()" aria-label="اتجاه الترتيب">
              <option value="desc">تنازلي</option>
              <option value="asc">تصاعدي</option>
            </select>
          </div>
          <button type="button" class="btn btn-outline btn-sm" (click)="resetFilters()">
            <lucide-icon [img]="AlertCircleIcon" size="15"></lucide-icon> مسح الفلاتر
          </button>
        </div>
      </div>

      <div class="results-head" *ngIf="!loading && !loadError">
        <span>{{ totalCount }} نتيجة مطابقة (الصفحة {{ currentPage }} من {{ totalPages || 1 }})</span>
      </div>

      <app-loading-skeletons *ngIf="loading && !loadError" variant="card" [count]="6"></app-loading-skeletons>

      <app-empty-state
        *ngIf="!loading && loadError"
        title="تعذّر تحميل الصناديق"
        message="تحقق من الاتصال ثم حاول مرة أخرى."
        actionLabel="إعادة المحاولة"
        (action)="loadFunds()">
      </app-empty-state>

      <app-empty-state
        *ngIf="!loading && !loadError && funds.length === 0"
        title="لا توجد صناديق مطابقة للفلاتر المختارة"
        message="جرّب تعديل خيارات البحث أو إزالة أحد الفلاتر لتوسيع نطاق النتائج.">
      </app-empty-state>

      <div class="entity-grid" *ngIf="!loading && !loadError && funds.length">
        <app-entity-card
          *ngFor="let fund of funds; let i = index; trackBy: trackFund"
          [itemIndex]="i"
          [title]="fund.nameAr"
          [subtitle]="fund.nameEn"
          [icon]="getFundIcon(normFundType(fund.type))"
          [accent]="getFundAccent(normFundType(fund.type)).accent"
          [tint]="getFundAccent(normFundType(fund.type)).tint"
          [ink]="getFundAccent(normFundType(fund.type)).ink"
          [pill]="getClassificationBadge(fund)"
          [stats]="cardStats(fund)"
          [footerText]="footerFor(fund)"
          [link]="['/funds', fund.slug]"
          [ribbon]="fund.isIslamic ? 'متوافق شرعياً' : null"
          [ribbonTitle]="fund.isIslamic ? shariaBadgeTooltip : null"
          [featured]="fund.isFeatured"
          [logoUrl]="fundLogoUrl(fund.localLogoPath, fund.logoUrl)">
        </app-entity-card>
      </div>

      <div *ngIf="!loading && !loadError && totalPages > 1" class="pagination" style="display: flex; justify-content: center; gap: 8px; margin-top: 24px; padding: 0 24px 24px;">
        <button class="btn btn-outline" (click)="goToPage(currentPage - 1)" [disabled]="currentPage <= 1">
          <lucide-icon [img]="ChevronDownIcon" size="16"></lucide-icon> السابق
        </button>
        <span style="display: flex; align-items: center; padding: 0 16px; color: var(--muted-foreground);">
          صفحة {{ currentPage }} من {{ totalPages }}
        </span>
        <button class="btn btn-outline" (click)="goToPage(currentPage + 1)" [disabled]="currentPage >= totalPages">
          التالي <lucide-icon [img]="ChevronDownIcon" size="16"></lucide-icon>
        </button>
      </div>

      <div class="attribution" style="margin: 24px; padding: 16px; background: var(--muted); border-radius: 12px; text-align: center; font-size: 12px; color: var(--muted-foreground);">
        المصدر: <a href="https://snduk.com" target="_blank" rel="noopener" style="color: var(--primary); text-decoration: underline;">SNDUK</a> |
        آخر تحديث: {{ lastSyncedAt ? (lastSyncedAt | date:'yyyy-MM-dd HH:mm') : '—' }} |
        <span style="color: var(--bad);">إخلاء المسؤولية: البيانات للأغراض المعلوماتية فقط ولا تُعد نصيحة استثمارية.</span>
      </div>
    </div>
  `,
  styles: [`
    .filter-label {
      font-size: 12px;
      font-weight: 600;
      display: block;
      margin-bottom: 4px;
      color: var(--foreground);
    }
    .filters-row-main > div {
      min-width: 0;
    }
    .filters select {
      width: 100%;
    }
    .attribution a:hover { text-decoration: none; }
    @media (max-width: 600px) {
      .filters-row-secondary {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class FundsListComponent implements OnInit {
  readonly SearchIcon = Search;
  readonly AlertCircleIcon = AlertCircle;
  readonly ChevronDownIcon = ChevronDown;
  readonly ShieldCheckIcon = ShieldCheck;
  readonly TrendingUpIcon = TrendingUp;
  readonly StarIcon = Star;
  readonly Building2Icon = Building2;
  readonly DollarSignIcon = DollarSign;

  readonly shariaBadgeTooltip = FUND_SHARIA_BADGE_TOOLTIP;
  readonly normFundType = normFundType;
  readonly fundLogoUrl = fundLogoUrl;

  headerCrumbs = [{ label: 'الرئيسية', link: ['/'] }];

  funds: FundListItemDto[] = [];
  /** Public classification tree driving the cascading filters. */
  classTree: FundClassificationTreeNode[] = [];
  /** Selected classification path, root-first slugs (e.g. ['equity','sector','sector-financial']). */
  classPath: string[] = [];
  loading = true;
  loadError = false;
  statsLoading = true;

  filter: FundListFilter = {
    sortBy: 'return1y',
    sortDir: 'desc',
    page: 1,
    pageSize: 20
  };

  /** Bound to the search input; debounced into filter.search via searchChanged. */
  searchText = '';
  readonly searchChanged = new Subject<string>();

  currentPage = 1;
  totalPages = 0;
  totalCount = 0;

  activeChip = 'all';
  chipOptions: ChipOption[] = [
    { id: 'all', label: 'الكل' },
    { id: 'islamic', label: 'متوافق شرعياً' },
    { id: 'featured', label: 'مميز' },
    { id: 'top_return', label: 'أعلى عائد' }
  ];

  kpiList: KpiData[] = [];

  lastSyncedAt: string | null = null;

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.searchChanged.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe((query) => {
      this.filter.search = query?.trim() || undefined;
      this.filter.page = 1;
      this.currentPage = 1;
      this.loadFunds();
    });

    // Deep-link support: node=<slug> as primary, supporting legacy params (type, subType, leaf, old slugs, path).
    this.route.queryParamMap.subscribe(params => {
      const nodeSlug = params.get('node');
      const leafSlug = params.get('leaf');
      const subTypeSlug = params.get('subType');
      const typeSlug = params.get('type');
      const rawPath = params.get('path');

      const targetSlug = nodeSlug || leafSlug || subTypeSlug || typeSlug;
      if (targetSlug) {
        this.pendingLegacySlug = targetSlug.trim().toLowerCase();
        if (this.classTree.length) {
          const full = this.pathForSlug(this.pendingLegacySlug);
          if (full.length) this.classPath = full;
          this.pendingLegacySlug = null;
          this.applyClassPathToFilter();
        }
      } else if (rawPath) {
        this.classPath = rawPath.split(',').map(s => s.trim().toLowerCase()).filter(Boolean);
        this.applyClassPathToFilter();
      } else {
        this.classPath = [];
        this.applyClassPathToFilter();
      }
    });

    this.loadClassTree();
    this.loadFunds();
    this.loadFundStats();
  }

  get sourceNote(): string | null {
    return 'المصدر: SNDUK';
  }

  /** Single legacy slug from ?node= / ?type= links, resolved to a full path once the tree loads. */
  private pendingLegacySlug: string | null = null;

  loadClassTree(): void {
    this.api.getClassificationTree().subscribe({
      next: (tree) => {
        this.classTree = tree || [];
        if (this.pendingLegacySlug) {
          const full = this.pathForSlug(this.pendingLegacySlug);
          if (full.length) this.classPath = full;
          this.pendingLegacySlug = null;
          this.applyClassPathToFilter();
        }
      },
      error: () => {}
    });
  }

  /** Full root-first path for any known slug (used for legacy single-slug links). */
  private pathForSlug(slug: string): string[] {
    const s = (slug || '').trim().toLowerCase();
    if (!s) return [];
    // Legacy alias handled server-side too, but resolve locally for the cascade UI.
    const target = s === 'commodities' ? 'silver' : s;
    const bySlug = new Map(this.classTree.map(n => [n.slug.toLowerCase(), n]));
    const node = bySlug.get(target);
    if (!node) {
      // Might still be valid server-side (e.g. hidden unclassified); single-level fallback.
      return [target];
    }
    const path: string[] = [node.slug];
    let cur: FundClassificationTreeNode | undefined = node;
    let guard = 0;
    while (cur?.parentSlug && guard++ < 8) {
      const parent = bySlug.get(cur.parentSlug.toLowerCase());
      if (!parent) break;
      path.unshift(parent.slug);
      cur = parent;
    }
    return path;
  }

  /** Deepest selected slug drives the API `node` filter (includes descendants server-side). */
  private applyClassPathToFilter(): void {
    const deepest = this.classPath.length ? this.classPath[this.classPath.length - 1] : undefined;
    this.filter.node = deepest;
    // The legacy `type` key is no longer written by this page (backend keeps accepting it).
    this.filter.type = undefined;
  }

  onClassPathChange(path: string[]): void {
    this.classPath = path;
    this.applyClassPathToFilter();
    this.filter.page = 1;
    this.currentPage = 1;
    this.syncClassPathToUrl();
    this.loadFunds();
  }

  private syncClassPathToUrl(): void {
    const deepest = this.classPath.length ? this.classPath[this.classPath.length - 1] : null;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        node: deepest,
        path: null,
        type: null,
        subType: null,
        leaf: null
      },
      queryParamsHandling: 'merge'
    });
  }

  loadFunds(): void {
    this.loading = true;
    this.loadError = false;
    this.filter.page = this.currentPage;

    this.api.getFunds(this.filter).subscribe({
      next: (res: FundPagedResultDto) => {
        this.funds = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.currentPage = res.page;
        this.loading = false;
        this.updateLastSyncedAt();
      },
      error: () => {
        this.loading = false;
        this.loadError = true;
      }
    });
  }

  loadFundStats(): void {
    this.statsLoading = true;
    // Use lightweight /api/funds/stats — single DB query, no fund records transferred.
    this.api.getFundStats().subscribe({
      next: (stats) => {
        this.kpiList = [
          { icon: this.Building2Icon, value: String(stats.totalCount), label: 'صندوقاً استثمارياً' },
          {
            icon: this.TrendingUpIcon,
            value: stats.bestDailyChange != null
              ? `${stats.bestDailyChange > 0 ? '+' : ''}${stats.bestDailyChange.toFixed(2)}%`
              : '—',
            sub: stats.bestDailyChangeCode ?? null,
            label: 'أفضل أداء اليوم'
          },
          { icon: this.ShieldCheckIcon, value: String(stats.islamicCount), label: 'متوافق شرعياً (SNDUK)' },
          { icon: this.DollarSignIcon, value: this.formatAum(stats.totalAum), label: 'إجمالي الأصول تحت الإدارة' }
        ];
        this.statsLoading = false;
      },
      error: () => { this.statsLoading = false; }
    });
  }

  private formatAum(value: number): string {
    if (!value) return '—';
    if (value >= 1e9) return `${(value / 1e9).toFixed(2)} مليار`;
    if (value >= 1e6) return `${(value / 1e6).toFixed(1)} م`;
    return String(Math.round(value));
  }

  updateLastSyncedAt(): void {
    const latest = this.funds.reduce((max, f) => f.lastSyncedAt && f.lastSyncedAt > max ? f.lastSyncedAt : max, '');
    this.lastSyncedAt = latest || null;
  }

  setChip(id: string): void {
    this.activeChip = id;
    this.filter.page = 1;
    this.currentPage = 1;
    switch (id) {
      case 'islamic':
        this.filter.isIslamic = true;
        this.filter.isFeatured = undefined;
        break;
      case 'featured':
        this.filter.isIslamic = undefined;
        this.filter.isFeatured = true;
        break;
      case 'top_return':
        this.filter.sortBy = 'return1y';
        this.filter.sortDir = 'desc';
        break;
      default:
        this.filter.isIslamic = undefined;
        this.filter.isFeatured = undefined;
    }
    this.loadFunds();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadFunds();
  }

  resetFilters(): void {
    this.filter = { sortBy: 'return1y', sortDir: 'desc', page: 1, pageSize: 20 };
    this.classPath = [];
    this.searchText = '';
    this.activeChip = 'all';
    this.currentPage = 1;
    this.syncClassPathToUrl();
    this.loadFunds();
  }

  trackFund(_: number, fund: FundListItemDto): string { return fund.code; }

  cardStats(fund: FundListItemDto): EntityStat[] {
    const stats: EntityStat[] = [
      fund.currentPrice != null
        ? { label: 'السعر', value: fund.currentPrice + ' ' + fund.currency }
        : { label: 'السعر', value: null },
      fund.dailyChange != null
        ? { label: 'التغيير اليومي', value: (fund.dailyChange > 0 ? '+' : '') + fund.dailyChange + '%', tone: fund.dailyChange > 0 ? 'positive' : fund.dailyChange < 0 ? 'negative' : 'muted' }
        : { label: 'التغيير اليومي', value: null },
      fund.return1Y != null
        ? { label: 'العائد 1Y', value: (fund.return1Y > 0 ? '+' : '') + fund.return1Y.toFixed(2) + '%' }
        : { label: 'العائد 1Y', value: null },
      fund.aum != null
        ? { label: 'الأصول', value: (fund.aum / 1e6).toFixed(1) + ' م' + fund.currency }
        : { label: 'الأصول', value: null },
      fund.managementFee != null
        ? { label: 'الرسوم', value: fund.managementFee + '%' }
        : { label: 'الرسوم', value: null }
    ];
    const riskLabel = fundRiskLabelAr(normFundRiskLevel(fund.riskLevel));
    if (riskLabel !== null) {
      stats.push({ label: 'المخاطر', value: riskLabel });
    }
    return stats;
  }

  getClassificationBadge(fund: FundListItemDto): string {
    if (fund.effectiveClassification?.rootNameAr) {
      return fund.effectiveClassification.rootNameAr;
    }
    if (fund.classificationPathAr) {
      const parts = fund.classificationPathAr.split(' › ');
      if (parts.length > 0 && parts[0].trim()) return parts[0].trim();
    }
    return fund.typeNameAr || 'صندوق استثمار';
  }

  footerFor(fund: FundListItemDto): string {
    const parts = [];
    const path = fund.effectiveClassification?.path || fund.classificationPathAr;
    if (path) parts.push(path);
    if (fund.assetManagerName) parts.push(fund.assetManagerName);
    if (!path && (fund.effectiveClassification?.rootNameAr || fund.typeNameAr)) {
      parts.push(fund.effectiveClassification?.rootNameAr || fund.typeNameAr);
    }
    if (fund.minimumInvestment) parts.push('الحد الأدنى: ' + fund.minimumInvestment);
    return parts.join(' • ');
  }

  getFundIcon(type: FundType): any {
    switch (type) {
      case 'equity': return this.Building2Icon;
      case 'fixed_income': return this.DollarSignIcon;
      case 'money_market': return this.ShieldCheckIcon;
      case 'balanced': return this.TrendingUpIcon;
      case 'index': return this.StarIcon;
      case 'gold': return this.DollarSignIcon;
      default: return this.Building2Icon;
    }
  }

  getFundAccent(type: FundType): { accent: string; tint: string; ink: string } {
    switch (type) {
      case 'equity': return { accent: '#2563eb', tint: '#dbeafe', ink: '#1e40af' };
      case 'fixed_income': return { accent: '#16a34a', tint: '#dcfce7', ink: '#166534' };
      case 'money_market': return { accent: '#0891b2', tint: '#cffafe', ink: '#0e7490' };
      case 'balanced': return { accent: '#9333ea', tint: '#f3e8ff', ink: '#6b21a8' };
      case 'index': return { accent: '#f59e0b', tint: '#fef3c7', ink: '#92400e' };
      case 'gold': return { accent: '#f59e0b', tint: '#fef3c7', ink: '#92400e' };
      default: return { accent: '#64748b', tint: '#f1f5f9', ink: '#475569' };
    }
  }
}
