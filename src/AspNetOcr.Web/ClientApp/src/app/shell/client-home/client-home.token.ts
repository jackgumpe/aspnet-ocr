import { InjectionToken, Provider, Type } from '@angular/core';
import { ClientHomeComponentContract, ClientHomeViewModel } from './client-home-view-model';
import { LedgerScanClientHomeComponent } from './ledger-scan-client-home.component';
import { LEDGER_SCAN_CLIENT_HOME_VIEW_MODEL } from './ledger-scan-client-home.viewmodel';

export const CLIENT_HOME_COMPONENT = new InjectionToken<Type<ClientHomeComponentContract>>('CLIENT_HOME_COMPONENT');
export const CLIENT_HOME_VIEW_MODEL = new InjectionToken<ClientHomeViewModel>('CLIENT_HOME_VIEW_MODEL');

export function provideClientHome(
  component: Type<ClientHomeComponentContract> = LedgerScanClientHomeComponent,
  viewModel: ClientHomeViewModel = LEDGER_SCAN_CLIENT_HOME_VIEW_MODEL
): Provider[] {
  return [
    { provide: CLIENT_HOME_COMPONENT, useValue: component },
    { provide: CLIENT_HOME_VIEW_MODEL, useValue: viewModel }
  ];
}
