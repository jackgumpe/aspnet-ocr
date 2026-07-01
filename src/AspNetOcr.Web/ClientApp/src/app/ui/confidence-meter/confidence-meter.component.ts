import { DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'asp-confidence-meter',
  standalone: true,
  imports: [DecimalPipe],
  template: `
    <div class="meter" [attr.aria-label]="label + ' ' + (value | number:'1.0-0') + '%'" role="meter" aria-valuemin="0" aria-valuemax="100" [attr.aria-valuenow]="value">
      <div class="meter__label">
        <span>{{ label }}</span>
        <strong>{{ value | number:'1.0-0' }}%</strong>
      </div>
      <div class="meter__track">
        <span [style.width.%]="value"></span>
      </div>
    </div>
  `,
  styleUrl: './confidence-meter.component.scss'
})
export class ConfidenceMeterComponent {
  @Input() label = 'ENGINE CONFIDENCE';
  @Input() value = 0;
}
