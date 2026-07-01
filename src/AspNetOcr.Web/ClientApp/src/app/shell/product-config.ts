export type ProductLayout = 'warehouse-lab';
export type ProductThemeClass = 'theme-ledger-scan' | 'theme-contrast-lab';

export interface ProductCapabilities {
  readonly inventory: boolean;
  readonly evidenceInspector: boolean;
}

export interface ProductNavigationItem {
  readonly label: string;
  readonly route: string;
  readonly ariaLabel: string;
  readonly capability?: keyof ProductCapabilities;
}

export interface ProductTerminology {
  readonly upload: string;
  readonly jobs: string;
  readonly evidence: string;
  readonly result: string;
  readonly health: string;
}

export interface ProductConfig {
  readonly schemaVersion: 'product_config_v1';
  readonly id: string;
  readonly name: string;
  readonly shortName: string;
  readonly logoAsset: string;
  readonly logoAlt: string;
  readonly tagline: string;
  readonly terminology: ProductTerminology;
  readonly navigation: readonly ProductNavigationItem[];
  readonly capabilities: ProductCapabilities;
  readonly layout: ProductLayout;
  readonly themeClass: ProductThemeClass;
}

export function assertProductConfig(config: ProductConfig): Readonly<ProductConfig> {
  const failures: string[] = [];

  if (config.schemaVersion !== 'product_config_v1') {
    failures.push('schemaVersion must be product_config_v1');
  }

  for (const key of ['id', 'name', 'shortName', 'logoAsset', 'logoAlt', 'tagline'] as const) {
    if (!config[key]) {
      failures.push(`${key} is required`);
    }
  }

  if (config.layout !== 'warehouse-lab') {
    failures.push('layout must be warehouse-lab');
  }

  if (!Array.isArray(config.navigation) || config.navigation.length === 0) {
    failures.push('navigation must include at least one item');
  }

  for (const item of config.navigation) {
    if (!item.label || !item.route || !item.ariaLabel) {
      failures.push('navigation items require label, route, and ariaLabel');
    }
  }

  if (failures.length > 0) {
    throw new Error(`Invalid ProductConfig: ${failures.join('; ')}`);
  }

  return Object.freeze(config);
}
