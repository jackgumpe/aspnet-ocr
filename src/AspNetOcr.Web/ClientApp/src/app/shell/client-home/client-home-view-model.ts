import { EventEmitter } from '@angular/core';
import { ProofRailStateCode } from '../../design-system/proof-rail/proof-rail-state';

export type ClientHomeIntentKind = 'open_intake' | 'open_job' | 'open_evidence';

export interface ClientHomeIntent {
  readonly kind: ClientHomeIntentKind;
  readonly jobId?: string;
}

export interface ClientHomeAction {
  readonly label: string;
  readonly intent: ClientHomeIntent;
}

export interface ClientHomeRecentJob {
  readonly id: string;
  readonly label: string;
  readonly stateCode: ProofRailStateCode;
  readonly updatedAt: string;
}

export interface ClientHomeHealthSummary {
  readonly level: 'healthy' | 'degraded' | 'critical';
  readonly label: string;
  readonly detail: string;
}

export interface ClientHomeViewModel {
  readonly productName: string;
  readonly recentJobs: readonly ClientHomeRecentJob[];
  readonly healthSummary: ClientHomeHealthSummary;
  readonly actions: readonly ClientHomeAction[];
}

export interface ClientHomeComponentContract {
  viewModel: ClientHomeViewModel;
  readonly intent: EventEmitter<ClientHomeIntent>;
}
