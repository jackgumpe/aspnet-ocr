import { ProductConfig } from '../../shell/product-config';

export const CONTRAST_LAB_CONFIG = {
  schemaVersion: 'product_config_v1',
  id: 'contrast-lab',
  name: 'Contrast Laboratory Verification Surface With Extended Product Naming',
  shortName: 'Contrast Lab',
  logoAsset: 'assets/contrast-lab-wide.svg',
  logoAlt: 'Contrast Lab wide mark',
  tagline: 'Adversarial proof of product shell boundaries',
  terminology: {
    upload: 'Capture bay',
    jobs: 'Run ledger',
    evidence: 'Proof file',
    result: 'Decoded record',
    health: 'Signal'
  },
  navigation: [
    { label: 'Home', route: '/', ariaLabel: 'Open contrast-lab home' },
    { label: 'Capture', route: '/upload', ariaLabel: 'Open contrast capture bay' },
    { label: 'Ledger', route: '/jobs', ariaLabel: 'Open contrast run ledger' },
    { label: 'Clients', route: '/clients', ariaLabel: 'Open contrast client fixture', capability: 'inventory' }
  ],
  capabilities: {
    inventory: true,
    evidenceInspector: true
  },
  layout: 'warehouse-lab',
  themeClass: 'theme-contrast-lab'
} as const satisfies ProductConfig;
