import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClientHomeIntent, ClientHomeViewModel } from './client-home-view-model';

@Component({
  selector: 'asp-ledger-scan-client-home',
  standalone: true,
  template: `
    <section class="client-home" aria-labelledby="client-home-title">
      <div class="client-home__header">
        <p class="eyebrow">{{ viewModel.healthSummary.label }}</p>
        <h1 id="client-home-title">{{ viewModel.productName }}</h1>
        <p>{{ viewModel.healthSummary.detail }}</p>
      </div>

      <div class="client-home__actions" aria-label="Primary actions">
        @for (action of viewModel.actions; track action.label) {
          <button type="button" class="focus-ring" (click)="intent.emit(action.intent)">
            {{ action.label }}
          </button>
        }
      </div>

      <section class="client-home__rail" aria-label="Recent proof states">
        @for (job of viewModel.recentJobs; track job.id) {
          <button type="button" class="client-home__job focus-ring" (click)="openJob(job.id)">
            <span>{{ job.label }}</span>
            <strong [attr.title]="'Canonical state code: ' + job.stateCode">{{ job.stateCode }}</strong>
            <small>{{ job.updatedAt }}</small>
          </button>
        }
      </section>
    </section>
  `,
  styleUrl: './ledger-scan-client-home.component.scss'
})
export class LedgerScanClientHomeComponent {
  @Input({ required: true }) viewModel!: ClientHomeViewModel;
  @Output() readonly intent = new EventEmitter<ClientHomeIntent>();

  openJob(jobId: string): void {
    this.intent.emit({ kind: 'open_job', jobId });
  }
}
