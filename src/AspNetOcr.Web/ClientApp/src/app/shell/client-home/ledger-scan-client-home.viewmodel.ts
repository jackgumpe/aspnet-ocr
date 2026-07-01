import { ClientHomeViewModel } from './client-home-view-model';

export const LEDGER_SCAN_CLIENT_HOME_VIEW_MODEL = {
  productName: 'LedgerScan',
  recentJobs: [
    {
      id: 'mock-ledger-001',
      label: 'sample-product-sheet.pdf',
      stateCode: 'LIVE_VERIFIED',
      updatedAt: 'recent'
    }
  ],
  healthSummary: {
    level: 'healthy',
    label: 'Evidence pipeline verified',
    detail: 'Consumer-path proof rail remains authoritative.'
  },
  actions: [
    { label: 'Open intake', intent: { kind: 'open_intake' } },
    { label: 'Open latest job', intent: { kind: 'open_job', jobId: 'mock-ledger-001' } },
    { label: 'Open evidence', intent: { kind: 'open_evidence', jobId: 'mock-ledger-001' } }
  ]
} as const satisfies ClientHomeViewModel;
