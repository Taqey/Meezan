import { Component, EventEmitter, HostListener, ElementRef, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideAngularModule, Search, ChevronDown, Star, FileText, type LucideIconData } from 'lucide-angular';
import { FavoriteToggleComponent } from '../favorite-toggle/favorite-toggle.component';

export type Tone = 'positive' | 'negative' | 'muted' | null | undefined;

/* ───────────────────────── Level 1 ───────────────────────── */

/** Generic icon wrapper (lucide). */
@Component({
  selector: 'mz-icon',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `<lucide-icon *ngIf="icon" [img]="icon" [size]="size"></lucide-icon>`
})
export class MzIconComponent {
  @Input() icon?: LucideIconData | null;
  @Input() size = 16;
}

/** Button or router link rendered as a button. */
@Component({
  selector: 'mz-button',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule],
  host: { class: 'mz', style: 'display:inline-flex' },
  template: `
    <a *ngIf="link; else btn" class="mz-btn" [ngClass]="variant" [routerLink]="link">
      <lucide-icon *ngIf="icon" [img]="icon" [size]="15"></lucide-icon><ng-content></ng-content>
    </a>
    <ng-template #btn>
      <button class="mz-btn" [ngClass]="[variant, block ? 'block' : '']" [attr.type]="'button'" [disabled]="disabled"
              [attr.aria-pressed]="pressed ?? null" [attr.aria-expanded]="expanded ?? null" (click)="pressed_.emit($event)">
        <lucide-icon *ngIf="icon" [img]="icon" [size]="15"></lucide-icon><ng-content></ng-content>
      </button>
    </ng-template>
  `
})
export class MzButtonComponent {
  @Input() variant: 'default' | 'primary' | 'soft' = 'default';
  @Input() icon?: LucideIconData | null;
  @Input() link?: string | unknown[] | null;
  @Input() disabled = false;
  @Input() block = false;
  @Input() pressed?: boolean | null;
  @Input() expanded?: boolean | null;
  @Output('clicked') pressed_ = new EventEmitter<Event>();
}

@Component({
  selector: 'mz-badge',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'mz', style: 'display:inline-flex' },
  template: `<span class="mz-badge" [ngClass]="[tone, pill ? 'pill' : '']"><ng-content></ng-content></span>`
})
export class MzBadgeComponent {
  @Input() tone: 'default' | 'neutral' | 'danger' | 'warn' | 'outline' = 'default';
  @Input() pill = false;
}

@Component({
  selector: 'mz-chip',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'mz', style: 'display:inline-flex' },
  template: `<button type="button" class="mz-chip" [class.active]="active" [attr.aria-pressed]="active" (click)="selected.emit()"><ng-content></ng-content></button>`
})
export class MzChipComponent {
  @Input() active = false;
  @Output() selected = new EventEmitter<void>();
}

@Component({
  selector: 'mz-search-input',
  standalone: true,
  imports: [LucideAngularModule],
  host: { class: 'mz', style: 'display:block;width:100%' },
  template: `
    <div class="mz-input-wrap">
      <span class="mz-input-icon"><lucide-icon [img]="SearchIcon" [size]="18"></lucide-icon></span>
      <input class="mz-input" type="text" [value]="value" [placeholder]="placeholder" [attr.aria-label]="ariaLabel || placeholder"
             (input)="valueChange.emit($any($event.target).value)" />
    </div>
  `
})
export class MzSearchInputComponent {
  readonly SearchIcon = Search;
  @Input() value = '';
  @Input() placeholder = '';
  @Input() ariaLabel = '';
  @Output() valueChange = new EventEmitter<string>();
}

export interface MzOption { value: string | number | boolean | null; label: string; }

@Component({
  selector: 'mz-select',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'mz', style: 'display:block' },
  template: `
    <select class="mz-select" [attr.aria-label]="ariaLabel" (change)="onChange($any($event.target).selectedIndex)">
      <option *ngFor="let o of options; let i = index" [selected]="o.value === value">{{ o.label }}</option>
    </select>
  `
})
export class MzSelectComponent {
  @Input() options: MzOption[] = [];
  @Input() value: MzOption['value'] = null;
  @Input() ariaLabel = '';
  @Output() valueChange = new EventEmitter<MzOption['value']>();
  onChange(i: number): void { this.valueChange.emit(this.options[i]?.value ?? null); }
}

