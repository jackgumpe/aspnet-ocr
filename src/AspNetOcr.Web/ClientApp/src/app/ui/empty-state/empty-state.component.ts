import { Component, Input } from '@angular/core';

@Component({
  selector: 'asp-empty-state',
  standalone: true,
  template: `
    <div class="empty">
      <span aria-hidden="true">{{ mark }}</span>
      <h3>{{ title }}</h3>
      <p>{{ message }}</p>
    </div>
  `,
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  @Input() mark = '—';
  @Input({ required: true }) title = '';
  @Input({ required: true }) message = '';
}
