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
