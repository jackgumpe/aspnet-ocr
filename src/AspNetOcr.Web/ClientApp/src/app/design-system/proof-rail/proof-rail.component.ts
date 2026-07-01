import { NgFor } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  PROOF_RAIL_STATE_DEFINITIONS,
  ProofRailReviewStatus,
  ProofRailStateCode,
  ProofRailStateDefinition
} from './proof-rail-state';

@Component({
  selector: 'asp-proof-rail',
  standalone: true,
  imports: [NgFor],
  template: `
    <section
      class="proof-rail"
      [class.is-scanning]="transition === 'scanning'"
      [class.is-latched]="transition === 'latched'"
      aria-label="Trusted OCR proof rail">
      <header class="proof-rail__header">
        <p class="eyebrow">Proof Rail</p>
        <span
          class="proof-rail__seal"
          [attr.title]="reviewTooltip"
          [attr.aria-label]="reviewTooltip">
          <span aria-hidden="true">◉</span>
          <strong>{{ reviewStatus }}</strong>
        </span>
      </header>
      <ol class="proof-rail__track">
        <li
          *ngFor="let state of states"
          [class.is-active]="isActive(state.code)"
          [class.is-terminal]="state.code === 'LIVE_VERIFIED'"
          [attr.aria-current]="isActive(state.code) ? 'step' : null">
          <span
            class="proof-rail__node"
            [attr.title]="tooltipFor(state)"
            [attr.aria-label]="ariaLabelFor(state)">
            <span class="proof-rail__dot" aria-hidden="true">●</span>
            <span class="proof-rail__code">{{ state.code }}</span>
          </span>
          <span class="proof-rail__label">{{ state.label }}</span>
        </li>
      </ol>
      @if (transition === 'scanning') {
        <p class="proof-rail__transition" role="status">Amber scan pulse active</p>
      }
      @if (transition === 'latched') {
        <p class="proof-rail__transition proof-rail__transition--latched" role="status">Verified mechanical latch set</p>
      }
    </section>
  `,
  styleUrl: './proof-rail.component.scss'
})
export class ProofRailComponent {
  @Input() activeStates: readonly ProofRailStateCode[] = [];
  @Input() reviewStatus: ProofRailReviewStatus = 'NOT_SELECTED';
  @Input() transition: 'idle' | 'scanning' | 'latched' = 'idle';

  readonly states = PROOF_RAIL_STATE_DEFINITIONS;
  readonly reviewTooltip =
    'Review sampling is orthogonal: NOT_SELECTED does not make LIVE_VERIFIED incomplete.';

  isActive(code: ProofRailStateCode): boolean {
    return this.activeStates.includes(code);
  }

  tooltipFor(state: ProofRailStateDefinition): string {
    return `Canonical state code: ${state.code}. ${state.accessibleMeaning} Metadata: ${state.proofMetadata}. Integrity: ${state.integrityBehavior}`;
  }

  ariaLabelFor(state: ProofRailStateDefinition): string {
    return `${state.code}: ${state.accessibleMeaning}`;
  }
}
