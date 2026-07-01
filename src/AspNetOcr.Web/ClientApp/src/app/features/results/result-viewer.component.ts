import { AsyncPipe, DecimalPipe, JsonPipe, NgFor, NgIf } from '@angular/common';
import { Component, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { Subscription, switchMap } from 'rxjs';
import { OcrApiService } from '../../core/ocr-api.service';
import { OcrJob } from '../../core/ocr-job.model';
import { ProofRailComponent } from '../../design-system/proof-rail/proof-rail.component';
import { ProofRailStateCode } from '../../design-system/proof-rail/proof-rail-state';
import { ConfidenceMeterComponent } from '../../ui/confidence-meter/confidence-meter.component';
import { EmptyStateComponent } from '../../ui/empty-state/empty-state.component';
import { PanelComponent } from '../../ui/panel/panel.component';
import { StatusPillComponent } from '../../ui/status-pill/status-pill.component';

@Component({
  selector: 'asp-result-viewer',
  standalone: true,
  imports: [
    AsyncPipe,
    ConfidenceMeterComponent,
    DecimalPipe,
    EmptyStateComponent,
    JsonPipe,
    MatButtonModule,
    NgFor,
    NgIf,
    PanelComponent,
    ProofRailComponent,
    RouterLink,
    StatusPillComponent
  ],
  template: `
    <section class="page-head">
      <p class="eyebrow">Result</p>
      <h1>OCR result viewer</h1>
    </section>

    @if (job) {
      <div class="result-layout">
        <asp-panel [title]="job.sourceFileName" eyebrow="Document">
          <div class="result-summary">
            <asp-status-pill [status]="job.status"></asp-status-pill>
            @if (job.result) {
              <asp-confidence-meter label="ENGINE CONFIDENCE" [value]="job.result.confidence * 100"></asp-confidence-meter>
            }
          </div>
          @if (job.status !== 'complete') {
            <asp-empty-state mark="…" title="Result pending" message="The OCR job has not reached a terminal complete state."></asp-empty-state>
          }
          @if (job.error) {
            <p class="error-text">{{ job.error }}</p>
          }
        </asp-panel>

        <asp-panel title="Evidence proof" eyebrow="Integrity" id="evidence">
          <asp-proof-rail
            [activeStates]="proofStatesFor(job)"
            [transition]="proofTransitionFor(job)"
            reviewStatus="NOT_SELECTED">
          </asp-proof-rail>
        </asp-panel>

        @if (job.result) {
          <asp-panel title="Metrics" eyebrow="Engine confidence">
            <dl class="metric-list">
              <div>
                <dt>ENGINE CONFIDENCE</dt>
                <dd>{{ job.result.confidence | number:'1.2-2' }}</dd>
              </div>
              <div>
                <dt>CER</dt>
                <dd>{{ job.result.characterErrorRate ?? 0 | number:'1.2-4' }}</dd>
              </div>
              <div>
                <dt>WER</dt>
                <dd>{{ job.result.wordErrorRate ?? 0 | number:'1.2-4' }}</dd>
              </div>
            </dl>
          </asp-panel>

          <asp-panel title="Structured text" eyebrow="Normalized OCR">
            <pre class="text-output">{{ job.result.text }}</pre>
          </asp-panel>

          <asp-panel title="Extracted fields" eyebrow="Schema">
            <div class="fields">
              <div class="field-row" *ngFor="let field of job.result.fields">
                <strong>{{ field.name }}</strong>
                <span>{{ field.value }}</span>
                <small>{{ field.confidence.value * 100 | number:'1.0-0' }}%</small>
              </div>
            </div>
          </asp-panel>

          <asp-panel title="Raw JSON" eyebrow="Artifact">
            <button mat-stroked-button type="button" class="focus-ring" (click)="showRaw = !showRaw">
              {{ showRaw ? 'Hide JSON' : 'Show JSON' }}
            </button>
            @if (showRaw) {
              <pre class="json-output">{{ parsedRawJson(job.result.rawJson) | json }}</pre>
            }
          </asp-panel>
        }
      </div>
    } @else {
      <asp-empty-state mark="?" title="Result not found" message="The selected job is not available in the current API session."></asp-empty-state>
      <a mat-stroked-button class="focus-ring" routerLink="/jobs">Back to jobs</a>
    }
  `,
  styleUrl: './result-viewer.component.scss'
})
export class ResultViewerComponent implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(OcrApiService);
  private readonly subscription = new Subscription();

  job: OcrJob | null = null;
  showRaw = false;

  constructor() {
    this.subscription.add(
      this.route.paramMap.pipe(
        switchMap((params) => this.api.getJob(params.get('id') ?? ''))
      ).subscribe((job) => {
        this.job = job;
        if (job && (job.status === 'queued' || job.status === 'processing')) {
          this.subscription.add(this.api.pollJob(job.id).subscribe((nextJob) => {
            this.job = nextJob;
          }));
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  parsedRawJson(rawJson: string): unknown {
    try {
      return JSON.parse(rawJson);
    } catch {
      return rawJson;
    }
  }

  proofStatesFor(job: OcrJob): readonly ProofRailStateCode[] {
    if (job.status === 'complete') {
      return ['RESULT_CREATED', 'EVIDENCE_WRITTEN', 'INGESTED', 'QUERYABLE', 'LIVE_VERIFIED'];
    }

    if (job.status === 'processing') {
      return ['RESULT_CREATED', 'EVIDENCE_WRITTEN'];
    }

    if (job.status === 'failed') {
      return ['RESULT_CREATED', 'EVIDENCE_WRITTEN'];
    }

    return ['RESULT_CREATED'];
  }

  proofTransitionFor(job: OcrJob): 'idle' | 'scanning' | 'latched' {
    if (job.status === 'complete') {
      return 'latched';
    }

    if (job.status === 'processing') {
      return 'scanning';
    }

    return 'idle';
  }
}