/** Star toggle bound to the shared favorites service. */
@Component({
  selector: 'mz-favorite-button',
  standalone: true,
  imports: [FavoriteToggleComponent],
  host: { class: 'mz mz-fav' },
  template: `<app-favorite-toggle [symbol]="symbol"></app-favorite-toggle>`
})
export class MzFavoriteButtonComponent {
  @Input() symbol = '';
}

/** Compliant / non-compliant / neutral tag. */
@Component({
  selector: 'mz-status-tag',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'mz', style: 'display:inline-flex' },
  template: `<span class="mz-tag" [ngClass]="tone"><span class="mz-tag-dot"></span>{{ label }}</span>`
})
export class MzStatusTagComponent {
  @Input() tone: 'good' | 'bad' | 'neutral' = 'neutral';
  @Input() label = '';
}

/* ───────────────────────── Level 2 ───────────────────────── */

export interface MzStat {
  label: string;
  value: string | number | null;
  icon?: LucideIconData;
  chip?: string | null;
  accent?: boolean;
  /** Smaller text value (e.g. top performer name). */
  text?: string | null;
}

@Component({
  selector: 'mz-stat-card',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  host: { class: 'mz', style: 'display:block' },
  template: `
    <div class="mz-card mz-stat" style="height:100%">
      <div class="mz-stat-top">
        <span class="mz-stat-label">{{ stat.label }}</span>
        <span class="mz-stat-icon" *ngIf="stat.icon"><lucide-icon [img]="stat.icon" [size]="18"></lucide-icon></span>
      </div>
      <div class="mz-stat-body">
        <span class="mz-stat-value" [class.accent]="stat.accent">{{ stat.value ?? '—' }}</span>
        <span class="mz-stat-chip" *ngIf="stat.chip">{{ stat.chip }}</span>
      </div>
    </div>
  `
})
export class MzStatCardComponent {
  @Input() stat!: MzStat;
}

export interface MzMetric { label: string; value: string | number | null | undefined; tone?: Tone; ltr?: boolean; }

@Component({
  selector: 'mz-metric-item',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'mz', style: 'display:block;min-width:0' },
  template: `
    <div class="mz-metric">
      <span class="mz-metric-label">{{ label }}</span>
      <span class="mz-metric-value" [ngClass]="value == null || value === '' ? 'muted' : tone" [class.mz-ltr]="ltr">{{ value == null || value === '' ? '—' : value }}</span>
    </div>
  `
})
export class MzMetricItemComponent {
  @Input() label = '';
  @Input() value: string | number | null | undefined;
  @Input() tone: Tone;
  @Input() ltr = false;
}

export interface MzFilterOption { value: string; label: string; }

/** Dropdown with checkbox list (multi) or single choice. */
@Component({
  selector: 'mz-filter-dropdown',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  host: { class: 'mz', style: 'display:block' },
  template: `
    <div class="mz-fdrop">
      <button type="button" class="mz-fdrop-trigger" (click)="open = !open" [attr.aria-expanded]="open" [attr.aria-label]="ariaLabel || label">
        <span>{{ triggerLabel }}</span><lucide-icon [img]="ChevronIcon" [size]="14"></lucide-icon>
      </button>
      <div class="mz-fdrop-panel" *ngIf="open">
        <label class="mz-fdrop-item" *ngFor="let o of options">
          <input [type]="multi ? 'checkbox' : 'radio'" [checked]="isSelected(o.value)" (change)="toggle(o.value)" />
          <span>{{ o.label }}</span>
        </label>
      </div>
    </div>
  `
})
export class MzFilterDropdownComponent {
  readonly ChevronIcon = ChevronDown;
  @Input() label = '';
  @Input() ariaLabel = '';
  @Input() options: MzFilterOption[] = [];
  @Input() selected: string[] = [];
  @Input() multi = true;
  @Input() allLabel = '';
  @Output() selectedChange = new EventEmitter<string[]>();
  open = false;

