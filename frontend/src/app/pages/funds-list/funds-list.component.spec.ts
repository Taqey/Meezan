import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { FundsListComponent } from './funds-list.component';
import { ApiService } from '../../services/api.service';
import type { FundClassificationTreeNode, FundListItemDto } from '../../models/api.models';
import { REAL_FUNDS } from './real-funds.fixture';

function treeNode(partial: Partial<FundClassificationTreeNode> & { slug: string }): FundClassificationTreeNode {
  return {
    id: partial.slug.length, nameAr: partial.slug, nameEn: partial.slug, level: 0,
    parentSlug: null, displayOrder: 1, childrenLabelAr: 'التصنيف الفرعي',
    childrenLabelEn: 'Sub classification', directFundCount: 1, totalFundCount: 1,
    hasAutoRule: false, ...partial,
  };
}

function fund(code: string): FundListItemDto {
  return {
    id: code.length, sndukId: 1, publicId: 'p-' + code, slug: 's-' + code,
    nameAr: 'صندوق ' + code, nameEn: 'Fund ' + code, code, type: 'equity' as never,
    typeNameAr: 'صندوق أسهم', typeNameEn: 'Equity', riskLevel: 'low' as never,
    isIslamic: false, isFeatured: false, currentPrice: 100, dailyChange: 1,
    return1M: 1, return3M: 2, return6M: 3, return1Y: 10, return3Y: 30,
    totalScore: 50, aum: 1000000, managementFee: 1, minimumInvestment: '1000',
    currency: 'EGP', assetManagerId: 1, assetManagerName: 'مدير', assetManagerNameEn: 'Mgr',
    logoUrl: '', localLogoPath: '', lastSyncedAt: '2026-10-14T21:00:00',
    classificationSlug: 'equity', classificationPathAr: 'صندوق أسهم', classificationSource: 'SNDUK',
  };
}

describe('FundsListComponent classification wiring', () => {
  let fixture: ComponentFixture<FundsListComponent>;

  beforeEach(async () => {
    const tree: FundClassificationTreeNode[] = [
      treeNode({ slug: 'equity', nameAr: 'صندوق أسهم', totalFundCount: 2, childrenLabelAr: 'نوع صندوق الأسهم' }),
      treeNode({ slug: 'sector', nameAr: 'قطاع', level: 1, parentSlug: 'equity', totalFundCount: 1, childrenLabelAr: 'القطاع' }),
      treeNode({ slug: 'sector-financial', nameAr: 'مالي', level: 2, parentSlug: 'sector', totalFundCount: 1 }),
    ];
    const api = {
      getFunds: (f: { pageSize?: number }) => of(f?.pageSize === 1000
        ? { items: REAL_FUNDS, totalCount: 3, page: 1, pageSize: 1000, totalPages: 1 }
        : { items: [fund('AA'), fund('BB')], totalCount: 2, page: 1, pageSize: 20, totalPages: 1 }),
      getClassificationTree: () => of(tree),
    };
    const route = { queryParamMap: of(new Map() as never) };
    await TestBed.configureTestingModule({
      imports: [FundsListComponent],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: api },
        { provide: ActivatedRoute, useValue: route },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(FundsListComponent);
  });

  it('renders the cascade and fund cards', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const html = fixture.nativeElement.innerHTML as string;
    expect(html).toContain('cascade-0');
    expect(html).toContain('صندوق AA');
  });

  it('renders verbatim live fund items without hanging', async () => {
    const comp = fixture.componentInstance;
    fixture.detectChanges(); // ngOnInit with stub data
    await fixture.whenStable();
    comp.funds = REAL_FUNDS;
    comp.totalCount = 171;
    comp.loading = false;
    (comp as unknown as { statsLoading: boolean }).statsLoading = false;
    fixture.detectChanges();
    await fixture.whenStable();
    const cards = fixture.nativeElement.querySelectorAll('app-entity-card').length;
    expect(`funds=${comp.funds.length} cards=${cards}`).toBe('funds=3 cards=3');
  });

  it('drives the node filter from a cascade pick', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const comp = fixture.componentInstance;
    comp.onClassPathChange(['equity', 'sector']);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(comp.filter.node).toBe('sector');
  });
});
