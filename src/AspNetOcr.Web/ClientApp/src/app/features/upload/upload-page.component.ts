import { AsyncPipe, NgIf } from '@angular/common';
import { Component, OnDestroy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Subscription } from 'rxjs';
import { OcrApiService } from '../../core/ocr-api.service';
import { OcrJob } from '../../core/ocr-job.model';
import { MetricCardComponent } from '../../ui/metric-card/metric-card.component';
import { PanelComponent } from '../../ui/panel/panel.component';
import { StatusPillComponent } from '../../ui/status-pill/status-pill.component';
import { UploadZoneComponent } from '../../ui/upload-zone/upload-zone.component';

@Component({
  selector: 'asp-upload-page',
  standalone: true,
  imports: [
    AsyncPipe,
    MatButtonModule,
    MetricCardComponent,
    NgIf,
    PanelComponent,
    RouterLink,
    StatusPillComponent,
    UploadZoneComponent
  ],
  template: `
    <section class="page-head">
      <p class="eyebrow">ASP-OCR-003</p>
      <h1>OCR upload</h1>
    </section>

    <div class="metrics" *ngIf="api.summary$() | async as summary">
      <asp-metric-card label="Jobs" [value]="summary.total.toString()" detail="Session history"></asp-metric-card>
      <asp-metric-card label="Active" [value]="summary.active.toString()" detail="Queued or processing" tone="accent"></asp-metric-card>
      <asp-metric-card label="Failed" [value]="summary.failed.toString()" detail="Retry available" tone="danger"></asp-metric-card>
    </div>

    <div class="upload-layout">
      <asp-panel title="New document" eyebrow="MockOcrProvider">
        <asp-upload-zone
          [busy]="busy"
          [progress]="progress"
          [error]="error"
          (fileSelected)="onFileSelected($event)">
        </asp-upload-zone>
        @if (error && lastFile) {
          <div class="retry-row">
            <button mat-stroked-button type="button" class="focus-ring" (click)="retry()">Retry</button>
          </div>
        }
      </asp-panel>

      <asp-panel title="Current job" eyebrow="Status">
        @if (currentJob) {
          <div class="job-card">
            <div>
              <h2>{{ currentJob.sourceFileName }}</h2>
              <p>{{ currentJob.pagesProcessed }} / {{ currentJob.totalPages || 1 }} pages processed</p>
            </div>
            <asp-status-pill [status]="currentJob.status"></asp-status-pill>
            @if (currentJob.status === 'complete') {
              <a mat-flat-button color="primary" class="focus-ring" [routerLink]="['/results', currentJob.id]">Open result</a>
            }
            @if (currentJob.status === 'failed') {
              <p class="error-text">{{ currentJob.error }}</p>
            }
          </div>
        } @else {
          <p class="muted">No active upload.</p>
        }
      </asp-panel>
    </div>
  `,
  styleUrl: './upload-page.component.scss'
})
export class UploadPageComponent implements OnDestroy {
  readonly api = inject(OcrApiService);

  busy = false;
  progress = 0;
  error = '';
  currentJob: OcrJob | null = null;
  lastFile: File | null = null;

  private readonly subscription = new Subscription();

  constructor() {
    this.subscription.add(this.api.refreshJobs().subscribe());
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onFileSelected(file: File): void {
    this.lastFile = file;
    this.error = '';

    if (!this.isSupported(file)) {
      this.error = 'Only PDF, PNG, JPG, and TIFF files are accepted.';
      this.busy = false;
      this.progress = 0;
      return;
    }

    this.busy = true;
    this.progress = 24;
    this.subscription.add(
      this.api.createJob(file).subscribe({
        next: (job) => {
          this.currentJob = job;
          this.progress = 52;
          this.watchJob(job.id);
        },
        error: () => {
          this.error = 'Upload failed. Retry is available.';
          this.busy = false;
          this.progress = 0;
        }
      })
    );
  }

  retry(): void {
    if (this.lastFile) {
      this.onFileSelected(this.lastFile);
    }
  }

  private watchJob(id: string): void {
    this.subscription.add(
      this.api.pollJob(id).subscribe((job) => {
        if (!job) {
          return;
        }

        this.currentJob = job;
        this.progress = job.status === 'complete' ? 100 : job.status === 'failed' ? 100 : 76;
        if (job.status === 'complete' || job.status === 'failed') {
          this.busy = false;
          if (job.status === 'failed') {
            this.error = job.error ?? 'OCR job failed.';
          }
        }
      })
    );
  }

  private isSupported(file: File): boolean {
    const name = file.name.toLowerCase();
    return file.type === 'application/pdf' ||
      file.type.startsWith('image/') ||
      ['.pdf', '.png', '.jpg', '.jpeg', '.tif', '.tiff'].some((extension) => name.endsWith(extension));
  }
}