  constructor(private el: ElementRef) {}

  @HostListener('document:click', ['$event'])
  onDoc(e: MouseEvent): void { if (!this.el.nativeElement.contains(e.target)) this.open = false; }

  get triggerLabel(): string {
    if (!this.selected.length) return this.allLabel || this.label;
    if (this.selected.length === 1) return this.options.find((o) => o.value === this.selected[0])?.label ?? this.selected[0];
    return `${this.label} (${this.selected.length})`;
  }
  isSelected(v: string): boolean { return this.selected.includes(v); }
  toggle(v: string): void {
    if (!this.multi) { this.selectedChange.emit(this.isSelected(v) ? [] : [v]); this.open = false; return; }
    this.selectedChange.emit(this.isSelected(v) ? this.selected.filter((x) => x !== v) : [...this.selected, v]);
  }
}

@Component({
  selector: 'mz-pagination-button',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'mz', style: 'display:inline-flex' },
  template: `<button type="button" class="mz-pgbtn" [class.active]="active" [disabled]="disabled" (click)="pressed.emit()"><ng-content></ng-content></button>`
})
export class MzPaginationButtonComponent {
  @Input() active = false;
  @Input() disabled = false;
  @Output() pressed = new EventEmitter<void>();
}

@Component({
  selector: 'mz-indicator-card',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'mz', style: 'display:block' },
  template: `
    <div class="mz-indicator">
      <span class="mz-indicator-label">{{ label }}</span>
      <span class="mz-indicator-value" [class.mz-ltr]="ltr">{{ value == null || value === '' ? '—' : value }}</span>
      <span class="mz-indicator-hint" *ngIf="hint">{{ hint }}</span>
    </div>
  `
})
export class MzIndicatorCardComponent {
  @Input() label = '';
  @Input() value: string | number | null | undefined;
  @Input() hint?: string | null;
  @Input() ltr = false;
}

export interface MzAuthority {
  name: string;
  status: 'good' | 'bad' | 'neutral';
  statusLabel: string;
  date?: string | null;
  note?: string | null;
  pdfUrl?: string | null;
  actionLabel?: string | null;
}

@Component({
  selector: 'mz-authority-card',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, MzStatusTagComponent],
  host: { class: 'mz mz-authority-host' },
  template: `
    <div class="mz-authority" [ngClass]="authority.status">
      <div class="mz-authority-top">
        <div class="mz-authority-head">
          <span class="mz-authority-name">{{ authority.name }}</span>
          <mz-status-tag *ngIf="authority.name !== 'مؤشر الشريعة EGX33'" [tone]="authority.status" [label]="authority.statusLabel"></mz-status-tag>
        </div>
        <div class="mz-authority-verdict" [class.no-opinion]="authority.statusLabel === 'لا يوجد رأي مسجّل'">
          {{ authority.statusLabel }}
        </div>
        <div class="mz-authority-meta">
          <span *ngIf="authority.date">{{ authority.date }}</span>
          <span *ngIf="!authority.date && authority.name === 'مؤشر الشريعة EGX33'" class="mz-authority-egx33-code">EGX33</span>
        </div>
      </div>
      <div class="mz-authority-bottom">
        <div class="mz-authority-note" *ngIf="authority.note">{{ authority.note }}</div>
        <div class="mz-authority-actions" *ngIf="authority.pdfUrl || authority.actionLabel">
          <a *ngIf="authority.pdfUrl" [href]="authority.pdfUrl" target="_blank" rel="noopener noreferrer" class="mz-authority-pdf">
            <lucide-icon [img]="FileTextIcon" [size]="14"></lucide-icon> View PDF
          </a>
          <button *ngIf="authority.actionLabel" type="button" class="mz-authority-action" (click)="action.emit()">
            {{ authority.actionLabel }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class MzAuthorityCardComponent {
  readonly FileTextIcon = FileText;
  @Input() authority!: MzAuthority;
  @Output() action = new EventEmitter<void>();
}

export const MZ_STAR = Star;
