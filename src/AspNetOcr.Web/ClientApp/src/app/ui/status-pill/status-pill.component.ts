import { Component, Input } from '@angular/core';

@Component({
  selector: 'asp-status-pill',
  standalone: true,
  template: `<span class="pill" [class]="'pill pill--' + normalized">{{ normalized }}</span>`,
  styleUrl: './status-pill.component.scss'
})
export class StatusPillComponent {
  @Input() status = 'queued';

  get normalized(): string {
    return this.status.toLowerCase();
  }
}
