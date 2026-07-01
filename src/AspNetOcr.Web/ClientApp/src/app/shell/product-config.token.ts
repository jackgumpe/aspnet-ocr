import { InjectionToken, Provider } from '@angular/core';
import { ProductConfig, assertProductConfig } from './product-config';

export const PRODUCT_CONFIG = new InjectionToken<Readonly<ProductConfig>>('PRODUCT_CONFIG', {
  providedIn: 'root',
  factory: () => {
    throw new Error('PRODUCT_CONFIG must be provided at build time.');
  }
});

export function provideProductConfig(config: ProductConfig): Provider {
  return {
    provide: PRODUCT_CONFIG,
    useValue: assertProductConfig(config)
  };
}
