import { AfterViewInit, Component, ComponentRef, OnDestroy, ViewChild, ViewContainerRef, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { PRODUCT_CONFIG } from '../product-config.token';
import { CLIENT_HOME_COMPONENT, CLIENT_HOME_VIEW_MODEL } from './client-home.token';
import { ClientHomeComponentContract, ClientHomeIntent, ClientHomeViewModel } from './client-home-view-model';

@Component({
  selector: 'asp-client-home-host',
  standalone: true,
  template: `<ng-container #host></ng-container>`
})
export class ClientHomeHostComponent implements AfterViewInit, OnDestroy {
  @ViewChild('host', { read: ViewContainerRef, static: true }) host!: ViewContainerRef;

  private readonly componentType = inject(CLIENT_HOME_COMPONENT);
  private readonly viewModel = inject(CLIENT_HOME_VIEW_MODEL);
  private readonly product = inject(PRODUCT_CONFIG);
  private readonly router = inject(Router);
  private readonly subscription = new Subscription();
  private componentRef: ComponentRef<ClientHomeComponentContract> | null = null;

  ngAfterViewInit(): void {
    this.componentRef = this.host.createComponent(this.componentType);
    this.componentRef.setInput('viewModel', this.productViewModel());
    this.subscription.add(this.componentRef.instance.intent.subscribe((intent) => this.handleIntent(intent)));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.componentRef?.destroy();
  }

  private handleIntent(intent: ClientHomeIntent): void {
    if (intent.kind === 'open_intake') {
      void this.router.navigate(['/upload']);
      return;
    }

    if (intent.kind === 'open_job') {
      void this.router.navigate(intent.jobId ? ['/results', intent.jobId] : ['/jobs']);
      return;
    }

    if (intent.kind === 'open_evidence') {
      void this.router.navigate(intent.jobId ? ['/results', intent.jobId] : ['/jobs'], { fragment: 'evidence' });
    }
  }

  private productViewModel(): ClientHomeViewModel {
    return {
      ...this.viewModel,
      productName: this.product.name
    };
  }
}
