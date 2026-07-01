import { AsyncPipe, DatePipe, DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { OcrApiService } from '../../core/ocr-api.service';
import { OcrJob } from '../../core/ocr-job.model';
import { ConfidenceMeterComponent } from '../../ui/confidence-meter/confidence-meter.component';
import { EmptyStateComponent } from '../../ui/empty-state/empty-state.component';
import { MetricCardComponent } from '../../ui/metric-card/metric-card.component';
import { PanelComponent } from '../../ui/panel/panel.component';
import { StatusPillComponent } from '../../ui/status-pill/status-pill.component';

@Component({
  selector: 'asp-job-dashboard',
  standalone: true,
  imports: [
    AsyncPipe,
    ConfidenceMeterComponent,
    DatePipe,
    DecimalPipe,
    EmptyStateComponent,
    MatButtonModule,
    MetricCardComponent,
    NgFor,
    NgIf,
    PanelComponent,
    RouterLink,
    StatusPillComponent
  ],
  template: `
    <section class="page-head">
      <p class="eyebrow">Queue</p>
      <h1>Job dashboard</h1>
    </section>

    <div class="metrics" *ngIf="api.summary$() | async as summary">
      <asp-metric-card label="Total" [value]="summary.total.toString()" detail="Jobs in memory"></asp-metric-card>
      <asp-metric-card label="Complete" [value]="summary.complete.toString()" detail="Ready to inspect"></asp-metric-card>
      <asp-metric-card label="Active" [value]="summary.active.toString()" detail="Queued or processing" tone="accent"></asp-metric-card>
      <asp-metric-card label="Failed" [value]="summary.failed.toString()" detail="Needs retry" tone="danger"></asp-metric-card>
    </div>

    <asp-panel title="History" eyebrow="Mock OCR queue">
      <ng-container *ngIf="api.jobs$ | async as jobs">
        <div class="jobs" *ngIf="jobs.length > 0; else emptyJobs">
          <article class="job-row" *ngFor="let job of jobs; trackBy: trackByJobId">
            <div class="job-row__main">
              <h2>{{ job.sourceFileName }}</h2>
              <p>{{ job.createdAtUtc | date:'medium' }} · {{ job.pagesProcessed }} / {{ job.totalPages || 1 }} pages</p>
            </div>
            <asp-status-pill [status]="job.status"></asp-status-pill>
            <asp-confidence-meter *ngIf="job.confidence !== null" [value]="job.confidence * 100"></asp-confidence-meter>
            <a mat-stroked-button class="focus-ring" [routerLink]="['/results', job.id]" [attr.aria-label]="'Open result for ' + job.sourceFileName">
              Open
            </a>
          </article>
        </div>
        <ng-template #emptyJobs>
          <asp-empty-state mark="+" title="No jobs" message="Upload a document to create the first OCR job."></asp-empty-state>
        </ng-template>
      </ng-container>
    </asp-panel>
  `,
  styleUrl: './job-dashboard.component.scss'
})
export class JobDashboardComponent {
  readonly api = inject(OcrApiService);

  constructor() {
    this.api.refreshJobs().subscribe();
  }

  trackByJobId(_index: number, job: OcrJob): string {
    return job.id;
  }
}
