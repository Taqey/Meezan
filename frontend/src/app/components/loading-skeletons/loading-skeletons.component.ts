import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/** Shimmer placeholders for grids and KPI strips while data loads. */
@Component({
  selector: 'app-loading-skeletons',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="entity-grid" *ngIf="variant === 'card'" aria-hidden="true">
      <div class="skeleton-card" *ngFor="let n of range; trackBy: trackIdx">
        <div class="sk-line sk-title"></div>
        <div class="sk-line sk-text"></div>
        <div class="sk-line sk-price"></div>
      </div>
    </div>
    <div class="kpi-grid" *ngIf="variant === 'kpi'" aria-hidden="true">
      <div class="kpi-card" *ngFor="let n of range; trackBy: trackIdx">
        <div class="sk-line sk-text"></div>
        <div class="sk-line sk-price"></div>
      </div>
    </div>
    <ng-container *ngIf="variant === 'stat'">
      <div class="mz-card mz-stat" *ngFor="let n of range; trackBy: trackIdx" style="height:100%" aria-hidden="true">
        <div class="sk-line sk-text" style="width:50%;margin-bottom:8px"></div>
        <div class="sk-line sk-title" style="width:70%;height:28px"></div>
      </div>
    </ng-container>
  `
})
export class LoadingSkeletonsComponent {
  @Input() count = 6;
  @Input() variant: 'card' | 'kpi' | 'stat' = 'card';

  get range(): number[] {
    return Array.from({ length: Math.max(0, this.count) }, (_, i) => i);
  }

  trackIdx(index: number): number {
    return index;
  }
}
