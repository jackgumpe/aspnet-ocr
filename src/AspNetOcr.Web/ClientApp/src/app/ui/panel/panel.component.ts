import { Component, Input } from '@angular/core';

@Component({
  selector: 'asp-panel',
  standalone: true,
  template: `
    <section class="panel surface-panel">
      @if (eyebrow || title) {
        <header class="panel__header">
          @if (eyebrow) {
            <p class="eyebrow">{{ eyebrow }}</p>
          }
          @if (title) {
            <h2>{{ title }}</h2>
          }
        </header>
      }
      <ng-content></ng-content>
    </section>
  `,
  styleUrl: './panel.component.scss'
})
export class PanelComponent {
  @Input() title = '';
  @Input() eyebrow = '';
}
