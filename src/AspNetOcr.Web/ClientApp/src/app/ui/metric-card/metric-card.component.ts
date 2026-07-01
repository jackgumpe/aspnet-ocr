import { Component, Input } from '@angular/core';

@Component({
  selector: 'asp-metric-card',
  standalone: true,
  template: `
    <article class="metric" [class.metric--accent]="tone === 'accent'" [class.metric--danger]="tone === 'danger'">
      <span>{{ label }}</span>
      <strong>{{ value }}</strong>
      @if (detail) {
        <small>{{ detail }}</small>
      }
    </article>
  `,
  styleUrl: './metric-card.component.scss'
})
export class MetricCardComponent {
  @Input({ required: true }) label = '';
  @Input({ required: true }) value = '';
  @Input() detail = '';
  @Input() tone: 'default' | 'accent' | 'danger' = 'default';
}
