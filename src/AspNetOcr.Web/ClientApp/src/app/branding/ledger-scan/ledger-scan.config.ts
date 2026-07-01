import { ProductConfig } from '../../shell/product-config';

export const LEDGER_SCAN_CONFIG = {
  schemaVersion: 'product_config_v1',
  id: 'ledger-scan',
  name: 'LedgerScan',
  shortName: 'LedgerScan',
  logoAsset: 'assets/ledger-scan-mark.svg',
  logoAlt: 'LedgerScan document mark',
  tagline: 'Verified OCR evidence workspace',
  terminology: {
    upload: 'Intake',
    jobs: 'Work queue',
    evidence: 'Evidence',
    result: 'Result',
    health: 'Health'
  },
  navigation: [
    { label: 'Home', route: '/', ariaLabel: 'Open LedgerScan home' },
    { label: 'Intake', route: '/upload', ariaLabel: 'Open document intake' },
    { label: 'Queue', route: '/jobs', ariaLabel: 'Open OCR job queue' },
    { label: 'Clients', route: '/clients', ariaLabel: 'Open client inventory placeholder', capability: 'inventory' }
  ],
  capabilities: {
    inventory: true,
    evidenceInspector: true
  },
  layout: 'warehouse-lab',
  themeClass: 'theme-ledger-scan'
} as const satisfies ProductConfig;
