import { ComparisonBadgeComponent } from '../comparison-badge/comparison-badge.component';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';
import type { MzListingCardData } from './ui-listing';

/** Pure presentation helpers shared by the listing/details page configs (no business logic). */

export function signedPct(value: number | null | undefined, digits = 2): string | null {
  if (value === null || value === undefined || isNaN(Number(value))) return null;
  const v = Number(value);
  return `${v > 0 ? '+' : ''}${v.toFixed(digits)}%`;
}

export function pctTone(value: number | null | undefined): 'positive' | 'negative' | 'muted' {
  if (value === null || value === undefined || isNaN(Number(value))) return 'muted';
  return Number(value) > 0 ? 'positive' : Number(value) < 0 ? 'negative' : 'muted';
}

export function fmtNumber(value: number | null | undefined, digits = 2): string | null {
  if (value === null || value === undefined || isNaN(Number(value))) return null;
  return Number(value).toLocaleString('en-US', { minimumFractionDigits: digits, maximumFractionDigits: digits });
}

/** Sharia status → tag, reusing the StatusBadge label/class rules. */
export function statusTag(status?: string | null): { label: string; tone: 'good' | 'bad' | 'neutral' } {
  const b = new StatusBadgeComponent();
  b.status = status;
  const cls = b.badgeClass;
  return { label: b.label, tone: cls === 'status-good' ? 'good' : cls === 'status-bad' ? 'bad' : 'neutral' };
}

/** Fair-value comparison → ribbon, reusing the ComparisonBadge wording/rules. */
export function comparisonRibbon(
  comparison?: string | null, fairValueDiffPct?: number | null, closingPrice?: number | null, fairValue?: number | null
): NonNullable<MzListingCardData['ribbon']> {
  const c = new ComparisonBadgeComponent();
  c.comparison = comparison;
  c.fairValueDiffPct = fairValueDiffPct;
  c.closingPrice = closingPrice;
  c.fairValue = fairValue;
  const cls = c.badgeClass;
  return { text: c.label, tone: cls === 'cheap' ? 'good' : cls === 'expensive' ? 'bad' : 'neutral' };
}

import { INDEX_ARABIC_NAMES, type StockListItemDto, type ConstituentItemDto } from '../../models/api.models';
import { getNonComplianceReasons, normalizeStatusValue } from '../../models/shariah-standards';

/** Shared stock card mapper: identical configuration across Stocks page, Sector detail, and Index detail. */
export function stockToCard(s: StockListItemDto | ConstituentItemDto): MzListingCardData {
  const tag = statusTag(s.shariahStatus);
  const reasons = normalizeStatusValue(s.shariahStatus) === 'noncompliant'
    ? getNonComplianceReasons({
        coreActivityCompliant: (s as any).coreActivityCompliant,
        categoryAr: (s as any).categoryAr,
        spHaramEarningPercentage: (s as any).spHaramEarningPercentage,
        loansPercentage: (s as any).loansPercentage
      })
    : [];
  const indexLabels = (s.indices || []).map((code) =>
    code === 'Sectoral-Indices' ? (s.sectorNameAr || INDEX_ARABIC_NAMES['Sectoral-Indices'] || 'قطاعي') : (INDEX_ARABIC_NAMES[code] || code));
  const change = signedPct(s.changePct);
  const footerItems: string[] = [s.sectorNameAr || '', indexLabels.join(' • ')].filter(Boolean);
  if (s.weight !== null && s.weight !== undefined && s.weight > 0) {
    footerItems.push(`الوزن: ${s.weight.toFixed(2)}%`);
  }
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
    footerItems,
    link: ['/stocks', s.ticker]
  };
}

